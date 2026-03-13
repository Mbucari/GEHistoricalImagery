using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibDumpedTileDatabase.Configurations;

internal class OperationConfiguration : IEntityTypeConfiguration<Operation>
{
	public void Configure(EntityTypeBuilder<Operation> builder)
	{
		builder.HasKey(o => o.OperationId);

		builder
			.HasMany(o => o.DumpedTiles)
			.WithOne(dt => dt.Operation)
			.HasForeignKey(dt => dt.OperationId)
			.IsRequired();
	}
}
