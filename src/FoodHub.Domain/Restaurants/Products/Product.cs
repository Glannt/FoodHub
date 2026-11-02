using FoodHub.Domain.Enums;
using FoodHub.SharedKernel;

namespace FoodHub.Domain.Restaurants.Products;

public abstract class Product : AggregateRoot<Guid>
{
    protected Product()
    {
    }

    protected Product(
        Guid restaurantId,
        Guid productCategoryId,
        string name,
        string? description,
        decimal basePrice,
        string? imageUrl,
        string? sku,
        int preparationTimeMinutes,
        bool isActive,
        ProductType productType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(basePrice);
        ArgumentOutOfRangeException.ThrowIfNegative(preparationTimeMinutes);

        RestaurantId = restaurantId;
        ProductCategoryId = productCategoryId;
        Name = name;
        Description = description;
        BasePrice = basePrice;
        ImageUrl = imageUrl;
        SKU = sku;
        PreparationTimeMinutes = preparationTimeMinutes;
        IsActive = isActive;
        ProductType = productType;

        Id = Guid.NewGuid();
    }

    public Guid RestaurantId { get; private set; }
    public Guid ProductCategoryId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal BasePrice { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? SKU { get; private set; }
    public int PreparationTimeMinutes { get; private set; }
    public bool IsActive { get; private set; }
    public ProductType ProductType { get; private set; }

    public void UpdateBaseInfo(string name, string? description, decimal basePrice, string? imageUrl, string? sku, int preparationTimeMinutes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(basePrice);
        ArgumentOutOfRangeException.ThrowIfNegative(preparationTimeMinutes);

        Name = name;
        Description = description;
        BasePrice = basePrice;
        ImageUrl = imageUrl;
        SKU = sku;
        PreparationTimeMinutes = preparationTimeMinutes;
        Touch();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        Touch();
    }
}