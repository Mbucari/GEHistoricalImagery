using LibEsri.Geometry;
using LibMapCommon;
using System.Text.Json.Nodes;

namespace LibEsri;

public static class EsriExtensions
{
	public static IEnumerable<DatedRegion> ToDatedRegions(this JsonArray? jsonArray, Layer layer)
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

	public static IEnumerable<WebCoordinate> ToCoordinates(this JsonArray? jsonArray)
	{
		if (jsonArray is null || jsonArray.Count == 0)
			yield break;

		foreach (var c in jsonArray.OfType<JsonArray>())
		{
			if (c.Count == 2 &&
				c[0]?.GetValue<double>() is double x &&
				c[1]?.GetValue<double>() is double y)
				yield return new WebCoordinate(x, y);
		}
	}

	public static (int pixelX, int pixelY) GetGlobalPixelCoordinate(this WebCoordinate coordinate, int zoom)
	{
		//https://github.com/Leaflet/Leaflet/discussions/8100

		const int TILE_SZ = 256;
		const int HALF_TILE = TILE_SZ / 2;

		var size = 1 << zoom;

		var xPercent = (HALF_TILE + coordinate.X * TILE_SZ / WebCoordinate.Equator) * size;
		var yPercent = (HALF_TILE - coordinate.Y * TILE_SZ / WebCoordinate.Equator) * size;
		return ((int)double.Round(xPercent, 0), (int)double.Round(yPercent, 0));
	}
}
