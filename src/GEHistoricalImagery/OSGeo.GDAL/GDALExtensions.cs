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
}
