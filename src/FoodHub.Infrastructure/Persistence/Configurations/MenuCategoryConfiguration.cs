using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FoodHub.Domain.Restaurants.Menus;

namespace FoodHub.Infrastructure.Persistence.Configurations;

public sealed class MenuCategoryConfiguration : IEntityTypeConfiguration<MenuCategory>
{
    public void Configure(EntityTypeBuilder<MenuCategory> builder)
    {
        builder.ToTable("menu_categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.MenuVersionId).HasColumnName("menu_version_id");
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(c => c.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(c => c.SortOrder).HasColumnName("sort_order");
        builder.Property(c => c.IsVisible).HasColumnName("is_visible").HasDefaultValue(true);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");

        builder.HasMany(c => c.MenuItems)
            .WithOne()
            .HasForeignKey(i => i.MenuCategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(c => c.MenuItems)
            .HasField("_menuItems")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(c => new { c.MenuVersionId, c.SortOrder });
    }
}
