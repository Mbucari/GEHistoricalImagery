using CommandLine;
using Google.Protobuf.WellKnownTypes;
using LibMapCommon;
using LibMapCommon.Geometry;
using System.ComponentModel;

namespace GEHistoricalImagery.Cli;

internal abstract class AoiVerb : OptionsBase
{
	[Option("lower-left", SetName = "Rectangle-Corners", HelpText = "Geographic coordinate of the lower-left (southwest) corner of the rectangular area of interest.", MetaValue = "LAT,LONG")]
	public Wgs1984? LowerLeft { get; set; }

	[Option("upper-right", SetName = "Rectangle-Corners", HelpText = "Geographic coordinate of the upper-right (northeast) corner of the rectangular area of interest.", MetaValue = "LAT,LONG")]
	public Wgs1984? UpperRight { get; set; }

	[Option("region", SetName = "Region", Separator = '+', HelpText = "Geographic coordinate of the upper-right (northeast) corner of the rectangular area of interest.", MetaValue = "Lat0,Long0+Lat1,Long1+Lat2,Long2")]
	public IList<string>? RegionCoordinates { get; set; }

	[Option('z', "zoom", HelpText = "Zoom level [1-23]", MetaValue = "N", Required = true)]
	public int ZoomLevel { get; set; }

	protected Wgs1984Poly Region { get; set; } = null!;

	protected IEnumerable<string> GetAoiErrors()
	{
		if (ZoomLevel > 23)
			yield return $"Zoom level: {ZoomLevel} is too large. Max zoom is 23";
		else if (ZoomLevel < 1)
			yield return $"Zoom level: {ZoomLevel} is too small. Min zoom is 1";

		if (RegionCoordinates?.Count > 0)
		{
			var converter = TypeDescriptor.GetConverter(typeof(Wgs1984));
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

			Region = new Wgs1984Poly(coords);
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
				var aoi = new Rectangle(LowerLeft.Value, UpperRight.Value);
				Region = new Wgs1984Poly(aoi.LowerLeft, aoi.GetUpperLeft<Wgs1984>(), aoi.UpperRight, aoi.GetLowerRight<Wgs1984>());
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
