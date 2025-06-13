using LibMapCommon;
using LibMapCommon.Geometry;

namespace LibEsri.Geometry;

public class DatedRegion : GeoRegion<WebMercator>
{
	public DateOnly Date { get; }

	private DatedRegion(DateOnly date, double leftmostX, double rightmostX, GeoPolygon<WebMercator>[] rings)
		:base(leftmostX, rightmostX, rings)
	{
		Date = date;
	}

	private new static GeoRegion<WebMercator> Create(params WebMercator[] coords)
		=> throw new NotSupportedException();

	public static DatedRegion Create(DateOnly date, GeoPolygon<WebMercator>[] rawRings, GeoRegion<WebMercator>? clippingRegion = null)
	{

		if (clippingRegion is null)
		{
			return new(date, rawRings.Select(r => r.MinX).Min(), rawRings.Select(r => r.MaxX).Max(), rawRings);
		}
		else
		{
			var rings = new List<GeoPolygon<WebMercator>>();
			foreach (var poly in clippingRegion.Polygons)
			{
				rings.AddRange(rawRings.SelectMany(r => r.Clip(poly)));
			}
			return new(date, rings.Select(r => r.MinX).Min(), rings.Select(r => r.MaxX).Max(), rings.ToArray());
		}
	}
}
