using LibMapCommon;
using LibMapCommon.Geometry;
using OSGeo.OGR;
using OSGeo.OSR;

namespace OSGeo.GDAL;

[Flags]
public enum GDAL_OF : uint
{
	READONLY = 0,
	ALL = READONLY,
	UPDATE = 1,
	RASTER = 2,
	VECTOR = 4,
	GNM = 8,
	MULTIDIM_RASTER = 0x10,
	SHARED = 0x20,
	VERBOSE_ERROR = 0x40,
	INTERNAL = 0x80,
	ARRAY_BLOCK_ACCESS = 0x100,
	HASHSET_BLOCK_ACCESS = ARRAY_BLOCK_ACCESS
}

internal static class GDALExtensions
{
	public static GeoTransform GetGeoTransform(this Dataset dataset)
	{
		var geoTransform = new GeoTransform();
		dataset.GetGeoTransform(geoTransform.Transformation);
		return geoTransform;
	}

	public static void SetGeoTransform(this Dataset dataset, GeoTransform transform)
	{
		dataset.SetGeoTransform(transform.Transformation);
	}

	public record ShapePolygon(GeoPolygon<Wgs1984> Polygon, Dictionary<string, string> Features);
	public static IEnumerable<ShapePolygon> GetPolygons(this DataSource shp)
	{
		if (shp.GetLayerCount() == 0)
			yield break;

		using var t_sr = new SpatialReference("");
		t_sr.ImportFromEPSG(Wgs1984.EpsgNumber);

		for (int i = shp.GetLayerCount() - 1; i >= 0; i--)
		{
			using var layer = shp.GetLayerByIndex(i);
			if (layer.GetGeomType() is not wkbGeometryType.wkbPolygon)
				continue;

			using var s_sr = layer.GetSpatialRef();
			using var xForm = new CoordinateTransformation(s_sr, t_sr);

			for (Feature? feature; (feature = layer.GetNextFeature()) is not null; feature.Dispose())
			{
				using var geometry = feature.GetGeometryRef();
				using var ring = geometry.GetGeometryRef(0);
				if (ring.GetGeometryType() is not wkbGeometryType.wkbLineString and not wkbGeometryType.wkbLinearRing)
					continue;

				var numPoints = ring.GetPointCount();
				if (numPoints < 3)
					continue;

				var featureCount = feature.GetFieldCount();
				var features = new Dictionary<string, string>(featureCount);
				for (int f = 0; f < featureCount; f++)
				{
					using var field = feature.GetFieldDefnRef(f);
					features[field.GetName()] = feature.GetFieldAsString(f);
				}

				var points = new Wgs1984[numPoints];
				var point = new double[3];
				for (int j = 0; j < numPoints; j++)
				{
					ring.GetPoint(j, point);
					xForm.TransformPoint(point);
					points[j] = new Wgs1984(point[0], point[1]);
				}

				if (points[0].Equals(points[^1]))
				{
					if (points.Length < 3)
						continue;
					Array.Resize(ref points, points.Length - 1);
				}

				yield return new ShapePolygon(new GeoPolygon<Wgs1984>(points), features);
			}
		}
	}
}
