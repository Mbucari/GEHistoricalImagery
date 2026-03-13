namespace LibDumpedTileDatabase;

public class DumpedTile
{
	internal int DumpedTileId { get; private set; }
	internal int OperationId { get; private set; }
	public Operation? Operation { get; set; }
	public DateOnly? TileDate { get; set; }
	public DateOnly? LayerDate { get; set; }
	public int Column { get; set; }
	public int Row { get; set; }
	public int Zoom { get; set; }
	public double Latitude_Top { get; set; }
	public double Latitude_Bottom { get; set; }
	public double Longitude_Left { get; set; }
	public double Longitude_Right { get; set; }
	public string? SavedFile { get; set; }
}
