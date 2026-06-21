using LibMapCommon;
using LibMapCommon.Geometry;
using OSGeo.OGR;
using OSGeo.OSR;

namespace GEHistoricalImagery.Cli.Availability;

internal static class AvailabilityDatabase
{

	const string LayerName = "GEHI_Availability";
	const string ProviderField = "provider";
	const string ZoomLevelField = "zoom_level";
	const string ImageryDateField = "image_date";
	const string IsCompleteField = "iscomplete";

	const string WBLayerNameField = "layer_name";
	const string WBLayerIdField = "layer_id";
	const string WBLayerDateField = "layer_date";

	public static void SaveAvailabilityData(this IDatedRegion[] regions, string filename, Provider provider, string driverName)
	{
		if (regions is null || regions.Length == 0)
			throw new ArgumentException("No regions to save.", nameof(regions));

		using var driver = Ogr.GetDriverByName(driverName);
		using var database = File.Exists(filename) ? driver.Open(filename, 1) : driver.CreateDataSource(filename, null);
		if (database is null)
		{
			Console.Error.WriteLine($"Failed to create or open database at {filename}");
			return;
		}

		using var srcSr = regions[0].GetSpatialReference();
		using var infoDataLayer = database.GetOrCreateLayer(srcSr);
		if (infoDataLayer is null)
		{
			Console.Error.WriteLine($"Failed to create or open layer in database at {filename}");
			return;
		}

		ProgressWriter.Instance.BeginProgress($"Saving availability data to {driverName}");
		using var layerDef = infoDataLayer.GetLayerDefn();
		using var layerSr = infoDataLayer.GetSpatialRef() ?? srcSr;

		infoDataLayer.EnsureSharedFields(layerDef);
		infoDataLayer.EnsureWaybackFields(layerDef);

		for (int i = 0; i < regions.Length; i++)
		{
			var region = regions[i];
			using var feature = new Feature(layerDef);
			using var geometry = region.GetMultiPolygon();
			geometry.TransformTo(layerSr);

			feature.SetGeometryDirectly(geometry);
			feature.SetField(ProviderField, provider.ToString());
			feature.SetField(ZoomLevelField, region.ZoomLevel);
			feature.SetField(ImageryDateField, region.Date);
			feature.SetField(IsCompleteField, region.IsComplete ? 1 : 0);

			if (region is LibEsri.Geometry.DatedRegion edr)
			{
				feature.SetField(WBLayerNameField, edr.Layer.Title);
				feature.SetField(WBLayerIdField, edr.Layer.ID);
				feature.SetField(WBLayerDateField, edr.Layer.Date);
			}
			
			infoDataLayer.CreateFeature(feature);
			ProgressWriter.Instance.ReportProgress(i / (double)regions.Length);
		}
		ProgressWriter.Instance.EndProgress();
	}

	private static Layer GetOrCreateLayer(this DataSource database, SpatialReference sr)
		=> database.GetLayerByName(LayerName) is { } layer ? layer
		: database.GetLayerByIndex(0) is { } firstLayer ? firstLayer
		: database.CreateLayer(LayerName, sr, wkbGeometryType.wkbMultiPolygon, null);

	private static void EnsureSharedFields(this Layer layer, FeatureDefn layerDef)
	{
		layer.EnsureFields(layerDef, ProviderField, FieldType.OFTString, 7);
		layer.EnsureFields(layerDef, ZoomLevelField, FieldType.OFTInteger, 2);
		layer.EnsureFields(layerDef, ImageryDateField, FieldType.OFTDate);
		layer.EnsureFields(layerDef, IsCompleteField, FieldType.OFTInteger, 1);
	}

	private static void EnsureWaybackFields(this Layer layer, FeatureDefn layerDef)
	{
		layer.EnsureFields(layerDef, WBLayerNameField, FieldType.OFTString, 34);
		layer.EnsureFields(layerDef, WBLayerIdField, FieldType.OFTInteger);
		layer.EnsureFields(layerDef, WBLayerDateField, FieldType.OFTDate);
	}

	private static void EnsureFields(this Layer layer, FeatureDefn layerDef, string fieldName, FieldType fieldType, int fieldSize = 0)
	{
		if (layerDef.GetFieldIndex(fieldName) == -1)
		{
			using FieldDefn def = new(fieldName, fieldType);
			if (fieldSize > 0)
			{
				def.SetWidth(fieldSize);
			}
			layer.CreateField(def, 1);
		}
	}
}
