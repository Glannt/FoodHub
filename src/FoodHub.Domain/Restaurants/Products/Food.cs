using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Restaurants.Products;

public sealed class Food : Product
{
    private Food()
    {
    }

    public Food(
        Guid restaurantId,
        Guid productCategoryId,
        string name,
        string? description,
        decimal basePrice,
        string? imageUrl,
        string? sku,
        int preparationTimeMinutes,
        bool isActive,
        SpiceLevel spiceLevel,
        bool isVegetarian,
        int? calories,
        string? allergens,
        string? portionSize)
        : base(restaurantId, productCategoryId, name, description, basePrice, imageUrl, sku, preparationTimeMinutes, isActive, ProductType.Food)
    {
        SpiceLevel = spiceLevel;
        IsVegetarian = isVegetarian;
        Calories = calories;
        Allergens = allergens;
        PortionSize = portionSize;
    }

    public SpiceLevel SpiceLevel { get; private set; }
    public bool IsVegetarian { get; private set; }
    public int? Calories { get; private set; }
    public string? Allergens { get; private set; }
    public string? PortionSize { get; private set; }

    public void UpdateFoodDetails(SpiceLevel spiceLevel, bool isVegetarian, int? calories, string? allergens, string? portionSize)
    {
        SpiceLevel = spiceLevel;
        IsVegetarian = isVegetarian;
        Calories = calories;
        Allergens = allergens;
        PortionSize = portionSize;
        Touch();
    }
}