using LibEsri.Geometry;
using LibMapCommon;
using LibMapCommon.Geometry;
using System.Text.Json.Nodes;

namespace LibEsri;

public static class EsriExtensions
{
	internal static IEnumerable<DatedRegion> ToDatedRegions(this JsonArray? jsonArray, Layer layer, GeoRegion<WebMercator> region)
	{
		if (jsonArray is null || jsonArray.Count == 0)
			yield break;

		foreach (var f in jsonArray.OfType<JsonObject>())
		{
			if (f?["attributes"]?["SRC_DATE2"]?.GetValue<long>() is not long dateNum)
				continue;

			if (f?["geometry"]?["rings"]?.AsArray().ToRings().ToArray() is not GeoPolygon<WebMercator>[] rings)
				continue;

			var dateOnly = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeMilliseconds(dateNum).DateTime);
			yield return DatedRegion.Create(dateOnly, rings, region);
		}
	}

	private static IEnumerable<GeoPolygon<WebMercator>> ToRings(this JsonArray? jsonArray)
	{
		if (jsonArray is null || jsonArray.Count == 0)
			yield break;

		foreach (var r in jsonArray.OfType<JsonArray>())
		{
			var coordinates = r.ToCoordinates();
			GeoPolygon<WebMercator>? polygon = null;
			try
			{
				if (coordinates.Any())
					polygon = new GeoPolygon<WebMercator>(coordinates.ToArray());
			}
			catch { }
			if (polygon is not null)
				yield return polygon;
		}
	}

	private static IEnumerable<WebMercator> ToCoordinates(this JsonArray? jsonArray)
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
