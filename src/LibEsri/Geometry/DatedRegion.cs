using LibMapCommon;

namespace LibEsri.Geometry;

public class DatedRegion
{
	public DateOnly Date { get; }
	internal Ring[] Rings { get; }

	internal DatedRegion(DateOnly date, Ring[] rings)
	{
		Date = date;
		Rings = rings;
	}

	public bool Contains(WebMercator coordinate) => Rings.Any(r => r.Contains(coordinate));

}
