using LibGoogleEarth;

namespace GEHistoricalImagery.Cli.Info;

internal partial class InfoCommand
{
	private async IAsyncEnumerable<TileInfo> EnumerateTileInfos_Keyhole(KeyholeTile tile)
	{
		DbRoot root = await DbRoot.CreateAsync(Database.TimeMachine, CacheDir);
		if (await root.GetNodeAsync(tile) is not { } node)
			yield break;

		foreach (DatedTile? dated in node.GetAllDatedTiles().Where(d => d.Date.Year != 1).OrderByDescending(d => d.Date))
		{
			yield return new TileInfo
			{
				ImageryDate = dated.Date,
				Epoch = dated.Epoch,
				Provider = root.GetProviderCopyright(dated)
			};
		}
	}
}
