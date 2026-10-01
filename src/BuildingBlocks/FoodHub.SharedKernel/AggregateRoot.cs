using System;
using System.Collections.Generic;
using System.Text;

namespace FoodHub.SharedKernel;

public abstract class AggregateRoot<TId> : Entity<TId> where TId : IEquatable<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
