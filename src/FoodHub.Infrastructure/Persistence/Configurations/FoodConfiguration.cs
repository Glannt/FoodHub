using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FoodHub.Domain.Restaurants.Products;

namespace FoodHub.Infrastructure.Persistence.Configurations;

public sealed class FoodConfiguration : IEntityTypeConfiguration<Food>
{
    public void Configure(EntityTypeBuilder<Food> builder)
    {
        builder.Property(f => f.SpiceLevel)
            .HasColumnName("food_spice_level")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(f => f.IsVegetarian).HasColumnName("food_is_vegetarian");
        builder.Property(f => f.Calories).HasColumnName("food_calories");
        builder.Property(f => f.Allergens).HasColumnName("food_allergens").HasMaxLength(500);
        builder.Property(f => f.PortionSize).HasColumnName("food_portion_size").HasMaxLength(100);
    }
}
