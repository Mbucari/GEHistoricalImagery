using Keyhole;
using LibGoogleEarth.WORKINGPROGRESS;

namespace LibGoogleEarth;

public class TerrainTile : IEarthAsset<GridMesh[]>
{
	private const string ROOT_URL_NO_PROVIDER = "https://kh.google.com/flatfile?f1c-{0}-t.{1}";
	public KeyholeTile Tile { get; }
	/// <summary> The aerial image's epoch. </summary>
	public int Epoch { get; }
	/// <summary>
	/// The Google Earther image's provider number
	/// </summary>
	public int Provider { get; }
	/// <summary> Url to the encrypted aerial image. </summary>
	public string AssetUrl { get; }

	public bool Compressed => true;

	internal TerrainTile(KeyholeTile tile, IQuadtreeLayer datedTile)
	{
		Tile = tile;
		Provider = datedTile.Provider;
		Epoch = datedTile.LayerEpoch;

		AssetUrl = string.Format(ROOT_URL_NO_PROVIDER, tile.Path, Epoch);
	}

	public GridMesh[] Decode(byte[] bytes) => GridMesh.ParseAllMeshes(bytes);
}

