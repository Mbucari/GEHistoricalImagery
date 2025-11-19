using CommandLine;
using LibEsri;
using LibGoogleEarth;
using LibMapCommon;
using LibMapCommon.Geometry;
using OSGeo.GDAL;

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

	[Option("target-sr", HelpText = "Warp image to Spatial Reference. Either EPSG:#### or path to projection file (file system or web)", MetaValue = "[SPATIAL REFERENCE]", Default = null)]
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
			var aoi = webMerc.ToPixelRegion(ZoomLevel);
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

	private async Task<TileDataset<WebMercator>> DownloadTile(PixelRegion aoi, WayBack wayBack, EsriTile tile, DateOnly desiredDate)
	{
		try
		{
			EsriTile gotTile = tile;
			DatedEsriTile? node;
			while ((node = await wayBack.GetNearestDatedTileAsync(gotTile, desiredDate)) is null &&
				tile.Level - gotTile.Level < 2 && gotTile.Level >= 2)
			{
				gotTile = EsriTile.Create(gotTile.Row / 2, gotTile.Column / 2, gotTile.Level - 1);
			}

			if (node is null)
				return EmptyDataset(tile);

			var imageBts = await wayBack.DownloadTileAsync(node.Layer, node.Tile);
			var dataset = OpenDataset(imageBts);
			var message = node.CaptureDate == desiredDate ? null : $"Substituting imagery from {DateString(node.CaptureDate)} for tile at {tile.Wgs84Center}";

			if (gotTile.Level != tile.Level)
			{
				dataset = ResizeTile(gotTile, dataset, tile);
				message = $"Substituting level {gotTile.Level} imagery from {DateString(node.CaptureDate)} for tile at {tile.Wgs84Center}";
			}

			dataset = TrimDataset(dataset, aoi, tile);

			return new()
			{
				Tile = tile,
				Message = message,
				Dataset = dataset
			};
		}
		catch (HttpRequestException)
		{ /* Failed to get a dated tile image. This wile will be black in the final image. */ }

		return EmptyDataset(tile);
	}

	private async Task<TileDataset<WebMercator>> DownloadTile(PixelRegion aoi, WayBack wayBack, EsriTile tile, Layer layer)
	{
		EsriTile gotTile = tile;

		while (tile.Level - gotTile.Level < 2 && gotTile.Level >= 2)
		{
			try
			{
				var imageBts = await wayBack.DownloadTileAsync(layer, gotTile);
				var dataset = OpenDataset(imageBts);
				string? message = null;

				if (gotTile.Level != tile.Level)
				{
					dataset = ResizeTile(gotTile, dataset, tile);
					message = $"Substituting level {gotTile.Level} imagery from {layer.Title} for tile at {tile.Wgs84Center}";
				}

				dataset = TrimDataset(dataset, aoi, tile);

				return new()
				{
					Tile = tile,
					Message = message,
					Dataset = dataset
				};
			}
			catch (HttpRequestException)
			{ /* Failed to get a dated tile image. Try to find next level up. */ }

			gotTile = EsriTile.Create(gotTile.Row / 2, gotTile.Column / 2, gotTile.Level - 1);
		}

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
			var aoi = Region.ToPixelRegion(ZoomLevel);
			Console.Write("Grabbing Image Tiles: ");
			ReportProgress(0);

			return Region.GetTiles<KeyholeTile>(ZoomLevel).Select(t => Task.Run(() => DownloadTile(aoi, root, t, desiredDate)));
		}
	}

	private async Task<TileDataset<Wgs1984>> DownloadTile(PixelRegion aoi, DbRoot root, KeyholeTile tile, DateOnly desiredDate)
	{
		KeyholeTile gotTile = tile;
		TileNode? node;

		while ((node = await root.GetNodeAsync(gotTile)) is null &&
				tile.Level - gotTile.Level < 2 && gotTile.Level >= 2)
		{
			gotTile = KeyholeTile.Create(gotTile.Row / 2, gotTile.Column / 2, gotTile.Level - 1);
		}

		if (node is null)
			return EmptyDataset(tile);

		foreach (var dt in node.GetAllDatedTiles().OrderBy(d => int.Abs(desiredDate.DayNumber - d.Date.DayNumber)))
		{
			try
			{
				byte[]? imageBts = await root.GetEarthAssetAsync(dt);
				if (imageBts == null)
					continue;

				var dataset = OpenDataset(imageBts);
				string? message = null;

				if (gotTile.Level != tile.Level)
				{
					dataset = ResizeTile(gotTile, dataset, tile);
					message = dt.Date == default
						? $"Substituting level {gotTile.Level} default imagery of unknown date for tile at {tile.Wgs84Center}"
						: $"Substituting level {gotTile.Level} imagery from {DateString(dt.Date)} for tile at {tile.Wgs84Center}";
				}
				else if (dt.Date != desiredDate)
					message = dt.Date == default
						? $"Substituting default imagery of unknown date for tile at {tile.Wgs84Center}"
						: $"Substituting imagery from {DateString(dt.Date)} for tile at {tile.Wgs84Center}";

				dataset = TrimDataset(dataset, aoi, tile);

				return new()
				{
					Tile = tile,
					Dataset = dataset,
					Message = message
				};
			}
			catch (HttpRequestException)
			{ /* Failed to get a dated tile image. Try again with the next nearest date.*/ }
		}

		return EmptyDataset(tile);
	}

	#endregion

	#region Common

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

	private async Task Run_Common<T>(FileInfo saveFile, DateOnly desiredDate, GeoRegion<T> region, double tileCount, IEnumerable<Task<TileDataset<T>>> generator)
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

			//Release the lock for either saving or deleting
			fileLock.Dispose();

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

	private Dataset OpenDataset(byte[] jpgBytes)
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

	private Dataset TrimDataset<T>(Dataset image, PixelRegion aoi, ITile<T> tile)
		where T : IGeoCoordinate<T>
	{
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

	private void Image_Saving(object? sender, ImageSaveEventArgs e)
	{
		ReportProgress(e.Progress);
	}

	private static Dataset CreateMissingTile()
	{
		//The jpeg driver fails when a large number of empty tiles are written.
		//Empirically determined that three, non-zero pixels on the top and left
		//side of each tile is enough to successfully compress the jpeg.
		int pixelSpacing = (int)Math.Ceiling(TILE_SIZE / 3d);

		var image = new TileImage(TILE_SIZE, TILE_SIZE, NUM_BANDS);
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

		public TileImage(int width, int height, int bandCount)
		{
			BandCount = bandCount;
			BandMap = Enumerable.Range(1, BandCount).ToArray();
			RasterX = width;
			RasterY = height;
			ImageBytes = GC.AllocateUninitializedArray<byte>(RasterX * RasterY * BandCount);
		}

		public Dataset ToDataset()
		{
			using var tifDriver = Gdal.GetDriverByName("MEM");
			var memDataset = tifDriver.Create("", RasterX, RasterY, BandCount, DataType.GDT_Byte, null);
			memDataset.WriteRaster(0, 0, RasterX, RasterY, ImageBytes, RasterX, RasterY, BandCount, BandMap, BandCount, RasterY * BandCount, 1);
			return memDataset;
		}

		public byte[] GetPixel(int x, int y)
		{

			ArgumentOutOfRangeException.ThrowIfNegative(x, nameof(x));
			ArgumentOutOfRangeException.ThrowIfNegative(y, nameof(y));
			ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, RasterX, nameof(x));
			ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, RasterY, nameof(y));
			var index = (y * RasterY + x) * BandCount;
			var pixel = new byte[BandCount];
			for (int i = 0; i < BandCount; i++)
			{
				if (BandMap[i] != 0)
					pixel[i] = ImageBytes[index + i];
			}
			return pixel;
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
