using LibMapCommon;
using LibMapCommon.Geometry;
using OSGeo.OGR;
using OSGeo.OSR;

namespace GEHistoricalImagery.Cli.Availability;

internal static class AvailabilityDatabase
{

	const string LayerName = "GEHI_Info";
	const string ProviderField = "provider";
	const string ZoomLevelField = "zoom_level";
	const string ImageryDateField = "imagery_date";
	const string IsCompleteField = "is_complete";

	const string WBLayerNameField = "wb_layer_name";
	const string WBLayerIdField = "wb_layer_id";
	const string WBLayerDateField = "wb_layer_date";

	public static void SaveInfoData<T>(this DatedRegion<T>[] regions, string filename, int zoomLevel, Provider provider)
		where T : IGeoCoordinate<T>
	{
		using var driver = Ogr.GetDriverByName("GeoJSON");
		using var database = File.Exists(filename) ? driver.Open(filename, 1) : driver.CreateDataSource(filename, null);
		if (database is null)
		{
			Console.Error.WriteLine($"Failed to create or open database at {filename}");
			return;
		}

		using var infoDataLayer = database.GetOrCreateLayer();
		if (infoDataLayer is null)
		{
			Console.Error.WriteLine($"Failed to create or open layer in database at {filename}");
			return;
		}

		using var layerDef = infoDataLayer.GetLayerDefn();
		using var targetSr = new SpatialReference(null);
		targetSr.Import<Wgs1984>();

		infoDataLayer.EnsureSharedFields(layerDef);
		infoDataLayer.EnsureWaybackFields(layerDef);

		foreach (var region in regions)
		{
			using var feature = new Feature(layerDef);
			using var geometry = region.GetMultiPolygon();
			geometry.TransformTo(targetSr);

			feature.SetGeometryDirectly(geometry);
			feature.SetField(ProviderField, provider.ToString());
			feature.SetField(ZoomLevelField, zoomLevel);
			feature.SetField(ImageryDateField, region.Date);
			feature.SetField(IsCompleteField, region.IsComplete ? 1 : 0);

			if (region is LibEsri.Geometry.DatedRegion edr)
			{
				feature.SetField(WBLayerNameField, edr.Layer.Title);
				feature.SetField(WBLayerIdField, edr.Layer.ID);
				feature.SetField(WBLayerDateField, edr.Layer.Date);
			}
			
			infoDataLayer.CreateFeature(feature);
		}
	}

	private static Layer GetOrCreateLayer(this DataSource database)
	{
		if (database.GetLayerByName(LayerName) is not { } layer)
		{
			using var sr = new SpatialReference(null);
			sr.Import<Wgs1984>();
			layer = database.CreateLayer(LayerName, sr, wkbGeometryType.wkbMultiPolygon, null);
		}
		return layer;
	}

	private static void EnsureSharedFields(this Layer layer, FeatureDefn layerDef)
	{
		layer.EnsureFields(layerDef, ProviderField, FieldType.OFTString);
		layer.EnsureFields(layerDef, ZoomLevelField, FieldType.OFTInteger);
		layer.EnsureFields(layerDef, ImageryDateField, FieldType.OFTDate);
		layer.EnsureFields(layerDef, IsCompleteField, FieldType.OFTInteger);
	}

	private static void EnsureWaybackFields(this Layer layer, FeatureDefn layerDef)
	{
		layer.EnsureFields(layerDef, WBLayerNameField, FieldType.OFTString);
		layer.EnsureFields(layerDef, WBLayerIdField, FieldType.OFTInteger);
		layer.EnsureFields(layerDef, WBLayerDateField, FieldType.OFTDate);
	}

	private static void EnsureFields(this Layer layer, FeatureDefn layerDef, string fieldName, FieldType fieldType)
	{
		if (layerDef.GetFieldIndex(fieldName) == -1)
		{
			using FieldDefn def = new(fieldName, fieldType);
			layer.CreateField(def, 1);
		}
	}
}
