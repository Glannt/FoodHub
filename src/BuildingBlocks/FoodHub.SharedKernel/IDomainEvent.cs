using FoodHub.SharedKernel;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
