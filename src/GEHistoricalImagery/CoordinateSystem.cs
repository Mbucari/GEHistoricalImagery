using OSGeo.OSR;
using System.Diagnostics.CodeAnalysis;

namespace GEHistoricalImagery;

public class CoordinateSystem : IDisposable
{
	public SpatialReference SpatialReference { get; }
	public string Name { get; }
	public bool IsGeographic { get; }
	public int XAxis { get; } = -1;
	public int YAxis { get; } = -1;
	public int ZAxis { get; } = -1;
	public double LinearUnits { get; }

	private CoordinateSystem(SpatialReference sr)
	{
		SpatialReference = sr;
		Name = SpatialReference.GetName();
		LinearUnits = sr.GetLinearUnits();
		IsGeographic = SpatialReference.IsGeographic() != 0;

		var axisCount = sr.GetAxesCount();
		for (int a = 0; a < axisCount; a++)
		{
			switch (sr.GetAxisOrientation(null, a))
			{
				case AxisOrientation.OAO_East:
				case AxisOrientation.OAO_West:
					XAxis = a;
					break;
				case AxisOrientation.OAO_North:
				case AxisOrientation.OAO_South:
					YAxis = a;
					break;
				case AxisOrientation.OAO_Up:
				case AxisOrientation.OAO_Down:
					ZAxis = a;
					break;
			}
		}

		//Most coordinate systems don't have a 3rd axis, but just in case
		if (axisCount < 3)
			ZAxis = 2;
	}

	public static bool TryParse(string csText, [NotNullWhen(true)] out CoordinateSystem? cs)
	{
		var sr = new SpatialReference("");

		try
		{
			if (sr.SetFromUserInput(csText) == 0)
			{
				cs = new(sr);
				return cs.XAxis != -1 && cs.YAxis != -1 && cs.ZAxis != -1;
			}
		}
		catch { sr.Dispose(); }
		cs = null;
		return false;
	}

	public void Dispose() => SpatialReference.Dispose();
	public override string ToString() => Name;
}
