using CommandLine;
using GEHistoricalImagery.Kml;
using LibEsri;
using LibGoogleEarth;
using LibMapCommon;
using LibMapCommon.Geometry;

namespace GEHistoricalImagery.Cli;

internal abstract class AoiVerb : OptionsBase
{
	[Option("region-file", SetName = "Region-File", HelpText = "Path to a kmz or kml file containing the region geometry (polygon or polyline with at least three vertices)", MetaValue = "/path/to/kmzfile.kmz")]
	public string? RegionFile { get; set; }

	[Option("region", SetName = "Region", Separator = '+', HelpText = "A list of geographic coordinates which are the vertices of the polygonal area of interest. Vertex coordinates delimiter with a '+'. ", MetaValue = "Lat0,Long0+Lat1,Long1+Lat2,Long2")]
	public IList<string>? RegionCoordinates { get; set; }

	[Option("lower-left", SetName = "Rectangle-Corners", HelpText = "Geographic coordinate of the lower-left (southwest) corner of the rectangular area of interest.", MetaValue = "LAT,LONG")]
	public Wgs1984? LowerLeft { get; set; }

	[Option("upper-right", SetName = "Rectangle-Corners", HelpText = "Geographic coordinate of the upper-right (northeast) corner of the rectangular area of interest.", MetaValue = "LAT,LONG")]
	public Wgs1984? UpperRight { get; set; }

	[Option('z', "zoom", HelpText = "Zoom level [1-23]", MetaValue = "N", Required = true)]
	public int ZoomLevel { get; set; }

	[Option('p', "parallel", HelpText = $"(Default: ALL_CPUS) Number of concurrent downloads", MetaValue = "N")]
	public int ConcurrentDownload { get; set; }

	protected GeoRegion<Wgs1984> Region { get; set; } = null!;

	protected bool AnyAoiErrors()
	{
		var errors = GetAoiErrors().ToList();
		errors.ForEach(Console.Error.WriteLine);
		return errors.Count > 0;
	}

	protected IEnumerable<string> GetAoiErrors()
	{
		if (ConcurrentDownload <= 0)
			ConcurrentDownload = Environment.ProcessorCount;
		ConcurrentDownload = Math.Min(ConcurrentDownload, Environment.ProcessorCount);

		if (ZoomLevel > 23)
			yield return $"Zoom level: {ZoomLevel} is too large. Max zoom is 23";
		else if (ZoomLevel < 1)
			yield return $"Zoom level: {ZoomLevel} is too small. Min zoom is 1";

		if (RegionFile != null)
		{
			var placemarks = Placemark.LoadFromKeyhole(RegionFile)?.Where(p => p.Type is PlacemarkType.LineString or PlacemarkType.Polygon).ToArray();
			if (placemarks is null)
				yield return "Invalid KMZ file";
			else
			{
				var placemarkOptions = placemarks.Select(p => new PlacemarkOption(p)).Where(p => p.AreaSquareMeters > 0).ToArray();
				if (placemarkOptions.Length == 0)
				{
					yield return "Keyhole file doesn't contain any enclosed regions";
				}
				else if (placemarkOptions.Length == 1)
				{
					Region = GeoRegion<Wgs1984>.Create(placemarks[0].Coordinates);
				}
				else
				{
					var prompt = "Select which placemark to use as the region of interest";
					Console.WriteLine(prompt);
					Console.WriteLine(new string('=', prompt.Length));

					var placemark = OptionChooser<PlacemarkOption>.WaitForOptions(placemarkOptions)?.Placemark;
					if (placemark is null)
						yield return "No placemark was selected";
					else
						Region = GeoRegion<Wgs1984>.Create(placemark.Coordinates);
				}
			}
		}
		else if (RegionCoordinates?.Count >= 3)
		{
			var converter = new Wgs1984TypeConverter();
			var coords = new Wgs1984[RegionCoordinates.Count];
			for (int i = 0; i < RegionCoordinates.Count; i++)
			{
				if (converter.ConvertFrom(RegionCoordinates[i]) is not Wgs1984 coord)
				{
					yield return $"Invalid coordinate '{RegionCoordinates[i]}'";
					yield break;
				}
				coords[i] = coord;
			}

			Region = GeoRegion<Wgs1984>.Create(coords);
		}
		else if (LowerLeft is null && UpperRight is null)
			yield return "An area of interest must be specified either with the 'region' option or the 'lower-left' and 'upper-right' options";
		else if (LowerLeft is null)
			yield return $"Invalid lower-left coordinate.{Environment.NewLine} Location must be in decimal Lat,Long. e.g. 37.58289,-106.52305";
		else if (UpperRight is null)
			yield return $"Invalid upper-right coordinate.{Environment.NewLine} Location must be in decimal Lat,Long. e.g. 37.58289,-106.52305";
		else
		{
			string? errorMessage = null;
			try
			{
				var llX = LowerLeft.Value.Longitude;
				var llY = LowerLeft.Value.Latitude;
				var urX = UpperRight.Value.Longitude;
				var urY = UpperRight.Value.Latitude;
				if (urX < llX)
					urX += 360;

				Region = GeoRegion<Wgs1984>.Create(
					new Wgs1984(llY, llX),
					new Wgs1984(urY, llX),
					new Wgs1984(urY, urX),
					new Wgs1984(llY, urX));
			}
			catch (Exception e)
			{
				errorMessage = $"Invalid rectangle.{Environment.NewLine} {e.Message}";
			}
			if (errorMessage != null)
				yield return errorMessage;
		}

		if (Region is not null)
		{
			TileStats rectStats;
			if (Provider is Provider.Wayback)
			{
				var webMerc = Region.ToWebMercator();
				rectStats = webMerc.GetRectangularRegionStats<EsriTile>(ZoomLevel);
			}
			else
				rectStats = Region.GetRectangularRegionStats<KeyholeTile>(ZoomLevel);

			if (rectStats.TileCount < 100_000)
				yield break;

			string? result;
			do
			{
				Console.Write("""
				Warning: the specified region and zoom level spans {0} tiles. This could take a very long time and may fail. Recommended to either decrease the zoom level or shrink the area of interest."
				Do you want to continue? (y/N)  
				""", rectStats.TileCount.ToString("N0"));
				result = Console.ReadLine()?.Trim().ToLower();
			}
			while (result is not ("y" or "n" or "" or null));
			if (result != "y")
				yield return "Aborting due to user request";
		}
	}

