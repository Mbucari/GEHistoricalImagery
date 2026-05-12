using LibMapCommon;
using LibMapCommon.Geometry;
using OSGeo.OGR;
using OSGeo.OSR;

namespace OSGeo.GDAL;

/// <summary>
/// Flags use by <see cref="OSGeo.GDAL.Gdal.OpenEx(string, uint, string[], string[], string[])"/>
/// </summary>
[Flags]
public enum GDAL_OF : uint
{
	/// <summary> Open in read-only mode.</summary>
	READONLY = 0,
	/// <summary> Allow raster and vector drivers to be used. </summary>
	ALL = 0,
	/// <summary> Open in update mode. </summary>
	UPDATE = 1,
	/// <summary> Allow raster drivers to be used. </summary>
	RASTER = 2,
	/// <summary> Allow vector drivers to be used. </summary>
	VECTOR = 4,
	/// <summary> Allow gnm drivers to be used. </summary>
	GNM = 8,
	/// <summary> Allow multidimensional raster drivers to be used. </summary>
	MULTIDIM_RASTER = 0x10,
	/// <summary> Open in shared mode. </summary>
	SHARED = 0x20,
	/// <summary> Emit error message in case of failed open. </summary>
	VERBOSE_ERROR = 0x40,
	/// <summary>Open as internal dataset.<para/>
	/// Such dataset isn't registered in the global list of opened dataset. Cannot be used with <see cref="SHARED"/>.
	/// </summary>
	INTERNAL = 0x80,
	/// <summary> Let GDAL decide if a array-based or hashset-based storage strategy for cached blocks must be used.<para/>
	/// <see cref="DEFAULT_BLOCK_ACCESS"/>, <see cref="ARRAY_BLOCK_ACCESS"/> and <see cref="HASHSET_BLOCK_ACCESS"/> are mutually exclusive. </summary>
	DEFAULT_BLOCK_ACCESS = 0,
	/// <summary> Use a array-based storage strategy for cached blocks.<para/>
	/// <see cref="DEFAULT_BLOCK_ACCESS"/>, <see cref="ARRAY_BLOCK_ACCESS"/> and <see cref="HASHSET_BLOCK_ACCESS"/> are mutually exclusive. </summary>
	ARRAY_BLOCK_ACCESS = 0x100,
	/// <summary> Use a hashset-based storage strategy for cached blocks.<para/>
	/// <see cref="DEFAULT_BLOCK_ACCESS"/>, <see cref="ARRAY_BLOCK_ACCESS"/> and <see cref="HASHSET_BLOCK_ACCESS"/> are mutually exclusive. </summary>
	HASHSET_BLOCK_ACCESS = 0x200,
	/// <summary>
	/// Open in thread-safe mode.<para/>
	/// Not compatible with <see cref="VECTOR"/>, <see cref="MULTIDIM_RASTER"/> or <see cref="UPDATE"/></summary>
	THREAD_SAFE = 0x800
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
