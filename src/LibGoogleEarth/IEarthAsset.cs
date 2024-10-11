namespace LibGoogleEarth;

public interface IEarthAsset<out T>
{
	bool Compressed { get; }
	string AssetUrl { get; }
	T Decode(byte[] bytes);
}
