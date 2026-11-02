using FoodHub.Domain.Enums;

namespace FoodHub.Domain.Restaurants.Products;

public sealed class Drink : Product
{
    private Drink()
    {
    }

    public Drink(
        Guid restaurantId,
        Guid productCategoryId,
        string name,
        string? description,
        decimal basePrice,
        string? imageUrl,
        string? sku,
        int preparationTimeMinutes,
        bool isActive,
        int volumeMl,
        bool isAlcoholic,
        bool isServedCold,
        string? sugarLevelOptions)
        : base(restaurantId, productCategoryId, name, description, basePrice, imageUrl, sku, preparationTimeMinutes, isActive, ProductType.Drink)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(volumeMl);

        VolumeMl = volumeMl;
        IsAlcoholic = isAlcoholic;
        IsServedCold = isServedCold;
        SugarLevelOptions = sugarLevelOptions;
    }

    public int VolumeMl { get; private set; }
    public bool IsAlcoholic { get; private set; }
    public bool IsServedCold { get; private set; }
    public string? SugarLevelOptions { get; private set; }

    public void UpdateDrinkDetails(int volumeMl, bool isAlcoholic, bool isServedCold, string? sugarLevelOptions)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(volumeMl);

        VolumeMl = volumeMl;
        IsAlcoholic = isAlcoholic;
        IsServedCold = isServedCold;
        SugarLevelOptions = sugarLevelOptions;
        Touch();
    }
}