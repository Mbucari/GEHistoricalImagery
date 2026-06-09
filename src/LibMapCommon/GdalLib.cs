using OSGeo.GDAL;
using System.Runtime.CompilerServices;

namespace LibMapCommon;

public class GdalLib
{
	private static bool _isRegistered = false;
	static GdalLib()
	{
		Register();
	}
	public static void Register(int maxCache = 1024 * 1024 * 300)
	{
		if (Interlocked.CompareExchange(ref _isRegistered, true, false))
			return;

#if DEBUG
		Environment.SetEnvironmentVariable("GDAL_DATA", AppContext.BaseDirectory);
		//Gdal.SetConfigOption("CPL_DEBUG", "ON");
#endif

		string gdalData = Environment.GetEnvironmentVariable("GDAL_DATA") ?? AppContext.BaseDirectory;
		string geoTiffCsv = Environment.GetEnvironmentVariable("GEOTIFF_CSV") ?? gdalData;
		string projLib = Environment.GetEnvironmentVariable("PROJ_LIB") ?? gdalData;
		string certFile = Environment.GetEnvironmentVariable("GDAL_CURL_CA_BUNDLE") ?? Path.Combine(gdalData, "curl-ca-bundle.crt");
		Gdal.SetConfigOption("GDAL_DATA", gdalData);
		Gdal.SetConfigOption("GEOTIFF_CSV", geoTiffCsv);
		Gdal.SetConfigOption("PROJ_LIB", projLib);
		Gdal.SetConfigOption("GDAL_CURL_CA_BUNDLE", certFile);
		Gdal.SetConfigOption("GDAL_PAM_ENABLED", "NO");

        Gdal.AllRegister();
        Gdal.SetCacheMax(maxCache);
    }

	public static IEnumerable<Driver> EnumerateRasterDrivers()
	{
		int driverCount = Gdal.GetDriverCount();
		for (int i = 0; i < driverCount; i++)
		{
			yield return Gdal.GetDriver(i);
		}
	}

	public static IEnumerable<OSGeo.OGR.Driver> EnumerateVectorDrivers()
	{
		int driverCount = OSGeo.OGR.Ogr.GetDriverCount();
		for (int i = 0; i < driverCount; i++)
		{
			yield return OSGeo.OGR.Ogr.GetDriver(i);
		}
	}
}
