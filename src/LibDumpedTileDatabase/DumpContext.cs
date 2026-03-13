using LibDumpedTileDatabase.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LibDumpedTileDatabase;

public class SqliteContextFactory : IDesignTimeDbContextFactory<DumpContext>
{
	public DumpContext CreateDbContext(string[] args)
	{
		return DumpContext.Create(string.Empty);
	}
}

public class DumpContext : DbContext
{
	public const string DefaultDbFileName = "DumpTiles.db";
	public DbSet<Operation> Operations { get; private set; }
	public DbSet<DumpedTile> DumpedTiles { get; private set; }

	private DumpContext(DbContextOptions<DumpContext> options)
		: base(options) { }

	public static DumpContext Create(DirectoryInfo directory)
	{
		directory.Create();
		var dbFile = Path.Combine(directory.FullName, DefaultDbFileName);

		if (!File.Exists(dbFile))
		{
			var emptyDb = Path.Combine(AppContext.BaseDirectory, DefaultDbFileName);
			if (!File.Exists(emptyDb))
				throw new FileNotFoundException($"The empty database file '{emptyDb}' was not found in the program files directory.");

			File.Copy(emptyDb, dbFile);
		}
		return Create(dbFile);
	}

	public static DumpContext Create(string dbFile)
	{
		string connectionString = $"Data Source={dbFile};";
		var options
			= new DbContextOptionsBuilder<DumpContext>()
			.EnableSensitiveDataLogging()
			.UseSqlite(connectionString);
		return new DumpContext(options.Options);
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		modelBuilder.ApplyConfiguration(new OperationConfiguration());
		modelBuilder.ApplyConfiguration(new DumpedTileConfiguration());
	}
}
