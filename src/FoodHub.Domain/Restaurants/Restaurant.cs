using FoodHub.Domain.Enums;
using FoodHub.SharedKernel;

namespace FoodHub.Domain.Restaurants;

public sealed class Restaurant : AggregateRoot<Guid>
{
    private Restaurant()
    {
    }

    public Restaurant(
        string name,
        string slug,
        string? description,
        string phoneNumber,
        string email,
        string address,
        double latitude,
        double longtitude,
        string? logoUrl,
        string? coverImageUrl,
        TimeOnly openingTime,
        TimeOnly closingTime,
        decimal minOrderAmount,
        RestaurantStatus status,
        bool isAcceptingOrders,
        Guid ownerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        Name = name;
        Slug = slug;
        Description = description;
        PhoneNumber = phoneNumber;
        Email = email;
        Address = address;
        Latitude = latitude;
        Longtitude = longtitude;
        LogoUrl = logoUrl;
        CoverImageUrl = coverImageUrl;
        OpeningTime = openingTime;
        ClosingTime = closingTime;
        MinOrderAmount = minOrderAmount;
        Status = status;
        IsApproved = status == RestaurantStatus.Active;
        IsAcceptingOrders = isAcceptingOrders;
        OwnerId = ownerId;

        Id = Guid.NewGuid();
    }

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string PhoneNumber { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string Address { get; private set; } = string.Empty;
    public double Latitude { get; private set; }
    public double Longtitude { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? CoverImageUrl { get; private set; }
    public TimeOnly OpeningTime { get; private set; }
    public TimeOnly ClosingTime { get; private set; }
    public decimal MinOrderAmount { get; private set; }
    public RestaurantStatus Status { get; private set; } = RestaurantStatus.Pending;
    public bool IsApproved { get; private set; }
    public bool IsAcceptingOrders { get; private set; }
    public Guid OwnerId { get; private set; }

    public void Approve()
    {
        IsApproved = true;
        Status = RestaurantStatus.Active;
        Touch();
    }

    public void Reject()
    {
        IsApproved = false;
        Status = RestaurantStatus.Inactive;
        Touch();
    }

    public void UpdateDetails(
        string name,
        string slug,
        string? description,
        string phoneNumber,
        string email,
        string address,
        double latitude,
        double longtitude,
        string? logoUrl,
        string? coverImageUrl,
        TimeOnly openingTime,
        TimeOnly closingTime,
        decimal minOrderAmount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        Name = name;
        Slug = slug;
        Description = description;
        PhoneNumber = phoneNumber;
        Email = email;
        Address = address;
        Latitude = latitude;
        Longtitude = longtitude;
        LogoUrl = logoUrl;
        CoverImageUrl = coverImageUrl;
        OpeningTime = openingTime;
        ClosingTime = closingTime;
        MinOrderAmount = minOrderAmount;
        Touch();
    }

    public void SetStatus(RestaurantStatus status)
    {
        Status = status;
        IsApproved = status == RestaurantStatus.Active;
        Touch();
    }

    public void SetAcceptingOrders(bool isAccepting)
    {
        IsAcceptingOrders = isAccepting;
        Touch();
    }
}
