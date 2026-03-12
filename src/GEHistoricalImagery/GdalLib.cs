using OSGeo.GDAL;
using System.Runtime.InteropServices;

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
        SetConfigOption("GDAL_DATA", gdalData);
        SetConfigOption("GEOTIFF_CSV", geoTiffCsv);
        SetConfigOption("PROJ_LIB", projLib);
        SetConfigOption("GDAL_CURL_CA_BUNDLE", certFile);

        Gdal.AllRegister();
        Gdal.SetCacheMax(maxCache);
    }

    /// <summary>
    /// Gdal.GetConfigOption incorrectly marshals strings as ANSI.
    /// Use this method to correctly marshal <paramref name="pszDefault"/>
    /// and the return value as UTF-8 strings.
    /// </summary>
    [DllImport("gdal_wrap", EntryPoint = "CSharp_OSGeofGDAL_GetConfigOption___")]
    [return: MarshalAs(UnmanagedType.LPUTF8Str)]
    public static extern string GetConfigOption(string pszKey, [MarshalAs(UnmanagedType.LPUTF8Str)] string pszDefault);

    /// <summary>
    /// Gdal.SetConfigOption only accepts string arguments which are marshalled
    /// as ANSI strings; however, GDAL expects UTF-8 encoded strings. Use this
    /// method to correctly marshal <paramref name="pszValue"/> as a UTF-8 string
    /// </summary>
    [DllImport("gdal_wrap", EntryPoint = "CSharp_OSGeofGDAL_SetConfigOption___")]
    public static extern void SetConfigOption(string pszKey, [MarshalAs(UnmanagedType.LPUTF8Str)] string pszValue);
}
