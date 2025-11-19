using OSGeo.GDAL;

namespace GEHistoricalImagery;

internal class GdalLib
{
	private static bool _isRegistered = false;
	public static void Register(int maxCache = 1024 * 1024 * 300)
	{
		if (_isRegistered)
			return;

#if LINUX
		Gdal.AllRegister();
#else
		GdalConfiguration.ConfigureGdal();
#endif
		Gdal.SetCacheMax(maxCache);
		_isRegistered = true;
	}
}
