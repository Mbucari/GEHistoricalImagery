using LibEsri.Geometry;
using LibMapCommon;
using LibMapCommon.Geometry;
using OSGeo.OGR;

namespace LibEsri;

public static class EsriExtensions
{
	internal static IEnumerable<DatedRegion> ToDatedRegions(this DataSource? result, GeoRegion<WebMercator> region)
	{
		if (result is null)
			yield break;
		int lcount = result.GetLayerCount();
		if (lcount == 0)
			yield break;
		if (lcount > 1)
			throw new ArgumentException("data source with more than 1 layer not supported", nameof(result));

		using var l = result.GetLayerByIndex(0);
		var fcount = l.GetFeatureCount(0);
		

		for (int i = 0; i < fcount; i++)
		{
			using var feature = l.GetNextFeature();
			var dcount = feature.GetFieldCount();

			for (int j = 0; j < dcount; j++)
			{
				using var defn = feature.GetFieldDefnRef(j);
				if (defn.GetName() != "SRC_DATE2" && defn.GetFieldType() != FieldType.OFTDateTime)
					continue;

				feature.GetFieldAsDateTime(0, out int year, out int month, out int day, out _, out _, out _, out _);
				using var g = feature.GetGeometryRef();
				OSGeo.OGR.Geometry intersect;
				if (g.IsValid())
				{
					intersect = region.Intersect(g);
				}
				else
				{
					using var gValid = g.MakeValid(["MODE=STRUCTURE"]);
					if (gValid is null || !gValid.IsValid())
						continue;
					intersect = region.Intersect(gValid);
				}

				using var envelope = new Envelope();
				intersect.GetEnvelope(envelope);
				if (envelope.MinX - envelope.MaxX == 0 || envelope.MinY - envelope.MaxY == 0)
					continue;
				if (envelope.MaxX - envelope.MinX >= WebMercator.Equator / 2)
				{
					//The region can cross the antimeridean and have a min = -180 and max = 180, but geometry delieved by ESRI
					//should all be split at antimeridean, so if we have a width of 180 or more, it's likely an error.
					throw new Exception("envelope too wide, likely invalid geometry");
				}
				yield return new DatedRegion(new DateOnly(year, month, day), envelope, intersect);
			}
		}
	}
}
