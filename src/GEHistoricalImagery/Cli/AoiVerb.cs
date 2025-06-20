using CommandLine;
using GEHistoricalImagery.Kml;
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

	protected GeoRegion<Wgs1984> Region { get; set; } = null!;

	protected IEnumerable<string> GetAoiErrors()
	{
		if (ZoomLevel > 23)
			yield return $"Zoom level: {ZoomLevel} is too large. Max zoom is 23";
		else if (ZoomLevel < 1)
			yield return $"Zoom level: {ZoomLevel} is too small. Min zoom is 1";

		if (RegionFile != null)
		{
			var placemarks = Placemark.LoadFromKeyhole(RegionFile)?.Where(p => p.Type is PlacemarkType.LineString or PlacemarkType.Polygon).ToArray();
			if (placemarks is null)
				yield return "Invalid KMZ file";
			else if (placemarks.Length == 0)
				yield return "Keyhole file doesn't contain any enclosed regions";
			else
			{
				if (placemarks.Length == 1)
					Region = GeoRegion<Wgs1984>.Create(placemarks[0].Coordinates);
				else
				{
					var prompt = "Select which placemark to use as the region of interest";
					Console.WriteLine(prompt);
					Console.WriteLine(new string('=', prompt.Length));

					var placemark
						= OptionChooser<PlacemarkOption>
						.WaitForOptions(placemarks.Select(p => new PlacemarkOption(p)).ToArray())
						?.Placemark;

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
			yield return "Invalid lower-left coordinate.\r\n Location must be in decimal Lat,Long. e.g. 37.58289,-106.52305";
		else if (UpperRight is null)
			yield return "Invalid upper-right coordinate.\r\n Location must be in decimal Lat,Long. e.g. 37.58289,-106.52305";
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
				errorMessage = $"Invalid rectangle.\r\n {e.Message}";
			}
			if (errorMessage != null)
				yield return errorMessage;
		}
	}

	private class PlacemarkOption : IConsoleOption
	{
		public string DisplayValue { get; }
		public Placemark Placemark { get; }

		public PlacemarkOption(Placemark placemark)
		{
			Placemark = placemark;
			var area = placemark.GetArea() / 1000000;
			DisplayValue = $"<{placemark.Type} '{placemark.Name}' ({area:F2} km^2)>";
		}

		public bool DrawOption() => true;
	}
}
