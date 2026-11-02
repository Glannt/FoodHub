using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FoodHub.Domain.Restaurants.Menus;
using FoodHub.Domain.Restaurants.Products;

namespace FoodHub.Infrastructure.Persistence.Configurations;

public sealed class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("menu_items", t =>
            t.HasCheckConstraint("ck_menu_items_price", "price > 0"));

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id).HasColumnName("id");
        builder.Property(i => i.MenuCategoryId).HasColumnName("menu_category_id");
        builder.Property(i => i.ProductId).HasColumnName("product_id");
        builder.Property(i => i.Price).HasColumnName("price").HasColumnType("numeric(18,2)");
        builder.Property(i => i.DisplayName).HasColumnName("display_name").HasMaxLength(200);
        builder.Property(i => i.IsAvailable).HasColumnName("is_available").HasDefaultValue(true);
        builder.Property(i => i.IsFeatured).HasColumnName("is_featured").HasDefaultValue(false);
        builder.Property(i => i.SortOrder).HasColumnName("sort_order");
        builder.Property(i => i.MaxQuantityPerOrder).HasColumnName("max_quantity_per_order");
        builder.Property(i => i.CreatedAt).HasColumnName("created_at");
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at");

        // Không xoá Product khi đang được bán trong thực đơn: phải gỡ khỏi menu trước.
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.MenuCategoryId, i.SortOrder });
        builder.HasIndex(i => i.ProductId);
    }
}
