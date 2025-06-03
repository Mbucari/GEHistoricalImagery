namespace LibMapCommon;

public interface ICoordinate<out T> : ICoordinate where T : ICoordinate<T>
{
	static abstract T FromWgs84(Wgs1984 wgs1984);
	static abstract int EpsgNumber { get; }
	static abstract double Equator { get; }
}

public interface ICoordinate
{
	public double X { get; }
	public double Y { get; }
}
