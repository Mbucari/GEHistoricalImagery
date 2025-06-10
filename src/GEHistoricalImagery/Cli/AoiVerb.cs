using CommandLine;
using Google.Protobuf.WellKnownTypes;
using LibMapCommon;
using LibMapCommon.Geometry;
using System.ComponentModel;

namespace GEHistoricalImagery.Cli;

internal abstract class AoiVerb : OptionsBase
{
	[Option("region", SetName = "Region", Separator = '+', HelpText = "A list of geographic coordinates which are the vertices of the polygonal area of interest. Vertex coordinates delimiter with a '+'. ", MetaValue = "Lat0,Long0+Lat1,Long1+Lat2,Long2")]
	public IList<string>? RegionCoordinates { get; set; }

	[Option("lower-left", SetName = "Rectangle-Corners", HelpText = "Geographic coordinate of the lower-left (southwest) corner of the rectangular area of interest.", MetaValue = "LAT,LONG")]
	public Wgs1984? LowerLeft { get; set; }

	[Option("upper-right", SetName = "Rectangle-Corners", HelpText = "Geographic coordinate of the upper-right (northeast) corner of the rectangular area of interest.", MetaValue = "LAT,LONG")]
	public Wgs1984? UpperRight { get; set; }

	[Option('z', "zoom", HelpText = "Zoom level [1-23]", MetaValue = "N", Required = true)]
	public int ZoomLevel { get; set; }

	protected GeoPolygon<Wgs1984> Region { get; set; } = null!;

	protected IEnumerable<string> GetAoiErrors()
	{
		if (ZoomLevel > 23)
			yield return $"Zoom level: {ZoomLevel} is too large. Max zoom is 23";
		else if (ZoomLevel < 1)
			yield return $"Zoom level: {ZoomLevel} is too small. Min zoom is 1";

		if (RegionCoordinates?.Count > 0)
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

			Region = new GeoPolygon<Wgs1984>(coords);
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
				Region = new GeoPolygon<Wgs1984>(
					LowerLeft.Value,
					new Wgs1984(UpperRight.Value.Latitude, LowerLeft.Value.Longitude),
					UpperRight.Value,
					new Wgs1984(LowerLeft.Value.Latitude, UpperRight.Value.Longitude));
			}
			catch (Exception e)
			{
				errorMessage = $"Invalid rectangle.\r\n {e.Message}";
			}
			if (errorMessage != null)
				yield return errorMessage;
		}
	}
}
