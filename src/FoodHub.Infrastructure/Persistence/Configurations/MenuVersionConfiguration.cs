using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FoodHub.Domain.Restaurants.Menus;

namespace FoodHub.Infrastructure.Persistence.Configurations;

public sealed class MenuVersionConfiguration : IEntityTypeConfiguration<MenuVersion>
{
    public void Configure(EntityTypeBuilder<MenuVersion> builder)
    {
        builder.ToTable("menu_versions");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id).HasColumnName("id");
        builder.Property(v => v.MenuId).HasColumnName("menu_id");
        builder.Property(v => v.VersionNumber).HasColumnName("version_number");
        builder.Property(v => v.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(v => v.PublishedAt).HasColumnName("published_at");
        builder.Property(v => v.PublishedBy).HasColumnName("published_by");
        builder.Property(v => v.ChangeNote).HasColumnName("change_note").HasMaxLength(1000);
        builder.Property(v => v.CreatedAt).HasColumnName("created_at");
        builder.Property(v => v.UpdatedAt).HasColumnName("updated_at");

        builder.HasMany(v => v.MenuCategories)
            .WithOne()
            .HasForeignKey(c => c.MenuVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(v => v.MenuCategories)
            .HasField("_menuCategories")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Mỗi menu chỉ được có một version trùng số.
        builder.HasIndex(v => new { v.MenuId, v.VersionNumber }).IsUnique();
    }
}
