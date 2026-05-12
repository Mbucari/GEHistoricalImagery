using OSGeo.GDAL;

namespace GEHistoricalImagery;

internal class GdalLib
{
	private static bool _isRegistered = false;
	public static void Register(int maxCache = 1024 * 1024 * 300)
	{
		if (Interlocked.CompareExchange(ref _isRegistered, true, false))
			return;

#if DEBUG
		Environment.SetEnvironmentVariable("GDAL_DATA", AppContext.BaseDirectory);
#endif

        string gdalData = Environment.GetEnvironmentVariable("GDAL_DATA") ?? AppContext.BaseDirectory;
		string geoTiffCsv = Environment.GetEnvironmentVariable("GEOTIFF_CSV") ?? gdalData;
		string projLib = Environment.GetEnvironmentVariable("PROJ_LIB") ?? gdalData;
		string certFile = Environment.GetEnvironmentVariable("GDAL_CURL_CA_BUNDLE") ?? Path.Combine(gdalData, "curl-ca-bundle.crt");
		Gdal.SetConfigOption("GDAL_DATA", gdalData);
		Gdal.SetConfigOption("GEOTIFF_CSV", geoTiffCsv);
		Gdal.SetConfigOption("PROJ_LIB", projLib);
		Gdal.SetConfigOption("GDAL_CURL_CA_BUNDLE", certFile);

        Gdal.AllRegister();
        Gdal.SetCacheMax(maxCache);
    }
}
