using System;
using System.Collections.Generic;
using System.Text;

namespace FoodHub.Domain.Persistence;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
