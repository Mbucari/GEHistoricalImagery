using LibMapCommon.Geometry;

namespace LibEsri.Geometry;

public class DatedRegion
{
	public DateOnly Date { get; }
	internal WebMercatorPoly[] Rings { get; private set; }

	internal DatedRegion(DateOnly date, WebMercatorPoly[] rings, WebMercatorPoly? clippingRegion = null)
	{
		Date = date;
		Rings = clippingRegion is null ? rings : rings.SelectMany(r => r.Clip(clippingRegion)).ToArray();
	}

	public bool ContainsTile(EsriTile tile) => Rings.Any(r => r.ContainsTile(tile));

}
