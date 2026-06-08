namespace OSGeo.GDAL;

/// <summary>
/// A 32-bit RGBA color
/// </summary>
/// <param name="Red">The red component of the color.</param>
/// <param name="Green">The green component of the color.</param>
/// <param name="Blue">The blue component of the color.</param>
/// <param name="Alpha">The alpha component of the color.</param>
public readonly record struct Color(byte Red, byte Green, byte Blue, byte Alpha)
{
	public Color(byte Red, byte Green, byte Blue, float Alpha)
		: this(Red, Green, Blue, (byte)Math.Round(Alpha * byte.MaxValue)) { }

	public static Color Empty { get; } = new Color(0xff, 0xff, 0xff, 0x00);
	public static Color White { get; } = new Color(0xff, 0xff, 0xff, 0xff);

	public readonly Color Overlay(Color topColor)
	{
		var a0 = topColor.Alpha / 255f;
		var a1 = Alpha / 255f;
		var invAlpha = (1 - a0) * a1;
		var a01 = invAlpha + a0;
		return new Color(
			(byte)Math.Round((invAlpha * Red + a0 * topColor.Red) / a01),
			(byte)Math.Round((invAlpha * Green + a0 * topColor.Green) / a01),
			(byte)Math.Round((invAlpha * Blue + a0 * topColor.Blue) / a01),
			(byte)Math.Round(a01 * 255));
	}
	public readonly Color Flatten() => Flatten(White);
	public readonly Color Flatten(Color background)
	{
		var a = Alpha / 255f;
		var invAlpha = 1 - a;
		return new Color(
			(byte)Math.Round(invAlpha * background.Red + a * Red),
			(byte)Math.Round(invAlpha * background.Green + a * Green),
			(byte)Math.Round(invAlpha * background.Blue + a * Blue),
			byte.MaxValue);
	}
}
