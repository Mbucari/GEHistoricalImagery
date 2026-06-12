using LibMapCommon;
using OSGeo.OGR;

namespace GEHistoricalImagery.Cli.Info;

internal static class InfoDatabase
{
	const string LayerName = "GEHI_Info";
	const string ProviderField = "provider";
	const string ZoomLevelField = "zoom_level";
	const string RowField = "row";
	const string ColumnField = "column";
	const string ImageryDateField = "imagery_date";
	const string QTPathField = "quadtree_path";
	const string GEProviderField = "tm_provider";
	const string GEEpochField = "tm_epoch";
	const string WBLayerNameField = "wb_layer_name";
	const string WBLayerIdField = "wb_layer_id";
	const string WBLayerDateField = "wb_layer_date";

	public static void SaveInfoData(this InfoData data, string filename)
	{
		using var driver = Ogr.GetDriverByName("GeoJSON");
		using var database = File.Exists(filename) ? driver.Open(filename, 1) : driver.CreateDataSource(filename, null);
		using var infoDataLayer = database.GetOrCreateLayer();
		using var layerDef = infoDataLayer.GetLayerDefn();

		infoDataLayer.EnsureSharedFields(layerDef);
		if (data.Provider == Provider.Wayback)
			infoDataLayer.EnsureWaybackFields(layerDef);
		else if (data.Provider == Provider.TM)
			infoDataLayer.EnsureGEFields(layerDef);

		foreach (var level in data.LevelInfos)
		{
			foreach (var tile in level.TileInfos)
			{
				using var point = new Geometry(wkbGeometryType.wkbPoint);
				point.AddPoint_2D(data.Wgs1984.Longitude, data.Wgs1984.Latitude);

				using var feature = new Feature(layerDef);
				feature.SetGeometry(point);
				feature.SetField(ProviderField, data.Provider.ToString());
				feature.SetField(ZoomLevelField, level.Level);
				feature.SetField(RowField, level.Row);
				feature.SetField(ColumnField, level.Column);
				feature.SetField(ImageryDateField, tile.ImageryDate);
				if (data.Provider == Provider.Wayback)
				{
					feature.SetField(WBLayerNameField, tile.LayerName);
					if (tile.LayerId.HasValue)
						feature.SetField(WBLayerIdField, tile.LayerId.Value);
					if (tile.LayerDate.HasValue)
						feature.SetField(WBLayerDateField, tile.LayerDate.Value);
				}
				else if (data.Provider == Provider.TM)
				{
					if (!string.IsNullOrEmpty(level.QuadtreePath))
						feature.SetField(QTPathField, level.QuadtreePath);
					if (tile.Epoch.HasValue)
						feature.SetField(GEEpochField, tile.Epoch.Value);
					if (!string.IsNullOrEmpty(tile.Provider))
						feature.SetField(GEProviderField, tile.Provider);
				}
				infoDataLayer.CreateFeature(feature);
			}
		}
	}

	private static Layer GetOrCreateLayer(this DataSource database)
	{
		if (database.GetLayerByName(LayerName) is not { } layer)
		{
			using var sr = new OSGeo.OSR.SpatialReference(null);
			sr.Import<Wgs1984>();
			layer = database.CreateLayer(LayerName, sr, wkbGeometryType.wkbPoint, null);
		}
		return layer;
	}

	private static void EnsureSharedFields(this Layer layer, FeatureDefn layerDef)
	{
		if (layerDef.GetFieldIndex(ProviderField) == -1)
		{
			using FieldDefn providerDef = new(ProviderField, FieldType.OFTString);
			layer.CreateField(providerDef, 1);
		}
		if (layerDef.GetFieldIndex(ZoomLevelField) == -1)
		{
			using FieldDefn zoomDef = new(ZoomLevelField, FieldType.OFTInteger);
			layer.CreateField(zoomDef, 1);
		}
		if (layerDef.GetFieldIndex(RowField) == -1)
		{
			using FieldDefn rowDef = new(RowField, FieldType.OFTInteger);
			layer.CreateField(rowDef, 1);
		}
		if (layerDef.GetFieldIndex(ColumnField) == -1)
		{
			using FieldDefn columnDef = new(ColumnField, FieldType.OFTInteger);
			layer.CreateField(columnDef, 1);
		}
		if (layerDef.GetFieldIndex(ImageryDateField) == -1)
		{
			using FieldDefn dateDef = new(ImageryDateField, FieldType.OFTDate);
			layer.CreateField(dateDef, 1);
		}
	}

	private static void EnsureWaybackFields(this Layer layer, FeatureDefn layerDef)
	{
		if (layerDef.GetFieldIndex(WBLayerNameField) == -1)
		{
			using FieldDefn nameDef = new(WBLayerNameField, FieldType.OFTString);
			layer.CreateField(nameDef, 1);
		}
		if (layerDef.GetFieldIndex(WBLayerIdField) == -1)
		{
			using FieldDefn idDef = new(WBLayerIdField, FieldType.OFTInteger);
			layer.CreateField(idDef, 1);
		}
		if (layerDef.GetFieldIndex(WBLayerDateField) == -1)
		{
			using FieldDefn dateDef = new(WBLayerDateField, FieldType.OFTDate);
			layer.CreateField(dateDef, 1);
		}
	}

	private static void EnsureGEFields(this Layer layer, FeatureDefn layerDef)
	{
		if (layerDef.GetFieldIndex(QTPathField) == -1)
		{
			using FieldDefn qtPathDef = new(QTPathField, FieldType.OFTString);
			layer.CreateField(qtPathDef, 1);
		}
		if (layerDef.GetFieldIndex(GEProviderField) == -1)
		{
			using FieldDefn providerDef = new(GEProviderField, FieldType.OFTString);
			layer.CreateField(providerDef, 1);
		}
		if (layerDef.GetFieldIndex(GEEpochField) == -1)
		{
			using FieldDefn epochDef = new(GEEpochField, FieldType.OFTInteger);
			layer.CreateField(epochDef, 1);
		}
	}
}
