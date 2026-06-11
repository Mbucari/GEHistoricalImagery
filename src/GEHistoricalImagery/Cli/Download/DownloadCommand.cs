using CommandLine;
using LibMapCommon;
using LibMapCommon.Geometry;
using OSGeo.GDAL;
using OSGeo.OGR;

namespace GEHistoricalImagery.Cli.Download;

[Verb("download", HelpText = "Download historical imagery to a single raster image")]
internal partial class DownloadCommand : FileDownloadVerb
{
	[Option('o', "output", HelpText = "Output raster image save location", MetaValue = "<file.ext>", Required = true)]
	public override string? SavePath { get; set; }

	[Option("of", HelpText = "GDAL raster file output format", Default = "GTiff", MetaValue = "<FORMAT>")]
	public string? OutputFormat { get; set; }

	[Option("co", HelpText = "GDAL raster file creation options", MetaValue = "<NAME>=<VALUE>")]
	public IEnumerable<string>? CreationOptions { get; set; }

	[Option("scale", HelpText = "Geo transform scale factor", MetaValue = "<S>", Default = 1d)]
	public double ScaleFactor { get; set; } = 1.0;

	[Option("offset-x", HelpText = "Geo transform X offset", MetaValue = "<X>", Default = 0d)]
	public double OffsetX { get; set; }

	[Option("offset-y", HelpText = "Geo transform Y offset", MetaValue = "<Y>", Default = 0d)]
	public double OffsetY { get; set; }

	[Option("scale-first", HelpText = "Perform scaling before offsetting X and Y", Default = false)]
	public bool ScaleFirst { get; set; }
	private RasterOptions RasterOptions { get; set; } = null!;

	protected override IEnumerable<string> GetValidationErrors()
	{
		foreach (var error in base.GetValidationErrors())
		{
			yield return error;
		}

		var format = GdalLib.EnumerateRasterDrivers().FirstOrDefault(d => d.ShortName.Equals(OutputFormat, StringComparison.OrdinalIgnoreCase));
		if (format is null)
		{
			yield return $"Output format {OutputFormat} is not supported by the GDAL build being used.";
		}
		else
		{
			RasterOptions
				= format.ShortName == RasterOptions.GTiff_Jpeg.DriverName ? RasterOptions.GTiff_Jpeg
				: format.ShortName == RasterOptions.COG_Jpeg.DriverName ? RasterOptions.COG_Jpeg
				: new RasterOptions(format.ShortName);
		}

		foreach (var option in CreationOptions ?? [])
		{
			var kvp = option.Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (kvp.Length == 2 && !string.IsNullOrEmpty(kvp[0]) && !string.IsNullOrEmpty(kvp[1]))
			{
				RasterOptions?.Options[kvp[0]] = kvp[1];
			}
			else
			{
				yield return $"Creation option '{option}' is not in the correct format. Must be in the format '<NAME>=<VALUE>'";
			}
		}
	}

	public override async Task RunAsync()
	{
		if (AnyValidationErrors())
			return;

		FileInfo saveFile;
		try
		{
			//Try to create the output file so any problems will cause early failure
			saveFile = new FileInfo(SavePath!.ReplaceUnixHomeDir());
			saveFile.Create().Dispose();
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Error saving file {SavePath}");
			Console.Error.WriteLine($"\t{ex.Message}");
			return;
		}
		var desiredDates = Dates!;
		var task = Provider is Provider.Wayback ? Run_Esri(saveFile, desiredDates)
			: Run_Keyhole(saveFile, desiredDates);

		await task;
	}