	private class PlacemarkOption : IConsoleOption
	{
		public string DisplayValue { get; }
		public Placemark Placemark { get; }
		public double AreaSquareMeters { get; }

		public PlacemarkOption(Placemark placemark)
		{
			Placemark = placemark;
			AreaSquareMeters = placemark.GetArea();
			DisplayValue = $"<{placemark.Type} '{placemark.Name}' ({AreaString(AreaSquareMeters)})>";
		}

		private static string AreaString(double squareMeters)
		{
			if (squareMeters < 1000000)
			{
				if (squareMeters < 100)
					return $"{squareMeters:N2} m^2";
				else if (squareMeters < 1_000)
					return $"{squareMeters:N1} m^2";
				return $"{squareMeters:N0} m^2";
			}
			else
			{
				var squareKm = squareMeters / 1_000_000;
				if (squareKm < 10)
					return $"{squareKm:N3} km^2";
				else if (squareKm < 100)
					return $"{squareKm:N2} km^2";
				else if (squareKm < 1_000)
					return $"{squareKm:N1} km^2";
				return $"{squareKm:N0} km^2";
			}
		}

		public bool DrawOption() => true;
	}

	/// <summary>
	/// Enumerate the tiles that intersect with the AOI region.  Progress is reported as the tiles are being enumerated.
	/// </summary>
	protected IEnumerable<TTile> EnumerateTiles<TTile, TCoordinate>(GeoRegion<TCoordinate> region)
		where TTile : ITile<TTile, TCoordinate>
		where TCoordinate : IGeoCoordinate<TCoordinate>
	{
		var allRectStats = region.Polygons.Select(p => p.GetRectangularRegionStats<TTile>(ZoomLevel)).ToArray();
		double totalTileCount = allRectStats.Sum(s => s.TileCount);
		Console.Write("Finding Tiles Inside Region: ");
		ReportProgress(0);
		long numTilesChecked = 0;

		for (int i = 0; i < allRectStats.Length; i++)
		{
			var polygon = region.Polygons[i];
			var stats = allRectStats[i];
			var polygonTiles = polygon.EnumerateTiles<TTile>(stats, () =>
			{
				var progress = ++numTilesChecked / totalTileCount;
				ReportProgress(progress);
			});
			foreach (var tile in polygonTiles)
					yield return tile;
		}
		ReplaceProgress("Done!" + Environment.NewLine);
	}
}
