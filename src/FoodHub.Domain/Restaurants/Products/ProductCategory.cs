using FoodHub.SharedKernel;

namespace FoodHub.Domain.Restaurants.Products;

public sealed class ProductCategory : AggregateRoot<Guid>
{
    private ProductCategory()
    {
    }

    public ProductCategory(Guid restaurantId, string name, string? description, Guid? parentCategoryId, int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(sortOrder);

        RestaurantId = restaurantId;
        Name = name;
        Description = description ?? string.Empty;
        ParentCategoryId = parentCategoryId;
        SortOrder = sortOrder;

        Id = Guid.NewGuid();
    }

    public Guid RestaurantId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public Guid? ParentCategoryId { get; private set; }

    public int SortOrder { get; private set; }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Touch();
    }

    public void UpdateDetails(string? description, Guid? parentCategoryId, int sortOrder)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sortOrder);
        Description = description ?? string.Empty;
        ParentCategoryId = parentCategoryId;
        SortOrder = sortOrder;
        Touch();
    }
}