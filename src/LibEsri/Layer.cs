using LibMapCommon;
using LibMapCommon.Geometry;
using System.Diagnostics;
using System.Xml.Linq;

namespace LibEsri;

public class Layer
{
	private static readonly XNamespace Ows = "https://www.opengis.net/ows/1.1";
	public string Title { get; init; }
	public DateOnly Date { get; }
	public int ID { get; }
	public string Identifier { get; }
	public string Format { get; }
	public string[] TileMatrixSets { get; }
	public string ResourceURL { get; }

	private Layer(string title, string identifier, string format, string resourceUrl, string[] matrixSets)
	{
		Title = title;
		Date = GetLayerDate(title);
		Identifier = identifier;
		Format = format;
		ResourceURL = resourceUrl;
		TileMatrixSets = matrixSets;
		ID = GetId(ResourceURL);
	}

	internal static Layer Parse(XElement layer)
	{
		var ns = layer.GetDefaultNamespace();
		var title = GetElementByName(layer, Ows, "Title").Value;
		var identifier = GetElementByName(layer, Ows, "Identifier").Value;
		var format = GetElementByName(layer, ns, "Format").Value;
		var resourceUrl = GetAttributeByName(GetElementByName(layer, ns, "ResourceURL"), "template").Value;
		var matrixSets = layer.Elements(ns + "TileMatrixSetLink").Select(e => GetElementByName(e, ns, "TileMatrixSet")).OfType<XElement>().Select(e => e.Value).ToArray();

		return new Layer(title, identifier, format, resourceUrl, matrixSets);
	}

	private string GetMetadataUrl(int level, bool returnGeometry, params string[] outFields)
	{
		const string KEY_TEXT = "/World_Imagery";

		var scale = int.Min(13, 23 - level);

		int start = ResourceURL.IndexOf("//") + 2;
		int end2 = ResourceURL.IndexOf('.', start);

		var newDomain = ResourceURL.Substring(0, start) + "metadata" + ResourceURL.Substring(end2);

		int end = newDomain.IndexOf(KEY_TEXT) + KEY_TEXT.Length;

		var retStr = returnGeometry ? "true" : "false";
		var query = string.Join(",", outFields);

		var url = newDomain.Substring(0, end) + "_Metadata" + Identifier.Replace("WB", "").ToLowerInvariant() +
			$"/MapServer/{scale}/query?f=json&where=1%3D1&outFields={query}&returnGeometry={retStr}";

		return url;
	}

	public string GetEnvelopeQueryUrl(GeoRegion<WebMercator> region, int level)
	{
		var ring = $"%7B%22rings%22%3A{GetRings(region)}%2C%22spatialReference%22%3A%7B%22wkid%22%3A{WebMercator.EpsgNumber}%7D%7D";

		var metadataUrl
			= GetMetadataUrl(level, returnGeometry: true, "SRC_DATE2")
			+ "&geometryType=esriGeometryPolygon&spatialRel=esriSpatialRelIntersects&geometry="
			+ ring;
		return metadataUrl;
	}

	private static string GetRings(GeoRegion<WebMercator> region)
	{
		string[] rings = new string[region.Polygons.Length];
		for (int i = 0; i < region.Polygons.Length; i++)
		{
			var poly = region.Polygons[i];
			string[] points = new string[poly.Edges.Count + 1];

			for (int j = 0; j < poly.Edges.Count; j++)
				points[j] = FormattableString.Invariant($"%5B{poly.Edges[j].Origin.X},{poly.Edges[j].Origin.Y}%5D");
			points[^1] = points[0];

			rings[i] = "%5B" + string.Join("%2C", points) + "%5D";
		}
		return "%5B" + string.Join("%2C", rings) + "%5D";
	}
	public string GetPointQueryUrl(EsriTile tile)
	{
		var center = tile.Center;

		var metadataUrl
			= GetMetadataUrl(tile.Level, returnGeometry: false, "SRC_DATE2")
			+ "&geometryType=esriGeometryPoint&spatialRel=esriSpatialRelIntersects&geometry="
			+ $"%7B%22spatialReference%22%3A%7B%22wkid%22%3A{WebMercator.EpsgNumber}%7D%2C%22x%22%3A{center.X}%2C%22y%22%3A{center.Y}%7D";
		return metadataUrl;
	}

	public string GetTileMapUrl(EsriTile tile)
	{
		const string KEY_TEXT = "/World_Imagery";
		int end = ResourceURL.IndexOf(KEY_TEXT) + KEY_TEXT.Length;
		var url = ResourceURL.Substring(0, end) + "/MapServer/tilemap";

		return $"{url}/{ID}/{tile.Level}/{tile.Row}/{tile.Column}";
	}

	public string GetAssetUrl(EsriTile tile)
		=> ResourceURL
			.Replace("{TileMatrixSet}", TileMatrixSets[0])
			.Replace("{TileMatrix}", tile.Level.ToString())
			.Replace("{TileRow}", tile.Row.ToString())
			.Replace("{TileCol}", tile.Column.ToString());

	public override string ToString() => Title;

	private static int GetId(string resourceURL)
	{
		const string KEY_TEXT = "/MapServer/tile/";

		int start = resourceURL.IndexOf(KEY_TEXT) + KEY_TEXT.Length;
		int end = resourceURL.IndexOf('/', start);
		var idString = resourceURL.Substring(start, end - start);
		return int.Parse(idString);
	}

	private static DateOnly GetLayerDate(string title)
	{
		const string KEY_TEXT = "(Wayback ";
		int start = title.IndexOf(KEY_TEXT) + KEY_TEXT.Length;
		int end = title.IndexOf(')', start);

		var dateStr = title.Substring(start, end - start);

		return DateOnly.ParseExact(dateStr, "yyyy-MM-dd");
	}

	[StackTraceHidden]
	private static XAttribute GetAttributeByName(XElement element, string name)
		=> element.Attribute(name) ?? throw new ArgumentException($"{element.Name.LocalName} does not contain attribute \"{name}\"");

	[StackTraceHidden]
	private static XElement GetElementByName(XElement element, XNamespace ns, string name)
		=> element.Element(ns + name) ?? throw new ArgumentException($"Layer does not contain element \"{name}\"");
}
