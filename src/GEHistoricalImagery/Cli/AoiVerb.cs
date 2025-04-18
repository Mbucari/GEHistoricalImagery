﻿using CommandLine;
using Google.Protobuf.WellKnownTypes;
using LibMapCommon;

namespace GEHistoricalImagery.Cli;

internal abstract class AoiVerb : OptionsBase
{
	[Option("lower-left", Required = true, HelpText = "Geographic coordinate of the lower-left (southwest) corner of the rectangular area of interest.", MetaValue = "LAT,LONG")]
	public Wgs1984? LowerLeft { get; set; }

	[Option("upper-right", Required = true, HelpText = "Geographic coordinate of the upper-right (northeast) corner of the rectangular area of interest.", MetaValue = "LAT,LONG")]
	public Wgs1984? UpperRight { get; set; }

	[Option('z', "zoom", HelpText = "Zoom level [1-23]", MetaValue = "N", Required = true)]
	public int ZoomLevel { get; set; }

	protected Rectangle Aoi { get; private set; }

	protected IEnumerable<string> GetAoiErrors()
	{
		if (ZoomLevel > 23)
			yield return $"Zoom level: {ZoomLevel} is too large. Max zoom is 23";
		else if (ZoomLevel < 1)
			yield return $"Zoom level: {ZoomLevel} is too small. Min zoom is 1";

		if (LowerLeft is null)
			yield return "Invalid lower-left coordinate.\r\n Location must be in decimal Lat,Long. e.g. 37.58289,-106.52305";
		else if (UpperRight is null)
			yield return "Invalid upper-right coordinate.\r\n Location must be in decimal Lat,Long. e.g. 37.58289,-106.52305";
		else
		{
			string? errorMessage = null;
			try
			{
				Aoi = new Rectangle(LowerLeft.Value, UpperRight.Value);
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
