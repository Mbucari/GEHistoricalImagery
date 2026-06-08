using OSGeo.GDAL;
using OSGeo.OGR;

namespace LibMapCommon.Geometry;

public class Placemark : IDisposable
{
	public wkbGeometryType ParsedGeometryType { get; }
	public wkbGeometryType GeometryType { get; }
	private OSGeo.OGR.Geometry? m_Geometry;
	public OSGeo.OGR.Geometry Geometry => m_Geometry ?? throw new ObjectDisposedException(nameof(Placemark));
	public Dictionary<string, object> Fields { get; }
	public string? Name => TryGetName();
	public double GeodesicArea { get; }
	private Placemark(Dictionary<string, object>  fields, OSGeo.OGR.Geometry geometry, wkbGeometryType parsedGeometryType)
	{
		m_Geometry = geometry;
		GeometryType = geometry.GetGeometryType();
		ParsedGeometryType = parsedGeometryType;
		Fields = fields;
		GeodesicArea = geometry.GeodesicArea();
	}

	private string? TryGetName()
	{
		if (Fields.TryGetValue("Name", out var name) && name is string strName && !string.IsNullOrWhiteSpace(strName))
			return strName;
		foreach (var f in Fields.Keys)
		{
			if (f.Contains("name", StringComparison.OrdinalIgnoreCase) && Fields[f] is string str && !string.IsNullOrWhiteSpace(str))
				return str;
		}
		return null;
	}

	public static Placemark? CreatePolygon(Feature feature)
	{
		using var g = feature.GetGeometryRef();
		var geometryType = g.GetGeometryType();
		
		if (geometryType is wkbGeometryType.wkbPolygon25D or wkbGeometryType.wkbMultiPolygon25D)
		{
			g.FlattenTo2D();
			return new Placemark(GetFields(feature), g.Clone(), geometryType);
		}
		else if(geometryType is wkbGeometryType.wkbPolygon or wkbGeometryType.wkbMultiPolygon)
		{
			return new Placemark(GetFields(feature), g.Clone(), geometryType);
		}
		else if (geometryType is wkbGeometryType.wkbLineString or wkbGeometryType.wkbLineString25D)
		{
			int pointCount = g.GetPointCount();
			using var sr = g.GetSpatialReference();
			sr.ApplyAxisMap();
			using var ring = new OSGeo.OGR.Geometry(wkbGeometryType.wkbLinearRing);
			double[] coord = new double[2];
			for (int i = 0; i < pointCount; i++)
			{
				g.GetPoint_2D(i, coord);
				ring.AddPoint_2D(coord[0], coord[1]);
			}
			ring.CloseRings();
			ring.AssignSpatialReference(sr);

			var polygon = new OSGeo.OGR.Geometry(wkbGeometryType.wkbPolygon);
			polygon.AssignSpatialReference(sr);
			polygon.AddGeometry(ring);
			return new Placemark(GetFields(feature), polygon, geometryType);
		}
		else
		{
			Gdal.Error(CPLErr.CE_Log, 0, $"Unsupported geometry type {geometryType} for feature with name '{feature.GetFieldAsString("Name")}'. Only Polygon and LineString are supported.");
			return null;
		}
	}

	private static Dictionary<string,object> GetFields(Feature feature)
	{
		var nfields = feature.GetFieldCount();
		Dictionary<string, object> dict = new Dictionary<string, object>(nfields);
		for (int f = 0; f < nfields; f++)
		{
			using var fDefn = feature.GetFieldDefnRef(f);
			var fieldName = fDefn.GetName();
			var fieldType = fDefn.GetFieldType();
			dict[fieldName] = fieldType switch
			{
				FieldType.OFTInteger => feature.GetFieldAsInteger(f),
				FieldType.OFTIntegerList => feature.GetFieldAsIntegerList(f, out _),
				FieldType.OFTReal => feature.GetFieldAsDouble(f),
				FieldType.OFTRealList => feature.GetFieldAsDoubleList(f, out _),
				FieldType.OFTStringList or FieldType.OFTWideStringList => feature.GetFieldAsStringList(f),
				FieldType.OFTInteger64 => feature.GetFieldAsInteger64(f),
				_ => feature.GetFieldAsString(f)
			};
		}
		return dict;
	}

	~Placemark() => Dispose();
	public void Dispose()
	{
		var geometry = Interlocked.Exchange(ref m_Geometry, null);
		if (geometry != null)
		{
			geometry.Dispose();
			Fields.Clear();
			GC.SuppressFinalize(this);
		}
	}
}

public class KmlFile : IDisposable
{
	private Placemark[]? m_Placemarks;
	public Placemark[] Placemarks => m_Placemarks ?? throw new ObjectDisposedException(nameof(KmlFile));

	private KmlFile(params Placemark[] placemarks)
	{
		m_Placemarks = placemarks;
	}

	public static KmlFile? Parse(string filename)
	{
		try
		{
			using var file = Gdal.OpenEx(filename, 0, ["LIBKML"], null, []);
			var nLay = file.GetLayerCount();
			List<Placemark> placemarks = new List<Placemark>();
			for (int i = 0; i < nLay; i++)
			{
				using var layer = file.GetLayer(i);
				Feature? feature;
				while ((feature = layer.GetNextFeature()) != null)
				{
					int numGeoFields = feature.GetGeomFieldCount();
					if (numGeoFields != 1)
					{
						Gdal.Error(CPLErr.CE_Log, 0, $"Field contans {numGeoFields} geometries. Only 1 is supported.");
					}
					else if (Placemark.CreatePolygon(feature) is { } p)
					{
						placemarks.Add(p);
					}
					feature.Dispose();
				}
			}
			return placemarks.Count > 0 ? new KmlFile(placemarks.ToArray()) : null;
		}
		catch (Exception e)
		{
			Console.Error.WriteLine(e);
			return null;
		}
	}

	~KmlFile() => Dispose();
	public void Dispose()
	{
		var placemarks = Interlocked.Exchange(ref m_Placemarks, null);
		if (placemarks != null)
		{
			foreach (var placemark in placemarks)
			{
				placemark.Dispose();
			}
			GC.SuppressFinalize(this);
		}
	}
}
