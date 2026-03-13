using Microsoft.EntityFrameworkCore;

namespace LibDumpedTileDatabase;

public static class DumpContextQueries
{

	extension(DumpContext context)
	{
		public List<Operation> GetOperations()
		{
			var local = context;
			return local.Operations.Include(t => t.DumpedTiles).AsEnumerable().ToList();
		}

		public Operation AddOperation(Operation operation)
		{
			return context.Operations.Add(operation).Entity;
		}

		public DumpedTile AddDumpedTile(DumpedTile tile)
		{
			return context.DumpedTiles.Add(tile).Entity;
		}
	}
}
