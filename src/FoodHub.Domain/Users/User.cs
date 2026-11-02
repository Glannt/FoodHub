using FoodHub.SharedKernel;

namespace FoodHub.Domain.Users;

public sealed class User : AggregateRoot<Guid>
{
    private User()
    {
    }

    public User(string fullName, string email, string phoneNumber, bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        FullName = fullName;
        Email = email;
        PhoneNumber = phoneNumber;
        IsActive = isActive;

        Id = Guid.NewGuid();
    }

    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public void Update(string fullName, string email, string phoneNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        FullName = fullName;
        Email = email;
        PhoneNumber = phoneNumber;
        Touch();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        Touch();
    }
}