using LibEsri.Geometry;
using LibMapCommon;
using LibMapCommon.Geometry;
using System.Text;
using System.Text.Json.Nodes;

namespace LibEsri;

public class WayBack
{
	private const string WayBackUrl = "https://wayback.maptiles.arcgis.com/arcgis/rest/services/world_imagery/mapserver/wmts/1.0.0/wmtscapabilities.xml";
	private readonly CachedHttpClient HttpClient;
	private Dictionary<int, Layer> Capabilities { get; }
	public IReadOnlyCollection<Layer> Layers => Capabilities.Values;

	private WayBack(CachedHttpClient cacheHttpClient, Dictionary<int, Layer> capabilities)
	{
		Capabilities = capabilities;
		HttpClient = cacheHttpClient;
	}

	public static async Task<WayBack> CreateAsync(string? cacheDir)
	{
		var cacheDirInfo = cacheDir is null ? null : new DirectoryInfo(cacheDir);
		cacheDirInfo?.Create();

		var cachedHttpClient = new CachedHttpClient(cacheDirInfo);

		var stream = await cachedHttpClient.GetStreamAsync(WayBackUrl);
		var caps = await LibEsri.Capabilities.LoadAsync(stream) ?? throw new Exception();

		return new WayBack(cachedHttpClient, caps.Layers.ToDictionary(l => l.ID));
	}

	public async Task<DateOnly> GetDateAsync(Layer layer, EsriTile tile)
	{
		var metadataUrl = layer.GetPointQueryUrl(tile);

		try
		{
			var ss = await DownloadJsonAsync(metadataUrl);

			var date = ss?["features"]?[0]?["attributes"]?["SRC_DATE2"]?.GetValue<long>();

			if (date is long dateNum)
				return DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeMilliseconds(dateNum).DateTime);
		}
		catch { }

		return layer.Date;
	}

	public async Task<DatedRegion[]> GetDateRegionsAsync(Layer layer, GeoRegion<WebMercator> region, int zoom)
	{
		var metadataUrl = layer.GetEnvelopeQueryUrl(region, zoom);

		try
		{
			var ss = await DownloadJsonAsync(metadataUrl);

			if (ss?["features"]?.AsArray().ToDatedRegions(layer, region).ToArray() is not DatedRegion[] regions)
				return Array.Empty<DatedRegion>();

			//consolidate duplicate dates
			Dictionary<DateOnly, DatedRegion> set = new();
			foreach (var r in regions)
			{
				if (set.TryGetValue(r.Date, out var dr))
				{
					var arr1 = dr.Polygons;
					Array.Resize(ref arr1, arr1.Length + r.Polygons.Length);
					Array.Copy(r.Polygons, 0, arr1, dr.Polygons.Length, r.Polygons.Length);
					set[r.Date] = DatedRegion.Create(r.Date, arr1);
				}
				else
					set.Add(r.Date, r);
			}

			return set.Values.ToArray();
		}
		catch { }
		return Array.Empty<DatedRegion>();
	}

	public async Task<byte[]> DownloadTileAsync(Layer layer, EsriTile tile)
	{
		var url = layer.GetAssetUrl(tile);
		var bts = await HttpClient.GetByteArrayAsync(url);
		return bts;
	}

	public async Task<DatedEsriTile?> GetNearestDatedTileAsync(EsriTile tile, DateOnly desiredDate)
	{
		DatedEsriTile? datedTile = null;

		await foreach (var dt in GetDatesAsync(tile))
		{
			datedTile ??= dt;
			if (dt.CaptureDate <= desiredDate)
			{
				var d1 = datedTile.CaptureDate.DayNumber - desiredDate.DayNumber;
				var d2 = desiredDate.DayNumber - dt.CaptureDate.DayNumber;

				if (d2 < d1)
					datedTile = dt;

				break;
			}

			datedTile = dt;
		}
		return datedTile;
	}

	public async IAsyncEnumerable<DatedEsriTile> GetDatesAsync(EsriTile tile)
	{
		int? skipUntil = null;
		DateOnly? lastDate = null;
		Layer? last = null;

		foreach (var (i, layer) in Capabilities)
		{
			if (skipUntil != null)
			{
				if (skipUntil == i)
					skipUntil = null;
				continue;
			}

			var url = layer.GetTileMapUrl(tile);
			var ss = await DownloadJsonAsync(url);

			Layer f;
			if (ss?["select"]?[0] is JsonValue v)
			{
				skipUntil = v.GetValue<int>();
				f = Capabilities[skipUntil.Value];
			}
			else
			{
				f = Capabilities[i];
			}

			if (ss?["data"]?[0]?.GetValue<int>() == 1)
			{
				var date = await GetDateAsync(f, tile);
				if (lastDate.HasValue && last != null && lastDate.Value != date)
				{
					//Only emit a layer once the actual tile date changes.
					//In this way, only the earliest version with unique imagery is emitted.
					yield return new DatedEsriTile(lastDate.Value, last, tile);
				}
				lastDate = date;
				last = f;
			}
		}

		if (lastDate.HasValue && last != null)
			yield return new DatedEsriTile(lastDate.Value, last, tile);
	}

	protected async Task<JsonNode?> DownloadJsonAsync(string url)
		=> JsonNode.Parse(await HttpClient.GetByteArrayAsync(url));
	protected async Task<string> DownloadStringAsync(string url)
		=> Encoding.UTF8.GetString(await HttpClient.GetByteArrayAsync(url));
}
