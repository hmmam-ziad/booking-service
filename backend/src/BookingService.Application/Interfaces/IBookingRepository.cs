using BookingService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace BookingService.Application.Interfaces
{
    public interface IBookingRepository
    {
        Task<Booking?> GetByIdAsync(string id, CancellationToken ct = default);

        // Returns all ACTIVE (non-cancelled) bookings for a resource that overlap with the given window. Used for the overlap check.
        Task<bool> HasOverlapAsync(string resourceId, DateTime start, DateTime end, CancellationToken ct = default);
        Task<(IReadOnlyList<Booking> Items, int TotalCount)> GetByResourceAsync(string resourceId, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default);
        Task AddAsync(Booking booking, CancellationToken ct = default);
        Task SaveChangesAsync(CancellationToken ct = default);
        /// <summary>
        /// Acquires an exclusive lock for the specified resource.
        /// This method must be called within an active transaction.
        /// The lock is released when the transaction is committed or rolled back.
        /// </summary>
        Task AcquireResourceLockAsync(string resourceId, CancellationToken ct = default);
    }
}
