using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Linq.Expressions;

namespace LibDumpedTileDatabase.Configurations;

internal class DumpedTileConfiguration : IEntityTypeConfiguration<DumpedTile>
{
	public void Configure(EntityTypeBuilder<DumpedTile> builder)
	{
		builder.HasKey(t => t.DumpedTileId);

		Expression<Func<DateOnly?, string?>> serialize = d => d.HasValue ? d.Value.ToString("yyyy-MM-dd") : null;
		Expression<Func<string?, DateOnly?>> deserialize = s => s == null ? null : DateOnly.ParseExact(s, "yyyy-MM-dd");
		DateOnlyComparer comparer = new();

		builder
			.Property(t => t.TileDate)
			.HasConversion(serialize, deserialize, comparer);
		builder
			.Property(t => t.LayerDate)
			.HasConversion(serialize, deserialize, comparer);
		builder
			.HasOne(t => t.Operation)
			.WithMany(o => o.DumpedTiles);
	}
}

internal class DateOnlyComparer : ValueComparer<DateOnly>
{
	public DateOnlyComparer() : base(
		(d1, d2) => d1 == d2,
		d => d.GetHashCode())
	{
	}
}
