using CommandLine;
using LibEsri;
using LibGoogleEarth;
using LibMapCommon;
using LibMapCommon.Geometry;
using OSGeo.GDAL;
using System.ComponentModel;

namespace GEHistoricalImagery.Cli;

[Verb("download", HelpText = "Download historical imagery")]
internal class Download : AoiVerb
{
	[Option('d', "date", HelpText = "Imagery Date", MetaValue = "yyyy/MM/dd", Required = true)]
	public DateOnly? Date { get; set; }

	[Option("layer-date", HelpText = "(Wayback only) The date specifies a layer instead of an image capture date")]
	public bool LayerDate { get; set; }

	[Option('o', "output", HelpText = "Output GeoTiff save location", MetaValue = "out.tif", Required = true)]
	public string? SavePath { get; set; }

	[Option('p', "parallel", HelpText = $"(Default: ALL_CPUS) Number of concurrent downloads", MetaValue = "N")]
	public int ConcurrentDownload { get; set; }

	[Option("target-sr", HelpText = "Warp image to Spatial Reference", MetaValue = "https://epsg.io/1234.wkt", Default = null)]
	public string? TargetSpatialReference { get; set; }

	[Option("scale", HelpText = "Geo transform scale factor", MetaValue = "S", Default = 1d)]
	public double ScaleFactor { get; set; }

	[Option("offset-x", HelpText = "Geo transform X offset", MetaValue = "X", Default = 0d)]
	public double OffsetX { get; set; }

	[Option("offset-y", HelpText = "Geo transform Y offset", MetaValue = "Y", Default = 0d)]
	public double OffsetY { get; set; }

	[Option("scale-first", HelpText = "Perform scaling before offsetting X and Y", Default = false)]
	public bool ScaleFirst { get; set; }

	public override async Task RunAsync()
	{
		bool hasError = false;

		foreach (var errorMessage in GetAoiErrors())
		{
			Console.Error.WriteLine(errorMessage);
			hasError = true;
		}

		if (Date is null)
		{
			Console.Error.WriteLine("Invalid imagery date");
			hasError = true;
		}

		if (string.IsNullOrWhiteSpace(SavePath))
		{
			Console.Error.WriteLine("Invalid output file");
			hasError = true;
		}

		if (ConcurrentDownload <= 0)
			ConcurrentDownload = Environment.ProcessorCount;

		if (hasError) return;

		FileInfo saveFile;
		try
		{
			//Try to create the output file so any problems will cause early failure
			saveFile = new FileInfo(SavePath!);
			saveFile.Create().Dispose();
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Error saving file {SavePath}");
			Console.Error.WriteLine($"\t{ex.Message}");
			return;
		}

		var desiredDate = Date!.Value;
		var task = Provider is Provider.Wayback ? Run_Esri(saveFile, desiredDate)
			: Run_Keyhole(saveFile, desiredDate);

		await task;
	}

	#region Esri

	private async Task Run_Esri(FileInfo saveFile, DateOnly desiredDate)
	{
		var wayBack = await WayBack.CreateAsync(CacheDir);
		var webMerc = Region.ToWebMercator();
		var stats = webMerc.GetPolygonalRegionStats<EsriTile>(ZoomLevel);

		await Run_Common(saveFile, desiredDate, webMerc, stats.TileCount, generateWork());

		IEnumerable<Task<TileDataset<WebMercator>>> generateWork()
		{
			var aoi = webMerc.ToPixelPolygon(ZoomLevel);
			if (LayerDate)
			{
				var layer = wayBack.Layers.OrderBy(l => int.Abs(l.Date.DayNumber - desiredDate.DayNumber)).First();

				Console.Write($"Grabbing Image Tiles From {layer.Title}: ");
				ReportProgress(0);
				return webMerc.GetTiles<EsriTile>(ZoomLevel).Select(t => Task.Run(() => DownloadTile(aoi, wayBack, t, layer)));
			}
			else
			{
				Console.Write($"Grabbing Image Tiles Nearest To {DateString(desiredDate)}: ");
				ReportProgress(0);
				return webMerc.GetTiles<EsriTile>(ZoomLevel).Select(t => Task.Run(() => DownloadTile(aoi, wayBack, t, desiredDate)));
			}
		}
	}

	private async Task<TileDataset<WebMercator>> DownloadTile(PixelPointPoly aoi, WayBack wayBack, EsriTile tile, DateOnly desiredDate)
	{
		try
		{
			var dt = await wayBack.GetNearestDatedTileAsync(tile, desiredDate);
			if (dt is null)
				return EmptyDataset(tile);

			var bytes = await wayBack.DownloadTileAsync(dt.Layer, dt.Tile);

			return new()
			{
				Tile = tile,
				Message = dt.CaptureDate == desiredDate ? null : $"Substituting imagery from {DateString(dt.CaptureDate)} for tile at {tile.Wgs84Center}",
				Dataset = OpenDataset(aoi, tile, bytes)
			};
		}
		catch (HttpRequestException)
		{ /* Failed to get a dated tile image. This wile will be black in the final image. */ }

		return EmptyDataset(tile);
	}

