using System.ComponentModel;
using System.Globalization;

namespace LibMapCommon;

public class Wgs1984TypeConverter : TypeConverter
{
	public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		if (value is not string text) return null;

		var split = text.Split(',');

		if (split.Length != 2) return null;

		if (!double.TryParse(split[0].Trim(), CultureInfo.InvariantCulture, out var lat) ||
			!double.TryParse(split[1].Trim(), CultureInfo.InvariantCulture, out var lng))
			return null;

		if (lat < -90 || lat > 90 || lng < -360 || lng > 360)
			return null;

		return new Wgs1984(lat, lng);
	}
}
