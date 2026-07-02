using System;
using System.Collections.Generic;
using System.Text;

namespace BookingService.Application.Interfaces
{
    /// <summary>
    /// Provides a simple way to manage database transactions,
    /// ensuring that multiple operations are completed as a single unit.
    /// </summary>
    public interface IUnitOfWork
    {
        Task BeginTransactionAsync(CancellationToken ct = default);
        Task CommitTransactionAsync(CancellationToken ct = default);
        Task RollbackTransactionAsync(CancellationToken ct = default);
    }
}