	private async Task<TileDataset<WebMercator>> DownloadTile(PixelPointPoly aoi, WayBack wayBack, EsriTile tile, Layer layer)
	{
		try
		{
			var bytes = await wayBack.DownloadTileAsync(layer, tile);

			return new()
			{
				Tile = tile,
				Message = null,
				Dataset = OpenDataset(aoi, tile, bytes)
			};
		}
		catch (HttpRequestException)
		{ /* Failed to get a dated tile image. This wile will be black in the final image. */ }

		return EmptyDataset(tile);
	}
	#endregion

	#region Keyhole

	private async Task Run_Keyhole(FileInfo saveFile, DateOnly desiredDate)
	{
		var root = await DbRoot.CreateAsync(Database.TimeMachine, CacheDir);
		var stats = Region.GetPolygonalRegionStats<KeyholeTile>(ZoomLevel);

		await Run_Common(saveFile, desiredDate, Region, stats.TileCount, generateWork());

		IEnumerable<Task<TileDataset<Wgs1984>>> generateWork()
		{
			var aoi = Region.ToPixelPolygon(ZoomLevel);
			Console.Write("Grabbing Image Tiles: ");
			ReportProgress(0);

			return Region.GetTiles<KeyholeTile>(ZoomLevel).Select(t => Task.Run(() => DownloadTile(aoi, root, t, desiredDate)));
		}
	}

	private async Task<TileDataset<Wgs1984>> DownloadTile(PixelPointPoly aoi, DbRoot root, KeyholeTile tile, DateOnly desiredDate)
	{
		if (await root.GetNodeAsync(tile) is not TileNode node)
			return EmptyDataset(tile);

		foreach (var dt in node.GetAllDatedTiles().OrderBy(d => int.Abs(desiredDate.DayNumber - d.Date.DayNumber)))
		{
			try
			{
				byte[]? imageBts = await root.GetEarthAssetAsync(dt);
				if (imageBts == null)
					continue;

				return new()
				{
					Tile = tile,
					Dataset = OpenDataset(aoi, tile, imageBts),
					Message = dt.Date == desiredDate ? null : $"Substituting imagery from {DateString(dt.Date)} for tile at {tile.Wgs84Center}"
				};
			}
			catch (HttpRequestException)
			{ /* Failed to get a dated tile image. Try again with the next nearest date.*/ }
		}

		return EmptyDataset(tile);
	}

	#endregion

	#region Common

	private async Task Run_Common<T>(FileInfo saveFile, DateOnly desiredDate, GeoPolygon<T> region, double tileCount, IEnumerable<Task<TileDataset<T>>> generator)
		where T : IGeoCoordinate<T>
	{
		var tempFile = Path.GetTempFileName();
		int numTilesProcessed = 0;
		int numTilesDownload = 0;
		var processor = new ParallelProcessor<TileDataset<T>>(ConcurrentDownload);

		try
		{
			using var image = new EarthImage<T>(region, ZoomLevel, tempFile);
			using Dataset? missingTile = CreateMissingTile();

			await foreach (var tds in processor.EnumerateResults(generator))
				using (tds)
				{
					image.AddTile(tds.Tile, tds.Dataset ?? missingTile);
					numTilesDownload++;

					if (tds.Message is not null)
						Console.Error.WriteLine($"\r\n{tds.Message}");

					ReportProgress(++numTilesProcessed / tileCount);
				}

			ReplaceProgress("Done!\r\n");
			Console.WriteLine($"{numTilesDownload} out of {tileCount} downloaded");

			if (numTilesDownload == 0)
			{
				if (saveFile.Exists)
					saveFile.Delete();
				return;
			}

			Console.Write("Saving Image: ");
			Progress = 0;

			image.Saving += Image_Saving;
			image.Save(saveFile.FullName, TargetSpatialReference, ConcurrentDownload, ScaleFactor, OffsetX, OffsetY, ScaleFirst);
			ReplaceProgress("Done!\r\n");
		}
		finally
		{
			if (File.Exists(tempFile))
				File.Delete(tempFile);
		}
	}

