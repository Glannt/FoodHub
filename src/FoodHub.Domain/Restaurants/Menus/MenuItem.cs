using FoodHub.SharedKernel;

namespace FoodHub.Domain.Restaurants.Menus;

public sealed class MenuItem : Entity<Guid>
{
    private MenuItem()
    {
    }

    public MenuItem(
        Guid menuCategoryId,
        Guid productId,
        decimal price,
        string? displayName,
        bool isAvailable,
        bool isFeatured,
        int sortOrder,
        int? maxQuantityPerOrder)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);
        ArgumentOutOfRangeException.ThrowIfNegative(sortOrder);
        if (maxQuantityPerOrder.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxQuantityPerOrder.Value);

        MenuCategoryId = menuCategoryId;
        ProductId = productId;
        Price = price;
        DisplayName = displayName;
        IsAvailable = isAvailable;
        IsFeatured = isFeatured;
        SortOrder = sortOrder;
        MaxQuantityPerOrder = maxQuantityPerOrder;

        Id = Guid.NewGuid();
    }

    public Guid MenuCategoryId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal Price { get; private set; }
    public string? DisplayName { get; private set; }
    public bool IsAvailable { get; private set; }
    public bool IsFeatured { get; private set; }
    public int SortOrder { get; private set; }
    public int? MaxQuantityPerOrder { get; private set; }

    public void Update(decimal price, string? displayName, bool isAvailable, bool isFeatured, int sortOrder, int? maxQuantityPerOrder)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);
        ArgumentOutOfRangeException.ThrowIfNegative(sortOrder);
        if (maxQuantityPerOrder.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxQuantityPerOrder.Value);

        Price = price;
        DisplayName = displayName;
        IsAvailable = isAvailable;
        IsFeatured = isFeatured;
        SortOrder = sortOrder;
        MaxQuantityPerOrder = maxQuantityPerOrder;
        Touch();
    }

    public void SetAvailable(bool isAvailable)
    {
        IsAvailable = isAvailable;
        Touch();
    }

    public void SetFeatured(bool isFeatured)
    {
        IsFeatured = isFeatured;
        Touch();
    }
}