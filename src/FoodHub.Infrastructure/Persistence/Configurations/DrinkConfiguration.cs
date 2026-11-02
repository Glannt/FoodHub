using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FoodHub.Domain.Restaurants.Products;

namespace FoodHub.Infrastructure.Persistence.Configurations;

public sealed class DrinkConfiguration : IEntityTypeConfiguration<Drink>
{
    public void Configure(EntityTypeBuilder<Drink> builder)
    {
        builder.Property(d => d.VolumeMl).HasColumnName("drink_volume_ml");
        builder.Property(d => d.IsAlcoholic).HasColumnName("drink_is_alcoholic");
        builder.Property(d => d.IsServedCold).HasColumnName("drink_is_served_cold");
        builder.Property(d => d.SugarLevelOptions)
            .HasColumnName("drink_sugar_level_options")
            .HasMaxLength(200);
    }
}
