using FoodHub.SharedKernel;

namespace FoodHub.Domain.Restaurants.Menus;

public sealed class MenuCategory : Entity<Guid>
{
    private MenuCategory()
    {
    }

    public MenuCategory(Guid menuVersionId, string name, string? description, int sortOrder, bool isVisible)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(sortOrder);

        MenuVersionId = menuVersionId;
        Name = name;
        Description = description;
        SortOrder = sortOrder;
        IsVisible = isVisible;

        Id = Guid.NewGuid();
    }

    public Guid MenuVersionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsVisible { get; private set; }

    private readonly List<MenuItem> _menuItems = [];
    public IReadOnlyCollection<MenuItem> MenuItems => _menuItems.AsReadOnly();

    public void Update(string name, string? description, int sortOrder, bool isVisible)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(sortOrder);

        Name = name;
        Description = description;
        SortOrder = sortOrder;
        IsVisible = isVisible;
        Touch();
    }

    public void SetVisible(bool isVisible)
    {
        IsVisible = isVisible;
        Touch();
    }

    public MenuItem AddItem(Guid productId, decimal price, string? displayName, bool isAvailable, bool isFeatured, int sortOrder, int? maxQuantityPerOrder)
    {
        var item = new MenuItem(Id, productId, price, displayName, isAvailable, isFeatured, sortOrder, maxQuantityPerOrder);
        _menuItems.Add(item);
        return item;
    }
}