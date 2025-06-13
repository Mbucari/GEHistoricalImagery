using LibMapCommon;
using LibMapCommon.Geometry;
using System.IO.Compression;
using System.Xml.Linq;

namespace GEHistoricalImagery.Kml;

public enum PlacemarkType
{
	Unknown,
	Point,
	LineString,
	Polygon
}

internal class Placemark
{
	public string Name { get; }
	public PlacemarkType Type { get; }
	public Wgs1984[] Coordinates { get; }
	private static Wgs1984TypeConverter Converter { get; } = new();

	private Placemark(string name, PlacemarkType  type, Wgs1984[] coordinates)
	{
		Name = name;
		Type = type;
		Coordinates = coordinates;
	}

	public double GetArea()
	{
		if (Type is PlacemarkType.Point or PlacemarkType.Unknown)
			return 0;

		var poly = GeoRegion<Wgs1984>.Create(Coordinates);
		double sphericalExcess = 0;
		foreach(var triangle in poly.TriangulatePolygon())
		{
			var v1 = triangle.Edges[0].Origin;
			var v2 = triangle.Edges[1].Origin;
			var v3 = triangle.Edges[2].Origin;

			var A = new Wgs1984(v1.Y, v1.X).ToRectangular();
			var B = new Wgs1984(v2.Y, v2.X).ToRectangular();
			var C = new Wgs1984(v3.Y, v3.X).ToRectangular();
			
			var a = Vector3.GetAngle(A, B, C);
			var b = Vector3.GetAngle(B, C, A);
			var c = Vector3.GetAngle(C, A, B);

			var E = a + b + c - Math.PI;
			sphericalExcess += E;
		}

		return sphericalExcess * Math.Pow(WebMercator.Equator / 2 / Math.PI, 2);
	}

	public static List<Placemark>? LoadFromKeyhole(string khFile)
	{
		try
		{
			using var file = File.Open(khFile, FileMode.Open, FileAccess.Read, FileShare.Read);

			if (file.Length < 2) return null;

			Span<byte> header = stackalloc byte[2];
			file.ReadExactly(header);
			file.Position = 0;

			if (header[0] == 'P' && header[1] == 'K')
			{
				var archive = new ZipArchive(file);

				return archive.Entries.Count != 1 || archive.Entries[0].Name != "doc.kml" ? null
					: LoadFromKml(archive.Entries[0].Open());
			}
			else
				return LoadFromKml(file);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine("Error reading keyhole file.\r\n" + ex.Message);
			return null;
		}
	}

	private static List<Placemark>? LoadFromKml(Stream kmlFile)
	{
		var xml = XDocument.Load(kmlFile);
		var kml = xml.Document?.FirstNode as XElement;
		var doc = kml?.FirstNode as XElement;

		if (doc?.Name?.LocalName != "Document")
			return null;

		var placemarks = GetPlacemarksRecursively(doc);
		return placemarks.Count > 0 ? placemarks : null;
	}

	private static List<Placemark> GetPlacemarksRecursively(XElement root)
	{
		var children = root.Elements().ToArray();
		List<Placemark> placemarks = [];

		foreach (var child in children)
		{
			var childName = child.Name.LocalName;

			if (childName == "Folder")
				placemarks.AddRange(GetPlacemarksRecursively(child));
			else if (childName == "Placemark" && Parse(child) is Placemark p)
				placemarks.Add(p);
		}

		return placemarks;
	}

	private static Placemark? Parse(XElement element)
	{
		var ns = element.GetDefaultNamespace();

		var name = element.Element(XName.Get("name", ns.NamespaceName))?.Value;
		if (name is null)
			return null;

		var coordinateList = element
			.Element(XName.Get("Polygon", ns.NamespaceName))
			?.Element(XName.Get("outerBoundaryIs", ns.NamespaceName))
			?.Element(XName.Get("LinearRing", ns.NamespaceName))
			?.Element(XName.Get("coordinates", ns.NamespaceName))
			?.Value;

		if (coordinateList is not null)
			return ParsePolygon(name, coordinateList);

		coordinateList = element
			.Element(XName.Get("LineString", ns.NamespaceName))
			?.Element(XName.Get("coordinates", ns.NamespaceName))
			?.Value;

		if (coordinateList is not null)
			return ParseLineString(name, coordinateList);

		coordinateList = element
			.Element(XName.Get("Point", ns.NamespaceName))
			?.Element(XName.Get("coordinates", ns.NamespaceName))
			?.Value;

		if (coordinateList is not null)
			return ParsePoint(name, coordinateList);

		return null;
	}

	private static Placemark? ParsePoint(string name, string coordinates)
	{
		var coords = ParseCoordinates(coordinates);
		return coords.Length == 1 ? new Placemark(name, PlacemarkType.Point, coords) : null;
	}

	private static Placemark? ParsePolygon(string name, string coordinates)
	{
		var coords = ParseCoordinates(coordinates);
		if (coords.Length < 4) return null;
		Array.Resize(ref coords, coords.Length - 1);
		return new Placemark(name, PlacemarkType.Polygon, coords);
	}

	private static Placemark? ParseLineString(string name, string coordinates)
	{
		var coords = ParseCoordinates(coordinates);
		return coords.Length >= 3 ? new Placemark(name, PlacemarkType.LineString, coords) : null;
	}

	private static Wgs1984[] ParseCoordinates(string coordinates)
		=> coordinates.Trim()
		.Split(' ', StringSplitOptions.RemoveEmptyEntries)
		.Select(c => c.Split(',', StringSplitOptions.RemoveEmptyEntries))
		.Where(c => c.Length >= 2)
		.Select(c => Converter.ConvertFrom($"{c[1]},{c[0]}"))
		.OfType<Wgs1984>()
		.ToArray();
}