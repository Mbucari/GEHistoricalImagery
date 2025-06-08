namespace LibMapCommon;

public interface IGeoCoordinate<out T> : IGeoCoordinate
{
	static abstract T Create(double x, double y);
}

public interface IGeoCoordinate : ICoordinate
{
	static abstract int EpsgNumber { get; }
	static abstract double Equator { get; }
}

public interface ICoordinate
{
	double X { get; }
	double Y { get; }
}
