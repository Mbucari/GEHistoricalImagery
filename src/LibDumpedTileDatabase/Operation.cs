namespace LibDumpedTileDatabase;

public class Operation
{
	internal int OperationId { get; private set; }
	public string? Provider { get; set; }
	public string Arguments { get; private set; }
	public string? OutputDirectory { get; set; }
	public List<DumpedTile>? DumpedTiles { get; set; }

	public Operation()
	{
		var args = Environment.GetCommandLineArgs();
		for (int i = 1; i < args.Length; i++)
		{
			if (args[i].Contains(' '))
				args[i] = "\"" + args[i] + "\"";
		}
		Arguments = string.Join(" ", args.Skip(1));
	}
}
