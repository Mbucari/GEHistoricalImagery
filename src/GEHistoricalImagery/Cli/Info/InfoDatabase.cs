using LibGoogleEarth;
using LibMapCommon;
using OSGeo.OGR;
using OSGeo.OSR;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace GEHistoricalImagery.Cli.Info;

internal class InfoDatabase : IDisposable
{
	public string DatabasePath { get; }
	private DataSource Database { get; }
	private Layer DumpedTiles { get; }
	private FeatureDefn DumpedTilesFeatureDef { get; }
	public InfoDatabase(string databasePath)
	{
		DatabasePath = databasePath;
		GdalLib.Register();
		using var driver = Ogr.GetDriverByName("GeoJSON");

		Database = File.Exists(databasePath) ? driver.Open(databasePath, 1) : driver.CreateDataSource(databasePath, null);

		var operationsLayer = Database.GetLayerByName("GEHI_info");
		if (operationsLayer == null)
		{
			using var sr = new SpatialReference(null);
			sr.Import<Wgs1984>();
			operationsLayer = Database.CreateLayer("GEHI_info", sr, wkbGeometryType.wkbPoint, null);
		}

		if (operationsLayer.FindFieldIndex("provider", 1) == -1)
		{
			using FieldDefn providerDef = new("provider", FieldType.OFTString);
			operationsLayer.CreateField(providerDef, 1);
		}
		if (operationsLayer.FindFieldIndex("level_infos", 1) == -1)
		{
			using FieldDefn levelInfosDef = new("level_infos", FieldType.OFTString);
			operationsLayer.CreateField(levelInfosDef, 1);
		}
		DumpedTiles = operationsLayer;
		DumpedTilesFeatureDef = operationsLayer.GetLayerDefn();
		DumpedTilesFeatureDef.GetFieldCount();
	}
	public void AddInfo(InfoData infoData)
	{
		var levelInfos = JsonSerializer.Serialize(infoData.LevelInfos, InfoDataSerilizer.Default.ListLevelInfo);
		using var feature = new Feature(DumpedTilesFeatureDef);
		DumpedTilesFeatureDef.GetFieldCount();
		feature.SetField("provider", infoData.Provider.ToString());
		feature.SetField("level_infos", levelInfos);
		using var point = new Geometry(wkbGeometryType.wkbPoint);
		point.SetPoint_2D(0, infoData.Wgs1984.Longitude, infoData.Wgs1984.Latitude);
		feature.SetGeometry(point);
		DumpedTiles.CreateFeature(feature);
	}
	public void Dispose()
	{
		DumpedTilesFeatureDef.Dispose();
		DumpedTiles.Dispose();
		Database.Dispose();
	}
}
