using OSGeo.GDAL;
using System.Text;

namespace GEHistoricalImagery;

internal class DxfRaster
{
	string[] Lines { get; }

	Dictionary<string, int> SectionIndices { get; }

	public DxfRaster()
	{
		Lines = Properties.Resources.embedded_raster_dxf.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
		SectionIndices = new();
		for (int i = 0; i < Lines.Length; i++)
		{
			if (Lines[i] == "SECTION")
			{
				string sectionName = Lines[i + 2];
				SectionIndices[sectionName] = i;
			}
		}
	}

	public void Save(string filePath)
	{
		File.WriteAllLines(filePath, Lines);
	}

	public void SetRaster(string filePath, int width, int height, GeoTransform geoTransform)
	{
		var lowerLeft = GetLowerLeftCorner(height, geoTransform);
		var upperRight = GetUpperRightCorner(width, geoTransform);
		Span<string> lines = Lines;
		DxfField field;

		//IMAGE
		int start = SectionIndices["ENTITIES"];
		int index = start + lines.Slice(start).IndexOf("AcDbRasterImage") + 1;
		while (index < lines.Length && (field = GetNext()).Code != 0)
		{
			switch (field.Code)
			{
				case 10:
					field.Value = lowerLeft.X.ToString();
					break;
				case 20:
					field.Value = lowerLeft.Y.ToString();
					break;
				case 11:
					field.Value = geoTransform.PixelWidth.ToString();
					break;
				case 21:
					field.Value = geoTransform.ColumnRotation.ToString();
					break;
				case 12:
					field.Value = (-geoTransform.RowRotation).ToString();
					break;
				case 22:
					field.Value = (-geoTransform.PixelHeight).ToString();
					break;
				case 13:
					field.Value = width.ToString();
					break;
				case 23:
					field.Value = height.ToString();
					break;
			}
		}
		//IMAGEDEF
		start = SectionIndices["OBJECTS"];
		index = start + lines.Slice(start).IndexOf("AcDbRasterImageDef") + 1;
		while (index < lines.Length && (field = GetNext()).Code != 0)
		{
			switch (field.Code)
			{
				case 1:
					field.Value = UnicodeToEscapedAscii(filePath);
					break;
				case 10:
					field.Value = width.ToString();
					break;
				case 20:
					field.Value = height.ToString();
					break;
				case 11:
					field.Value = (1d / width).ToString();
					break;
				case 21:
					field.Value = (1d / width).ToString();
					break;
			}
		}
		//VPORT
		var center = 0.5 * (upperRight + lowerLeft);
		var viewHeight = upperRight.Y - lowerLeft.Y;
		var viewWidth = upperRight.X - lowerLeft.X;
		const double ratio = 2.0;

		if (viewWidth / viewHeight > ratio)
		{
			viewHeight = viewWidth / ratio;
		}

		start = SectionIndices["TABLES"];
		start += lines.Slice(start).IndexOf("VPORT") + 1; //Table name
		index = start + lines.Slice(start).IndexOf("VPORT") + 1; //VPORT entry
		while (index < lines.Length && (field = GetNext()).Code != 0)
		{
			switch (field.Code)
			{
				case 12:
					field.Value = center.X.ToString();
					break;
				case 22:
					field.Value = center.Y.ToString();
					break;
				case 40:
					field.Value = (viewHeight * 1.1).ToString();
					break;
				case 41:
					field.Value = ratio.ToString();
					break;
			}
		}
		DxfField GetNext() => new() { Code = int.Parse(Lines[index++]), Value = ref Lines[index++] };
	}

	private static string UnicodeToEscapedAscii(string input)
	{
		var sb = new StringBuilder();
		foreach (var c in input)
		{
			if (c <= 127)
			{
				sb.Append(c);
			}
			else
			{
				sb.Append(@$"\U+{(ushort)c:X4}");
			}
		}
		return sb.ToString();
	}

	private static Vector2 GetLowerLeftCorner(int height, GeoTransform xform)
	{
		var center = new Vector2(xform.UpperLeft_X, xform.UpperLeft_Y);
		var h = new Vector2(xform.PixelWidth, xform.ColumnRotation);
		var v = new Vector2(xform.RowRotation, xform.PixelHeight);
		return center + (-0.5 * (v + h)) + height * v;
	}

	private static Vector2 GetUpperRightCorner(int width, GeoTransform xform)
	{
		var center = new Vector2(xform.UpperLeft_X, xform.UpperLeft_Y);
		var h = new Vector2(xform.PixelWidth, xform.ColumnRotation);
		var v = new Vector2(xform.RowRotation, xform.PixelHeight);
		return center + (-0.5 * (v + h)) + width * h;
	}

	private ref struct DxfField
	{
		public int Code;
		public ref string Value;
	}

	private record struct Vector2(double X, double Y)
	{
		public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.X + b.X, a.Y + b.Y);
		public static Vector2 operator *(double s, Vector2 a) => new Vector2(a.X * s, a.Y * s);
	}
}
