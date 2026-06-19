using LibMapCommon;
using LibMapCommon.Geometry;
using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace LibEsri;

public class Layer : IDatedElement
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

	public string GetMetadataQueryUrl(int level)
	{
		const string KEY_TEXT = "/World_Imagery";

		var scale = int.Min(13, 23 - level);

		int start = ResourceURL.IndexOf("//") + 2;
		int end2 = ResourceURL.IndexOf('.', start);

		var newDomain = string.Concat(
			ResourceURL.AsSpan(0, start),
			"metadata",
			ResourceURL.AsSpan(end2));

		int end = newDomain.IndexOf(KEY_TEXT) + KEY_TEXT.Length;
		var url = string.Concat(
			newDomain.AsSpan(0, end),
			"_Metadata",
			Identifier.Replace("WB", "").ToLowerInvariant(),
			$"/MapServer/{scale}/query");
		return url;
	}

	public static EsriQuery GetPolygonQuery(GeoRegion<WebMercator> region)
	{
		var arrayOfRings = new JsonArray();
		foreach (var polygon in region.GetPolygons())
		{
			int gCount = polygon.GetGeometryCount();
			for (int i = 0; i < gCount; i++)
			{
				var g = polygon.GetGeometryRef(i);
				var gType = g.GetGeometryType();
				if (gType is not OSGeo.OGR.wkbGeometryType.wkbLineString)
					throw new ArgumentException($"Expected geometry type of wkbLinearRing, got {gType}");
				var pCount = g.GetPointCount();

				var points = new JsonArray();
				arrayOfRings.Add(points);
				double[] point = new double[2];
				for (int k = 0; k < pCount; k++)
				{
					g.GetPoint_2D(k, point);
					//We only need meter precision for the query, so we can round to int 
					points.Add(new JsonArray((int)point[0], (int)point[1]));
				}
			}
		}
		return new EsriQuery
		{
			OutFields = [ "OBJECTID", "SRC_DATE2" ],
			SpatialRel = EsrieSriSpatialRel.Intersects,
			GeometryType = EsriGeometryType.Polygon,
			InSR = new JsonObject { ["wkid"] = WebMercator.EpsgNumber },
			Geometry = new JsonObject { ["rings"] = arrayOfRings },
		};
	}

	public static EsriQuery GetPointQuery(WebMercator center) => new EsriQuery
	{
		OutFields = ["SRC_DATE2"],
		SpatialRel = EsrieSriSpatialRel.Within,
		GeometryType = EsriGeometryType.Point,
		InSR = new JsonObject { ["wkid"] = WebMercator.EpsgNumber },
		Geometry = new JsonObject { ["x"] = center.X, ["y"] = center.Y },
	};

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
