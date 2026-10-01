using FoodHub.Domain.Restaurants;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Restaurant> Restaurants { get; }

    DbSet<MenuCategory> MenuCategories { get; }

    DbSet<MenuItem> MenuItems { get; }
}
