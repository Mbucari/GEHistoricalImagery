using LibEsri.Geometry;
using LibMapCommon;
using LibMapCommon.Geometry;
using OSGeo.GDAL;
using OSGeo.OGR;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;

namespace LibEsri;

public record DateOnLayer(Layer Layer, DateOnly SourceDate, long ObjectId, bool IsComplete, string QueryUrl, TileStats Stats)
{
	internal DatedRegion ToDatedRegion(OSGeo.OGR.Geometry geometry)
		=> new(Layer, IsComplete, Stats, SourceDate, geometry);
}

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

	/// <summary>
	/// Gets the image capture date for the given tile on the given layer, 
	/// by querying the feature server for that layer and tile center point,
	/// or returns the layer date if an error occurs.
	/// </summary>
	/// <param name="layer">The layer to query.</param>
	/// <param name="tile">The tile to query.</param>
	/// <returns>The image capture date.</returns>
	public async Task<DateOnly> GetDateAsync(Layer layer, EsriTile tile)
	{
		var queryUrl = layer.GetMetadataQueryUrl(tile.Level);
		var query = Layer.GetPointQuery(tile.Center);
		queryUrl += "?" + query.ToQueryString();

		var esriJson = await TryDownloadJsonAsync(queryUrl);
		var date = esriJson?["features"]?[0]?["attributes"]?["SRC_DATE2"]?.GetValue<long>();

		if (date is long dateNum)
			return DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeMilliseconds(dateNum).DateTime);

		return layer.Date;
	}
	/// <summary>
	/// Queries the given date on layer for its geometry, and returns a dated region
	/// representing the intersection of that geometry with the given region, or null
	/// if an error occurs or there is no intersection.
	/// </summary>
	public async Task<DatedRegion?> GetDatedRegionAsync(DateOnLayer dateOnLayer, GeoRegion<WebMercator> trimTo)
	{
		var query = new EsriQuery
		{
			ReturnGeometry = true,
			GeometryPrecision = 2,
			ObjectIds = [dateOnLayer.ObjectId]
		};
		var queryUrl = dateOnLayer.QueryUrl + "?" + query.ToQueryString();

		string memFile = $"/vsimem/{dateOnLayer.Layer.Title}/{dateOnLayer.ObjectId}.json";
		try
		{
			var esriJsonBts = await HttpClient.GetByteArrayAsync(queryUrl);
			Gdal.FileFromMemBuffer(memFile, esriJsonBts);

			using var driver = Ogr.GetDriverByName("ESRIJSON");
			using var ds = driver.Open(memFile, 0);
			Debug.Assert(ds.GetLayerCount() == 1);
			using var layer = ds.GetLayerByIndex(0);
			using var feature = layer.GetNextFeature();
			using var geom = feature.GetGeometryRef();

			return TrimDatedRegionToMultipolygon(geom, trimTo) is { } region ? dateOnLayer.ToDatedRegion(region)
				: null;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(ex.Message);
			await HttpClient.DeleteCachedPageAsync(queryUrl);
			return null;
		}
		finally
		{
			Gdal.Unlink(memFile);
		}
	}

	/// <summary>
	/// Queries the given layer for all features intersecting the given region.
	/// </summary>
	/// <param name="layer">The layer to query.</param>
	/// <param name="region">The region to query within.</param>
	/// <param name="zoom">The zoom level for the query.</param>
	/// <returns>An array of all dated regions within the specified region.</returns>
	public async Task<DateOnLayer[]> GetDatesOnLayerAsync(Layer layer, GeoRegion<WebMercator> region, int zoom)
	{
		var queryUrl = layer.GetMetadataQueryUrl(zoom);
		var query = Layer.GetPolygonQuery(region);
		var stats = region.GetRectangularRegionStats<EsriTile>(zoom);

		const int retryCount = 0;
		JsonNode? esriJson = await GetFeaturesAsync(queryUrl, query);
		for (int i = 0; i < retryCount && esriJson is null; i++)
		{
			Console.Error.WriteLine($"Error querying {layer.Title}. Retrying {i + 1}/{retryCount}");
			esriJson = await GetFeaturesAsync(queryUrl, query);
		}

		if (esriJson is null)
		{
			Console.Error.WriteLine($"Failed to query {layer.Title}. Try again later.");
			return Array.Empty<DateOnLayer>();
		}

		string memFile = $"/vsimem/{layer.Title}/zoom-{zoom}.json";

		try
		{
			Gdal.FileFromMemBuffer(memFile, Encoding.UTF8.GetBytes(esriJson.ToJsonString()));
			using var driver = Ogr.GetDriverByName("ESRIJSON");
			using var ds = driver.Open(memFile, 0);
			Debug.Assert(ds.GetLayerCount() == 1);
			using var l = ds.GetLayerByIndex(0);

			DateOnLayer[] dateOnLayer = new DateOnLayer[l.GetFeatureCount(1)];
			for (int i = 0; i < dateOnLayer.Length; i++)
			{
				using var f = l.GetNextFeature();
				f.GetFieldAsDateTime("SRC_DATE2", out var year, out var month, out var day, out _, out _, out _, out _);
				var date = new DateOnly(year, month, day);
				var oid = f.GetFieldAsInteger64("OBJECTID");
				dateOnLayer[i] = new(layer, date, oid, dateOnLayer.Length == 1, queryUrl, stats);
			}
			Array.Sort(dateOnLayer, (a, b) => a.SourceDate.CompareTo(b.SourceDate));
			return dateOnLayer;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(ex.Message);
			return Array.Empty<DateOnLayer>();
		}
		finally
		{
			Gdal.Unlink(memFile);
		}
	}

	/// <summary>
	/// Queries the feature server, concatenating results if the result limit is exceeded, until all features are retrieved or an error occurs.
	/// </summary>
	/// <param name="queryUrl">The URL of the feature server to query.</param>
	/// <param name="query">The query parameters.</param>
	/// <returns>A JSON node containing the features, or null if an error occurs.</returns>
	private async Task<JsonNode?> GetFeaturesAsync(string queryUrl, EsriQuery query)
	{
		var content = query.ToFormContent();
		try
		{
			var bytes = await HttpClient.PostByteArrayAsync(queryUrl, content);
			if (JsonNode.Parse(bytes) is not { } node || node["error"] is not null)
			{
				await HttpClient.DeleteCachedPageAsync(queryUrl, content);
				return null;
			}
			if (node["features"] is not JsonArray features)
				return null;

			int count = features.Count;
			while (node["exceededTransferLimit"]?.GetValue<bool>() is true)
			{
				query.ResultOffset = count;
				content = query.ToFormContent();
				bytes = await HttpClient.PostByteArrayAsync(queryUrl, content);

				if (JsonNode.Parse(bytes) is not { } newNode || newNode["error"] is not null)
				{
					await HttpClient.DeleteCachedPageAsync(queryUrl, content);
					return null;
				}
				if (newNode["features"] is not JsonArray newFeatures)
					return null;

				foreach (var jsonNode in newFeatures.ToArray())
				{
					newFeatures.Remove(jsonNode);
					features.Add(jsonNode);
					count++;
				}
				node["exceededTransferLimit"] = newNode["exceededTransferLimit"]?.GetValue<bool>();
			}
			return node;
		}
		catch
		{
			await HttpClient.DeleteCachedPageAsync(queryUrl, content);
			return null;
		}
	}
	/// <summary>
	/// Intersect the given geometry with the given region, and return the result as a multipolygon.
	/// </summary>
	private static OSGeo.OGR.Geometry? TrimDatedRegionToMultipolygon(OSGeo.OGR.Geometry geom, GeoRegion<WebMercator> trimTo)
	{
		OSGeo.OGR.Geometry intersect;
		if (geom.IsValid())
		{
			intersect = trimTo.Intersect(geom);
		}
		else
		{
			using var gValid = geom.MakeValid(["MODE=STRUCTURE"]);
			if (gValid is null || !gValid.IsValid())
				return null;
			intersect = trimTo.Intersect(gValid);
		}

		var type = intersect.GetGeometryType();
		if (type is wkbGeometryType.wkbMultiPolygon)
		{
			return intersect;
		}
		else if (type is not (wkbGeometryType.wkbGeometryCollection or wkbGeometryType.wkbPolygon))
		{
			//Probably either a wkbPoint, wkbLineString, or wkbMultiLineString
			return null;
		}

		var multiPolygon = new OSGeo.OGR.Geometry(wkbGeometryType.wkbMultiPolygon);
		using var sr = intersect.GetSpatialReference();
		multiPolygon.AssignSpatialReference(sr);

		if (type is wkbGeometryType.wkbPolygon)
		{
			multiPolygon.AddGeometry(intersect);
		}
		else
		{
			//wkbGeometryCollection
			bool addedPolygon = false;
			for (int i = intersect.GetGeometryCount() - 1; i >= 0; i--)
			{
				var subGeom = intersect.GetGeometryRef(i);
				var subGeomType = subGeom.GetGeometryType();
				if (subGeomType is wkbGeometryType.wkbPolygon)
				{
					multiPolygon.AddGeometry(subGeom);
					addedPolygon = true;
				}
				//If not a poolygon, either a point or linestring
			}
			if (!addedPolygon)
			{
				multiPolygon.Dispose();
				return null;
			}
		}
		return multiPolygon;
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
			JsonNode? ss = await TryDownloadJsonAsync(url);

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

	protected async Task<JsonNode?> TryDownloadJsonAsync(string url)
	{
		try
		{
			return JsonNode.Parse(await HttpClient.GetByteArrayAsync(url));
		}
		catch
		{
			await HttpClient.DeleteCachedPageAsync(url);
			return null;
		}
	}
	protected async Task<string> DownloadStringAsync(string url)
		=> Encoding.UTF8.GetString(await HttpClient.GetByteArrayAsync(url));
}
