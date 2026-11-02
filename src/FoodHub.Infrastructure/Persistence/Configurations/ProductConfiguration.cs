using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FoodHub.Domain.Enums;
using FoodHub.Domain.Restaurants;
using FoodHub.Domain.Restaurants.Products;

namespace FoodHub.Infrastructure.Persistence.Configurations;

/// <summary>
/// Ánh xạ cây kế thừa <see cref="Product"/> (Food/Drink) vào một bảng duy nhất
/// theo chiến lược TPH. Cột <c>product_type</c> vừa là dữ liệu nghiệp vụ vừa là
/// discriminator để EF biết phải materialize <see cref="Food"/> hay <see cref="Drink"/>.
/// </summary>
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", t =>
            t.HasCheckConstraint("ck_products_base_price", "base_price > 0"));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.RestaurantId).HasColumnName("restaurant_id");
        builder.Property(p => p.ProductCategoryId).HasColumnName("product_category_id");
        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(p => p.BasePrice).HasColumnName("base_price").HasColumnType("numeric(18,2)");
        builder.Property(p => p.ImageUrl).HasColumnName("image_url").HasMaxLength(500);
        builder.Property(p => p.SKU).HasColumnName("sku").HasMaxLength(100);
        builder.Property(p => p.PreparationTimeMinutes).HasColumnName("preparation_time_minutes");
        builder.Property(p => p.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(p => p.ProductType).HasColumnName("product_type");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");

        builder.HasDiscriminator(p => p.ProductType)
            .HasValue<Food>(ProductType.Food)
            .HasValue<Drink>(ProductType.Drink);

        builder.HasOne<Restaurant>()
            .WithMany()
            .HasForeignKey(p => p.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ProductCategory>()
            .WithMany()
            .HasForeignKey(p => p.ProductCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.RestaurantId);
        builder.HasIndex(p => new { p.RestaurantId, p.SKU })
            .IsUnique()
            .HasFilter("\"sku\" IS NOT NULL");
    }
}
