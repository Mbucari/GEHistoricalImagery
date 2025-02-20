namespace LibMapCommon;

public interface ICoordinate<T> : ICoordinate where T : ICoordinate<T>
{
	static abstract T FromWgs84(Wgs1984 wgs1984);
}

public interface ICoordinate
{
	public double X { get; }
	public double Y { get; }
	static abstract double Equator { get; }
	static abstract int EpsgNumber { get; }
}
