using FoodHub.Domain.Restaurants;
using FoodHub.Domain.Restaurants.Menus;
using FoodHub.Domain.Restaurants.Products;
using FoodHub.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Restaurant> Restaurants { get; }
    DbSet<User> Users { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductCategory> ProductCategories { get; }
    DbSet<Menu> Menus { get; }
    DbSet<MenuVersion> MenuVersions { get; }
    DbSet<MenuSchedule> MenuSchedules { get; }
    DbSet<MenuItem> MenuItems { get; }
    DbSet<MenuCategory> MenuCategories { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
