using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FoodHub.Domain.Restaurants;
using FoodHub.Domain.Restaurants.Products;

namespace FoodHub.Infrastructure.Persistence.Configurations;

public sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("product_categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.RestaurantId).HasColumnName("restaurant_id");
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(c => c.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(c => c.ParentCategoryId).HasColumnName("parent_category_id");
        builder.Property(c => c.SortOrder).HasColumnName("sort_order");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne<Restaurant>()
            .WithMany()
            .HasForeignKey(c => c.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Danh mục cha bị xoá thì danh mục con thành danh mục gốc, không bị xoá dây chuyền.
        builder.HasOne<ProductCategory>()
            .WithMany()
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.RestaurantId, c.Name });
    }
}
