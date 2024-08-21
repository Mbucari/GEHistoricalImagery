namespace OSGeo.GDAL;

public readonly record struct GeoTransform
{
	public double UpperLeft_X { get => Transformation[0]; set => Transformation[0] = value; }
	public double PixelWidth { get => Transformation[1]; set => Transformation[1] = value; }
	public double RowRotation { get => Transformation[2]; set => Transformation[2] = value; }
	public double UpperLeft_Y { get => Transformation[3]; set => Transformation[3] = value; }
	public double ColumnRotation { get => Transformation[4]; set => Transformation[4] = value; }
	public double PixelHeight { get => Transformation[5]; set => Transformation[5] = value; }

	public readonly double[] Transformation;
	private const int NUM_PARAMS = 6;

	public GeoTransform()
	{
		Transformation = new double[NUM_PARAMS];
	}

	public void Scale(double scale)
	{
		for (int i = 0; i < Transformation.Length; i++)
			Transformation[i] *= scale;
	}

	public void Translate(double x, double y)
	{
		UpperLeft_X += x;
		UpperLeft_Y += y;
	}
}