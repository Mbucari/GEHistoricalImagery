using LibMapCommon;

namespace LibEsri.Geometry;

internal class Ring
{
	private Vector2[] Coordinates { get; }
	public double MinX { get; }
	public double MinY { get; }
	public double MaxX { get; }
	public double MaxY { get; }
	public bool IsValid => Coordinates.Length > 2;

	public Ring(IEnumerable<WebCoordinate> coordinates)
	{
		Coordinates = coordinates.Select(c => (Vector2)c).ToArray();

		MinX = Coordinates.MinBy(v => v.X).X;
		MinY = Coordinates.MinBy(v => v.Y).Y;
		MaxX = Coordinates.MaxBy(v => v.X).X;
		MaxY = Coordinates.MaxBy(v => v.Y).Y;
	}

	public bool Contains(WebCoordinate coordinate)
	{
		if (!IsValid || coordinate.X < MinX || coordinate.X > MaxX || coordinate.Y < MinY || coordinate.Y > MaxY)
			return false;

		var testEdge = new Line2
		{
			Origin = (Vector2)coordinate,
			Direction = Vector2.UnitX
		};

		int hitCount = 0;
		Vector2 start = Coordinates[^1];
		for (int i = 0; i < Coordinates.Length; i++)
		{
			var end = Coordinates[i];

			var edge = new Line2 { Origin = start, Direction = end - start };

			var v = edge.Intersect(testEdge);

			if (v.X > 0 && v.X < 1 && v.Y > 0)
				hitCount++;
			start = end;
		}

		return (hitCount & 1) == 1;
	}
}
