using LibMapCommon;
using OSGeo.GDAL;

namespace GEHistoricalImagery;

internal class RasterOptions
{
	public static RasterOptions Jpeg { get; } = new RasterOptions("JPEG", ("QUALITY", "75"), ("WRITE_EXIF_METADATA", "NO"));
	public static RasterOptions COG_Jpeg { get; } = new RasterOptions("COG", ("COMPRESS", "JPEG"), ("QUALITY", "75"));
	public static RasterOptions GTiff_Jpeg { get; } = new RasterOptions("GTiff", ("COMPRESS", "JPEG"), ("JPEG_QUALITY", "75"), ("PHOTOMETRIC", "YCBCR"), ("TILED", "TRUE"));
	public static RasterOptions GTiff_Deflate { get; } = new RasterOptions("GTiff", ("COMPRESS", "DEFLATE"), ("ZLEVEL", "1"), ("PREDICTOR", "2"), ("NUM_THREADS", "ALL_CPUS"));
	public string DriverName { get; }
	public Dictionary<string, string> Options { get; }
	public RasterOptions(string driverName, params (string key, string value)[] options)
	{
		DriverName = driverName;
		Options = options.ToDictionary(i => i.key, i => i.value);
	}

	public string[] GetCreationOptions(int cpuCount = 1)
	{
		var options = Options.Select(i => $"{i.Key}={i.Value}");
		if (cpuCount > 1)
			options = options.Append($"NUM_THREADS={cpuCount}");
		return options.ToArray();
	}

	public GDALWarpAppOptions GetWarpOptions<TSource>(string target_srs, int cpuCount = 1) where TSource : IGeoCoordinate<TSource>
	{
		List<string> parameters = [
			"-multi",
			"-wo", $"NUM_THREADS={cpuCount}",
			"-wo", "OPTIMIZE_SIZE=TRUE",
			"-ot", "Byte",
			"-of", DriverName,
			"-s_srs", $"EPSG:{TSource.EpsgNumber}",
			"-t_srs", target_srs,
			"-r", "cubic"];

		foreach (var option in GetCreationOptions(cpuCount))
		{
			parameters.AddRange(["-co", option]);
		}
		return new GDALWarpAppOptions(parameters.ToArray());
	}

}
