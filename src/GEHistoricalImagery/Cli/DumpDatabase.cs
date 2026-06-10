using LibMapCommon;
using LibMapCommon.Geometry;
using OSGeo.OGR;
using OSGeo.OSR;

namespace GEHistoricalImagery.Cli;

internal class DumpDatabase : IDisposable
{
	public string DatabasePath { get; }
	private DataSource Database { get; }
	private Layer DumpedTiles { get; }
	private FeatureDefn DumpedTilesFeatureDef { get; }
	public long OperationId { get; }
	public DumpDatabase(string databasePath, GeoRegion<Wgs1984> region, Provider provider, int zoom, string? outputDirectory)
	{
		DatabasePath = databasePath;
		using var driver = Ogr.GetDriverByName("SQLite");

		Database = File.Exists(databasePath) ? driver.Open(databasePath, 1) : driver.CreateDataSource(databasePath, null);
		using var regionPoly = region.GetMultiPolygon();

		var operationsLayer = Database.GetLayerByName("dump_operations");
		if (operationsLayer == null)
		{
			using var sr = regionPoly.GetSpatialReference();
			operationsLayer = Database.CreateLayer("dump_operations", sr, wkbGeometryType.wkbMultiPolygon, ["FORMAT=WKT", "FID=dump_operation_id", "GEOMETRY_NAME=region_wkt"]);
			FieldDefn providerDef = new("provider", FieldType.OFTString);
			FieldDefn zoomDef = new("zoom_level", FieldType.OFTInteger);
			FieldDefn outDirDef = new("output_directory", FieldType.OFTString);
			FieldDefn commandDef = new("command_line", FieldType.OFTString);
			operationsLayer.CreateField(providerDef, 1);
			operationsLayer.CreateField(zoomDef, 1);
			operationsLayer.CreateField(outDirDef, 1);
			operationsLayer.CreateField(commandDef, 1);
		}

		DumpedTiles = Database.GetLayerByName("dumped_tiles");
		if (DumpedTiles == null)
		{
			using var sr = regionPoly.GetSpatialReference();
			using (var dumpedTiles = Database.CreateLayer("dumped_tiles", sr, wkbGeometryType.wkbPolygon, ["FORMAT=WKT", "FID=dumped_tile_id", "GEOMETRY_NAME=tile_perimeter_wkt"]))
			{
				FieldDefn operation_id = new("dump_operation_id", FieldType.OFTInteger);
				FieldDefn rowDef = new("row", FieldType.OFTInteger);
				FieldDefn columnDef = new("column", FieldType.OFTInteger);
				FieldDefn dateDef = new("date", FieldType.OFTDate);
				FieldDefn savedFileDef = new("saved_file", FieldType.OFTString);
				dumpedTiles.CreateField(operation_id, 1);
				dumpedTiles.CreateField(rowDef, 1);
				dumpedTiles.CreateField(columnDef, 1);
				dumpedTiles.CreateField(dateDef, 1);
				dumpedTiles.CreateField(savedFileDef, 1);
			}
			//Drop the table and re-create it with the foreign key constraint sincethe OGR
			//SQLite driver does not support creating tables with foreign key constraints
			var creationStatement = ExecuteStringQuery("SELECT sql FROM sqlite_master WHERE type='table' AND name='dumped_tiles';");
			ExecuteNoQuery("DROP TABLE dumped_tiles;");

			const string constraint = """
				, CONSTRAINT "FK_dumped_tiles_dump_operations_dump_operation_id" FOREIGN KEY ("dump_operation_id") REFERENCES "dump_operations" ("dump_operation_id") ON DELETE CASCADE
				""";
			var newTable = creationStatement.Insert(creationStatement.LastIndexOf(')'), constraint);
			ExecuteNoQuery(newTable);

			ExecuteNoQuery("""
			CREATE INDEX "IX_dumped_tiles_dump_operation_id" ON "dumped_tiles" ("dump_operation_id")
			""");
			DumpedTiles = Database.GetLayerByName("dumped_tiles");
		}

		DumpedTilesFeatureDef = DumpedTiles.GetLayerDefn();

		using FeatureDefn ldef = operationsLayer.GetLayerDefn();
		int fieldCount = ldef.GetFieldCount();
		using FeatureDefn featureDefn = new FeatureDefn(null);
		for (int i = 0; i < fieldCount; i++)
		{
			using var fieldDefn = ldef.GetFieldDefn(i);
			featureDefn.AddFieldDefn(fieldDefn);
		}
		using Feature feature = new Feature(featureDefn);
		feature.SetGeometry(regionPoly);
		feature.SetField("provider", provider.ToString());
		feature.SetField("command_line", Environment.CommandLine);
		feature.SetField("output_directory", outputDirectory);
		feature.SetField("zoom_level", zoom);
		operationsLayer.CreateFeature(feature);
		OperationId = feature.GetFID();
	}

	public void AddDumpedTile(ITileDataset dumpedTile, string savedFile)
	{
		var date = dumpedTile.LayerDate ?? dumpedTile.TileDate;
		using var tileEntry = new Feature(DumpedTilesFeatureDef);
		tileEntry.SetField("dump_operation_id", OperationId);
		tileEntry.SetField("row", dumpedTile.Tile.Row);
		tileEntry.SetField("column", dumpedTile.Tile.Column);
		tileEntry.SetField("date", date.Year, date.Month, date.Day, 0, 0, 0, 0);
		tileEntry.SetField("saved_file", Path.GetFileName(savedFile));

		using Geometry geom = dumpedTile.GetPolygonGeometry();

		using var src = geom.GetSpatialReference();
		using var dst = DumpedTiles.GetSpatialRef();
		if (src.IsSame(dst, null) == 0)
		{
			using var transform = new CoordinateTransformation(src, dst);
			geom.Transform(transform);
		}

		tileEntry.SetGeometryDirectly(geom);
		DumpedTiles.CreateFeature(tileEntry);
	}

	private string ExecuteStringQuery(string query)
	{
		using var result = Database.ExecuteSQL(query, null, "SQLITE");
		using var feature = result.GetFeature(0);
		var fstr = feature.GetFieldAsString(0);
		Database.ReleaseResultSet(result);
		return fstr;
	}

	private void ExecuteNoQuery(string query)
	{
		using var result = Database.ExecuteSQL(query, null, "SQLITE");
		Database.ReleaseResultSet(result);
	}

	public void Dispose()
	{
		DumpedTiles.Dispose();
		DumpedTilesFeatureDef.Dispose();
		Database.Dispose();
	}
}
