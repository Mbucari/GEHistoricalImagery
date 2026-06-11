using LibEsri;

namespace GEHistoricalImagery.Cli.Info;

internal partial class InfoCommand
{
	private async IAsyncEnumerable<TileInfo> EnumerateTileInfos_Esri(EsriTile tile)
	{
		WayBack wayBack = await WayBack.CreateAsync(CacheDir);

		await foreach (DatedEsriTile dated in wayBack.GetDatesAsync(tile))
		{
			yield return new TileInfo
			{
				LayerDate = dated.LayerDate,
				ImageryDate = dated.CaptureDate,
				LayerName = dated.Layer.Title,
				LayerId = dated.Layer.ID
			};
		}
	}
}
