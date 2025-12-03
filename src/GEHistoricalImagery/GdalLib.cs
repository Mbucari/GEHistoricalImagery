using Microsoft.VisualBasic;
using OSGeo.GDAL;
using System.Runtime.InteropServices;
using System.Text;

namespace GEHistoricalImagery;

internal class GdalLib
{
	private static bool _isRegistered = false;
	public static void Register(int maxCache = 1024 * 1024 * 300)
	{
		if (_isRegistered)
			return;

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
		_isRegistered = true;
	}

	/// <summary>
	/// Gdal.SetConfigOption only accepts string arguments which are marshalled
	/// as ANSI strings; however, GDAL expects UTF-8 encoded strings. Manually
	/// encode the strings to UTF-8 and pass the encoded bytes to the native method.
	/// </summary>
	private static void SetConfigOption(string name, string value)
	{
		int size = Encoding.UTF8.GetByteCount(value);
		var ut8Bytes = new byte[size + 1];
		Encoding.UTF8.GetBytes(value, ut8Bytes);
		SetConfigOption(name, ut8Bytes);
	}

	[DllImport("gdal_wrap", EntryPoint = "CSharp_OSGeofGDAL_SetConfigOption___")]
	public static extern void SetConfigOption(string jarg1, byte[] jarg2);
}
