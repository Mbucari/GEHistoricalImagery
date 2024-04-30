using System.ComponentModel;
using System.Globalization;

namespace GEHistoricalImagery.Cli;

internal class CoordinateTypeConverter : TypeConverter
{
	public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		if (value is not string text) return null;

		var split = text.Split(',');

		if (split.Length != 2) return null;

		if (!double.TryParse(split[0].Trim(), out var lat) || !double.TryParse(split[1].Trim(), out var lng)) return null;

		return new Coordinate(lat, lng);
	}
}