	private async Task Run_Common<T>(FileInfo saveFile, GeoRegion<T> region, double tileCount, IEnumerable<Task<TileDataset<T>>> generator)
		where T : IGeoCoordinate<T>
	{
		var tempFile = Path.GetTempFileName();
		int numTilesProcessed = 0;
		int numTilesDownload = 0;
		var processor = new ParallelProcessor<TileDataset<T>>(ConcurrentDownload);

		using var fileLock = saveFile.Create();
		try
		{
			using var image = new EarthImage<T>(region, ZoomLevel, tempFile);

			await foreach (var tds in processor.EnumerateResults(generator))
				using (tds)
				{
					if (tds.Dataset is not null)
					{
						image.AddTile(tds.Tile, tds.Dataset);
						numTilesDownload++;
					}

					if (tds.Message is not null)
						Console.Error.WriteLine(tds.Message);

					ProgressWriter.Instance.ReportProgress(++numTilesProcessed / tileCount);
				}

			ProgressWriter.Instance.EndProgress();
			Console.Error.WriteLine($"{numTilesDownload} out of {tileCount} downloaded");

			//Release the lock for either saving or deleting
			fileLock.Dispose();

			if (numTilesDownload == 0)
			{
				if (saveFile.Exists)
					saveFile.Delete();
				return;
			}

			ProgressWriter.Instance.BeginProgress("Saving Image: ");
			image.Saving += (_, e) => ProgressWriter.Instance.ReportProgress(e.Progress);
			image.Save(saveFile.FullName, RasterOptions, TargetSpatialReference, ConcurrentDownload, ScaleFactor, OffsetX, OffsetY, ScaleFirst);
			ProgressWriter.Instance.EndProgress();
		}
		finally
		{
			if (File.Exists(tempFile))
				File.Delete(tempFile);
		}
	}

	private static Dataset OpenDataset(byte[] jpgBytes)
	{
		const GDAL_OF openOptions = GDAL_OF.RASTER | GDAL_OF.INTERNAL | GDAL_OF.READONLY;
		string memFile = $"/vsimem/{Guid.NewGuid()}.jpeg";

		try
		{
			Gdal.FileFromMemBuffer(memFile, jpgBytes);
			return Gdal.OpenEx(memFile, (uint)openOptions, ["JPEG"], null, []);
		}
		finally
		{
			Gdal.Unlink(memFile);
		}
	}

	private static TileDataset<T> EmptyDataset<T>(ITile<T> tile, string? messageOverride = null) where T : IGeoCoordinate<T> => new(tile)
	{
		Message = messageOverride ?? $"No imagery available for tile at {tile.Wgs84Center}"
	};

	private static Dataset TrimDataset<T>(Dataset image, GeoRegion<T> aoi, ITile<T> tile) where T : IGeoCoordinate<T>
	{
		using Geometry tileg = tile.GetPolygon();

		if (!aoi.Overlaps(tileg))
			return image;

		using (image)
		{
			using Geometry intersection = aoi.Intersect(tileg);
			var geoXform = tile.GetGeoTransform();
			var minX = geoXform.UpperLeft_X + geoXform.PixelWidth / 2;
			var minY = geoXform.UpperLeft_Y + geoXform.PixelHeight / 2;

			using Geometry pt = new Geometry(wkbGeometryType.wkbPoint);
			var zeroPixel = new byte[image.RasterCount];
			var tileImage = new TileImage(image);
			for (int y = 0; y < image.RasterYSize; y++)
			{
				for (int x = 0; x < image.RasterXSize; x++)
				{
					pt.SetPoint_2D(0, minX + x * geoXform.PixelWidth, minY + y * geoXform.PixelHeight);
					if (!intersection.Contains(pt))
						tileImage.SetPixel(x, y, zeroPixel);
				}
			}
			return tileImage.ToDataset();
		}
	}

	private const int TILE_SIZE = 256;
	private const int NUM_BANDS = 3;
	private static Dataset ResizeTile(ITile gotTile, Dataset gotDataset, ITile tile)
	{
		var dimScale = 1 << (tile.Level - gotTile.Level);
		var diffX = tile.Column - gotTile.Column * dimScale;
		var diffY = tile.Row - gotTile.Row * dimScale;

		int side = TILE_SIZE / dimScale;

		int xstart = diffX * side, xend = xstart + side, ystart = diffY * side, yend = ystart + side;

		if (!gotTile.RowsIncreaseToSouth)
			(ystart, yend) = (TILE_SIZE - yend, TILE_SIZE - ystart);

		var image = new TileImage(gotDataset);
		var enlarged = new TileImage(TILE_SIZE, TILE_SIZE, NUM_BANDS);

		for (int xr = xstart, xw = 0; xr < xend; xr++, xw += dimScale)
		{
			for (int yr = ystart, yw = 0; yr < yend; yr++, yw += dimScale)
			{
				var pixel = image.GetPixel(xr, yr);

				for (int dx = 0; dx < dimScale; dx++)
				{
					for (int dy = 0; dy < dimScale; dy++)
					{
						enlarged.SetPixel(xw + dx, yw + dy, pixel);
					}
				}
			}
		}
		gotDataset.Dispose();
		return enlarged.ToDataset();
	}
}
