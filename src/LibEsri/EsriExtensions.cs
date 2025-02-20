﻿using LibEsri.Geometry;
using LibMapCommon;
using System.Text.Json.Nodes;

namespace LibEsri;

public static class EsriExtensions
{
	internal static IEnumerable<DatedRegion> ToDatedRegions(this JsonArray? jsonArray, Layer layer)
	{
		if (jsonArray is null || jsonArray.Count == 0)
			yield break;

		foreach (var f in jsonArray.OfType<JsonObject>())
		{
			if (f?["attributes"]?["SRC_DATE2"]?.GetValue<long>() is not long dateNum)
				continue;

			if (f?["geometry"]?["rings"]?.AsArray().ToRings().ToArray() is not Ring[] rings)
				continue;

			var dateOnly = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeMilliseconds(dateNum).DateTime);
			yield return new DatedRegion(dateOnly, rings);
		}
	}

	internal static IEnumerable<Ring> ToRings(this JsonArray? jsonArray)
	{
		if (jsonArray is null || jsonArray.Count == 0)
			yield break;

		foreach (var r in jsonArray.OfType<JsonArray>())
		{
			var coordinates = r.ToCoordinates();

			if (coordinates.Any())
				yield return new Ring(coordinates);
		}
	}

	internal static IEnumerable<WebMercator> ToCoordinates(this JsonArray? jsonArray)
	{
		if (jsonArray is null || jsonArray.Count == 0)
			yield break;

		foreach (var c in jsonArray.OfType<JsonArray>())
		{
			if (c.Count == 2 &&
				c[0]?.GetValue<double>() is double x &&
				c[1]?.GetValue<double>() is double y)
				yield return new WebMercator(x, y);
		}
	}
}
