using FoodHub.Domain.Enums;
using FoodHub.SharedKernel;

namespace FoodHub.Domain.Restaurants.Menus;

public sealed class Menu : AggregateRoot<Guid>
{
    private Menu()
    {
    }

    public Menu(Guid restaurantId, string name, string? description, bool isDefault, bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        RestaurantId = restaurantId;
        Name = name;
        Description = description;
        IsDefault = isDefault;
        IsActive = isActive;

        Id = Guid.NewGuid();
    }

    public Guid RestaurantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? CurrentVersionId { get; private set; }

    private readonly List<MenuSchedule> _schedules = [];
    private readonly List<MenuVersion> _versions = [];

    public IReadOnlyCollection<MenuSchedule> Schedules => _schedules.AsReadOnly();
    public IReadOnlyCollection<MenuVersion> Versions => _versions.AsReadOnly();

    public void Update(string name, string? description, bool isDefault, bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Description = description;
        IsDefault = isDefault;
        IsActive = isActive;
        Touch();
    }

    public void SetCurrentVersion(Guid versionId)
    {
        if (_versions.All(v => v.Id != versionId))
            throw new InvalidOperationException($"MenuVersion {versionId} không thuộc Menu {Id}.");

        CurrentVersionId = versionId;
        Touch();
    }

    /// <summary>
    /// Xuất bản một version: version chỉ định thành Published, mọi version Published khác
    /// bị chuyển sang Archived. Đây là bất biến cốt lõi "mỗi menu chỉ có tối đa 1 version Published".
    /// </summary>
    public void PublishVersion(Guid versionId, Guid? publishedBy)
    {
        var target = _versions.FirstOrDefault(v => v.Id == versionId)
            ?? throw new InvalidOperationException($"MenuVersion {versionId} không thuộc Menu {Id}.");

        foreach (var version in _versions)
        {
            if (version.Id == versionId) continue;
            if (version.Status == MenuVersionStatus.Published)
                version.SetArchived();
        }

        target.SetPublished(publishedBy);
        CurrentVersionId = target.Id;
        Touch();
    }

    /// <summary>Version đang hiệu lực: ưu tiên CurrentVersionId, nếu không có thì lấy bản Published.</summary>
    public MenuVersion? GetCurrentVersion() =>
        _versions.FirstOrDefault(v => v.Id == CurrentVersionId)
        ?? _versions.FirstOrDefault(v => v.Status == MenuVersionStatus.Published);

    public void SetDefault(bool isDefault)
    {
        IsDefault = isDefault;
        Touch();
    }

    public MenuSchedule AddSchedule(DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, DateOnly? validFrom, DateOnly? validTo, int priority)
    {
        var schedule = new MenuSchedule(Id, dayOfWeek, startTime, endTime, validFrom, validTo, priority);
        _schedules.Add(schedule);
        return schedule;
    }

    public MenuVersion CreateVersion(int versionNumber, MenuVersionStatus status, string? changeNote)
    {
        var version = new MenuVersion(Id, versionNumber, status, changeNote);
        _versions.Add(version);
        return version;
    }

    public MenuVersion CloneVersion(MenuVersion source, int newVersionNumber, string? changeNote)
    {
        // Create new draft version
        var newVersion = new MenuVersion(Id, newVersionNumber, MenuVersionStatus.Draft, changeNote);
        _versions.Add(newVersion);

        foreach (var cat in source.MenuCategories)
        {
            var newCat = newVersion.AddCategory(cat.Name, cat.Description, cat.SortOrder, cat.IsVisible);
            foreach (var item in cat.MenuItems)
            {
                newCat.AddItem(item.ProductId, item.Price, item.DisplayName, item.IsAvailable, item.IsFeatured, item.SortOrder, item.MaxQuantityPerOrder);
            }
        }
        return newVersion;
    }
}