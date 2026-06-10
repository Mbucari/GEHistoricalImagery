using CommandLine;
using LibEsri;
using LibGoogleEarth;
using LibMapCommon;
using LibMapCommon.Geometry;

namespace GEHistoricalImagery.Cli;

internal abstract class AoiVerb : OptionsBase
{
	[Option('z', "zoom", HelpText = "Zoom level [1-23]", MetaValue = "<N>", Required = true)]
	public int ZoomLevel { get; set; }

	[Option("region-file", SetName = "Region-File", HelpText = "Path to a kmz or kml file containing the region geometry (polygon or polyline with at least three vertices)", MetaValue = "<file.kmz>")]
	public string? RegionFile { get; set; }

	[Option("region", SetName = "Region", Separator = '+', HelpText = "A list of geographic coordinates which are the vertices of the polygonal area of interest. Vertex coordinates delimited with a '+'. ", MetaValue = "<Lt0>,<Ln0>+<Lt1>,<Ln1>+...")]
	public IList<string>? RegionCoordinates { get; set; }

	[Option("lower-left", SetName = "Rectangle-Corners", HelpText = "Geographic coordinate of the lower-left (southwest) corner of the rectangular area of interest.", MetaValue = "<LAT>,<LONG>")]
	public Wgs1984? LowerLeft { get; set; }

	[Option("upper-right", SetName = "Rectangle-Corners", HelpText = "Geographic coordinate of the upper-right (northeast) corner of the rectangular area of interest.", MetaValue = "<LAT>,<LONG>")]
	public Wgs1984? UpperRight { get; set; }

	[Option('p', "parallel", HelpText = $"(Default: ALL_CPUS) Number of concurrent downloads", MetaValue = "<N>")]
	public int ConcurrentDownload { get; set; }

	protected GeoRegion<Wgs1984> Region { get; set; } = null!;
	static AoiVerb()
	{
		GdalLib.Register();
	}

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
			if (KmlFile.Parse(PathHelper.ReplaceUnixHomeDir(RegionFile)) is not { } kml)
				yield return "Invalid KMZ file";
			else
			{
				var placemarkOptions = kml.Placemarks.Where(p => p.GeodesicArea > 0).Select(p => new PlacemarkOption(p)).ToArray();
				if (placemarkOptions.Length == 0)
				{
					yield return "Keyhole file doesn't contain any enclosed regions";
				}
				else if (placemarkOptions.Length == 1 || this is IQuietCommand { Quiet: true })
				{
					Region = GeoRegion<Wgs1984>.Create(placemarkOptions[0].Placemark);
				}
				else
				{
					var prompt = "Select which placemark to use as the region of interest";
					Console.Error.WriteLine(prompt);
					Console.Error.WriteLine(new string('=', prompt.Length));

					var placemark = OptionChooser<PlacemarkOption>.WaitForOptions(placemarkOptions)?.Placemark;
					if (placemark is null)
						yield return "No placemark was selected";
					else
						Region = GeoRegion<Wgs1984>.Create(placemark);
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
			yield return "An area of interest must be specified either with 'region', 'region-file', or the 'lower-left' and 'upper-right' options";
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
				var webMerc = Region.Transform<WebMercator>();
				rectStats = webMerc.GetRectangularRegionStats<EsriTile>(ZoomLevel);
			}
			else
				rectStats = Region.GetRectangularRegionStats<KeyholeTile>(ZoomLevel);

			if (rectStats.TileCount < 100_000 || this is IQuietCommand { Quiet: true })
				yield break;

			string? result;
			do
			{
				Console.Error.Write("""
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

	protected EsriTile[] GetTiles(GeoRegion<WebMercator> region)
	{
		BeginProgress("Finding Tiles Inside Region: ");
		var regionTiles = region.EnumerateTiles<EsriTile>(ZoomLevel, ReportProgress).ToArray();
		ReplaceProgress();
		return regionTiles;
	}

	protected KeyholeTile[] GetTiles(GeoRegion<Wgs1984> region)
	{
		BeginProgress("Finding Tiles Inside Region: ");
		var regionTiles = region.EnumerateTiles<KeyholeTile>(ZoomLevel, ReportProgress).ToArray();
		ReplaceProgress();
		return regionTiles;
	}

	private class PlacemarkOption : IConsoleOption
	{
		public string DisplayValue { get; }
		public Placemark Placemark { get; }
		public double AreaSquareMeters => Placemark.GeodesicArea;

		public PlacemarkOption(Placemark placemark)
		{
			Placemark = placemark;
			DisplayValue = $"<{placemark.ParsedGeometryType.ToString().Remove(0,3)} '{placemark.Name}' ({AreaString(AreaSquareMeters)})>";
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
}
