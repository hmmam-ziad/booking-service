using BookingService.Application.Interfaces;
using BookingService.Domain.Entities;
using BookingService.Domain.Enums;
using BookingService.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace BookingService.Infrastructure.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly BookingDbContext _context;

        public BookingRepository(BookingDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Booking booking, CancellationToken ct = default)
        {
            await _context.Bookings.AddAsync(booking, ct);
        }

        public async Task<Booking?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            return await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == id, ct);
        }

        public async Task<(IReadOnlyList<Booking> Items, int TotalCount)> GetByResourceAsync(string resourceId, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _context.Bookings
            .Where(b => b.ResourceId == resourceId)
            .AsQueryable();

            if (from.HasValue)
                query = query.Where(b => b.EndDateTime > from.Value);

            if (to.HasValue)
                query = query.Where(b => b.StartDateTime < to.Value);

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderBy(b => b.StartDateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task<bool> HasOverlapAsync(string resourceId, DateTime start, DateTime end, CancellationToken ct = default)
        {
            // Half-open interval overlap check, mirrors Booking.OverlapsWith().
            // Only active (non-cancelled) bookings block new ones.
            return await _context.Bookings
                .Where(b => b.ResourceId == resourceId && b.Status == BookingStatus.Confirmed)
                .AnyAsync(b => b.StartDateTime < end && start < b.EndDateTime, ct);
        }

        public Task SaveChangesAsync(CancellationToken ct = default)
        {
            return _context.SaveChangesAsync(ct);
        }



        public async Task AcquireResourceLockAsync(string resourceId, CancellationToken ct = default)
        {
            var connection = _context.Database.GetDbConnection();

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(ct);

            await using var command = connection.CreateCommand();
            command.CommandText = "sp_getapplock";
            command.CommandType = CommandType.StoredProcedure;
            command.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();
            command.Parameters.Add(new SqlParameter("@Resource", $"booking-resource:{resourceId}"));
            command.Parameters.Add(new SqlParameter("@LockMode", "Exclusive"));
            command.Parameters.Add(new SqlParameter("@LockOwner", "Transaction")); // auto-released on commit/rollback
            command.Parameters.Add(new SqlParameter("@LockTimeout", 10000)); // 10s max wait

            var returnValue = new SqlParameter
            {
                ParameterName = "@ReturnValue",
                SqlDbType = SqlDbType.Int,
                Direction = ParameterDirection.ReturnValue
            };
            command.Parameters.Add(returnValue);

            await command.ExecuteNonQueryAsync(ct);

            var result = (int)returnValue.Value;
            if (result < 0)
            {
                throw new InvalidOperationException(
                    $"Could not acquire lock for resource '{resourceId}' (sp_getapplock returned {result}).");
            }
        }
    }
}
