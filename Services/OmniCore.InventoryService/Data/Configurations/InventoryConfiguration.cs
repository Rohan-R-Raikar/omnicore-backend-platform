using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OmniCore.InventoryService.Models.Entities;

namespace OmniCore.InventoryService.Data.Configurations;

public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
	public void Configure(EntityTypeBuilder<Inventory> builder)
	{
		builder.ToTable("Inventories");

		builder.HasKey(inventory => inventory.Id);

		builder.Property(inventory => inventory.ProductId)
			.IsRequired();

		builder.HasIndex(inventory => inventory.ProductId)
			.IsUnique();

		builder.Property(inventory => inventory.AvailableQuantity)
			.IsRequired();

		builder.Property(inventory => inventory.ReservedQuantity)
			.IsRequired();

		builder.Property(inventory => inventory.UpdatedAt)
			.IsRequired();

        builder.Property(inventory => inventory.RowVersion)
			.IsRowVersion()
			.IsConcurrencyToken();

        builder.ToTable(table =>
		{
			table.HasCheckConstraint(
				"CK_Inventories_AvailableQuantity",
				"[AvailableQuantity] >= 0");

			table.HasCheckConstraint(
				"CK_Inventories_ReservedQuantity",
				"[ReservedQuantity] >= 0");
		});
	}
}