	private Dataset OpenDataset<TTile, T>(PixelPointPoly aoi, ITile<TTile, T> tile, byte[] jpgBytes)
		where T : IGeoCoordinate<T>
	{
		const GDAL_OF openOptions = GDAL_OF.RASTER | GDAL_OF.INTERNAL | GDAL_OF.READONLY;
		string memFile = $"/vsimem/{Guid.NewGuid()}.jpeg";

		try
		{
			Gdal.FileFromMemBuffer(memFile, jpgBytes);

			var image =  Gdal.OpenEx(memFile, (uint)openOptions, ["JPEG"], null, []);
			
			if (aoi.PolygonIntersects(tile.GetGeoPolygon().ToPixelPolygon(tile.Level)))
			{
				//Blank all pixels outside of the region

				var gpx = tile.GetTopLeftPixel();

				var tileImage = new TileImage(image);
				var zeroPixel = Enumerable.Repeat((byte)0, image.RasterCount).ToArray();

				for (int y = 0; y < image.RasterYSize; y++)
				{
					for (int x = 0; x < image.RasterXSize; x++)
					{
						if (!aoi.ContainsPoint(new PixelPoint(aoi.ZoomLevel, gpx.X + x + 0.5, gpx.Y + y + 0.5)))
							tileImage.SetPixel(x, y, zeroPixel);
					}
				}
				image.Dispose();
				return tileImage.ToDataset();
			}
			else return image;
		}
		finally
		{
			Gdal.Unlink(memFile);
		}
	}

	private void Image_Saving(object? sender, ImageSaveEventArgs e)
	{
		ReportProgress(e.Progress);
	}

	private static Dataset CreateMissingTile()
	{
		//The jpeg driver fails when a large number of empty tiles are written.
		//Empirically determined that three, non-zero pixels on the top and left
		//side of each tile is enough to successfully compress the jpeg.
		const int TILE_SIZE = 256;
		const int NUM_BANDS = 3;
		int pixelSpacing = (int)Math.Ceiling(TILE_SIZE / 3d);

		using var memDriver = Gdal.GetDriverByName("MEM");
		using var emptyDataset = memDriver.Create("", TILE_SIZE, TILE_SIZE, NUM_BANDS, DataType.GDT_Byte, null);
		var image = new TileImage(emptyDataset);
		byte[] color = [0, 0, 1];
		for (int i = 0; i < TILE_SIZE; i += pixelSpacing)
		{
			image.SetPixel(0, i, color);
			image.SetPixel(i, 0, color);
		}
		return image.ToDataset();
	}

	private static TileDataset<T> EmptyDataset<T>(ITile<T> tile) where T : IGeoCoordinate<T> => new()
	{
		Tile = tile,
		Message = $"No imagery available for tile at {tile.Wgs84Center}"
	};

	private class TileDataset<T> : IDisposable where T : IGeoCoordinate<T>
	{
		public required ITile<T> Tile { get; init; }
		public Dataset? Dataset { get; init; }
		public required string? Message { get; init; }

		public void Dispose()
		{
			Dataset?.Dispose();
		}
	}

	internal class TileImage
	{
		private byte[] ImageBytes { get; }
		private int BandCount { get; }
		private int[] BandMap { get; }
		private int RasterX { get; }
		private int RasterY { get; }
		public TileImage(Dataset tile)
		{
			ArgumentNullException.ThrowIfNull(tile, nameof(tile));

			BandCount = tile.RasterCount;
			BandMap = Enumerable.Range(1, BandCount).ToArray();
			RasterX = tile.RasterXSize;
			RasterY = tile.RasterYSize;

			ImageBytes = GC.AllocateUninitializedArray<byte>(RasterX * RasterY * BandCount);
			tile.ReadRaster(0, 0, RasterX, RasterY, ImageBytes, RasterX, RasterY, BandCount, BandMap, BandCount, RasterY * BandCount, 1);
		}

		public Dataset ToDataset()
		{
			using var tifDriver = Gdal.GetDriverByName("MEM");
			var memDataset = tifDriver.Create("", RasterX, RasterY, BandCount, DataType.GDT_Byte, null);
			memDataset.WriteRaster(0, 0, RasterX, RasterY, ImageBytes, RasterX, RasterY, BandCount, BandMap, BandCount, RasterY * BandCount, 1);
			return memDataset;
		}

		public void SetPixel(int x, int y, byte[] values)
		{
			ArgumentNullException.ThrowIfNull(values, nameof(values));
			ArgumentOutOfRangeException.ThrowIfNegative(x, nameof(x));
			ArgumentOutOfRangeException.ThrowIfNegative(y, nameof(y));
			ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, RasterX, nameof(x));
			ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, RasterY, nameof(y));
			if (values.Length != BandCount)
				throw new ArgumentOutOfRangeException(nameof(values));

			var index = (y * RasterY + x) * BandCount;

			for (int i = 0; i < BandCount; i++)
			{
				if (BandMap[i] != 0)
					ImageBytes[index + i] = values[i];
			}
		}
	}
	#endregion
}
