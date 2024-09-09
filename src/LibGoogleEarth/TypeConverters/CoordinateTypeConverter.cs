using System.ComponentModel;
using System.Globalization;

namespace LibGoogleEarth.TypeConverters;

public class CoordinateTypeConverter : TypeConverter
{
	public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		if (value is not string text) return null;

		var split = text.Split(',');

		if (split.Length != 2) return null;

		if (!double.TryParse(split[0].Trim(), out var lat) || !double.TryParse(split[1].Trim(), out var lng))
			return null;

		if (lat < -90 || lat > 90 || lng < -180 || lng > 180)
			return null;

		return new Coordinate(lat, lng);
	}
}
