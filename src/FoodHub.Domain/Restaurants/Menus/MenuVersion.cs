using FoodHub.Domain.Enums;
using FoodHub.SharedKernel;

namespace FoodHub.Domain.Restaurants.Menus;

public sealed class MenuVersion : Entity<Guid>
{
    private MenuVersion()
    {
    }

    public MenuVersion(Guid menuId, int versionNumber, MenuVersionStatus status, string? changeNote)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(versionNumber);

        MenuId = menuId;
        VersionNumber = versionNumber;
        Status = status;
        ChangeNote = changeNote;
        if (status == MenuVersionStatus.Published)
        {
            PublishedAt = DateTimeOffset.UtcNow;
        }

        Id = Guid.NewGuid();
    }

    public Guid MenuId { get; private set; }
    public int VersionNumber { get; private set; }
    public MenuVersionStatus Status { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public Guid? PublishedBy { get; private set; }
    public string? ChangeNote { get; private set; }

    private readonly List<MenuCategory> _menuCategories = [];
    public IReadOnlyCollection<MenuCategory> MenuCategories => _menuCategories.AsReadOnly();

    public MenuCategory AddCategory(string name, string? description, int sortOrder, bool isVisible)
    {
        var category = new MenuCategory(Id, name, description, sortOrder, isVisible);
        _menuCategories.Add(category);
        return category;
    }

    public void SetPublished(Guid? publishedBy)
    {
        Status = MenuVersionStatus.Published;
        PublishedAt = DateTimeOffset.UtcNow;
        PublishedBy = publishedBy;
        Touch();
    }

    public void SetArchived()
    {
        Status = MenuVersionStatus.Archived;
        Touch();
    }

    public void SetDraft(string? changeNote)
    {
        Status = MenuVersionStatus.Draft;
        ChangeNote = changeNote;
        PublishedAt = null;
        PublishedBy = null;
        Touch();
    }
}