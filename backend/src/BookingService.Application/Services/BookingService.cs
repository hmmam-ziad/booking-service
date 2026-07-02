using BookingService.Application.Common;
using BookingService.Application.DTOs;
using BookingService.Application.Interfaces;
using BookingService.Domain.Entities;
using BookingService.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace BookingService.Application.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public BookingService(IBookingRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task CancelAsync(string id, CancellationToken ct = default)
        {
            var booking = await _repository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Booking '{id}' was not found.");

            booking.Cancel();
            await _repository.SaveChangesAsync(ct);
        }

        public async Task<BookingResponse> CreateAsync(CreateBookingRequest request, CancellationToken ct = default)
        {
            // Validate the request before starting a database transaction.
            var booking = Booking.Create(
                request.ResourceId,
                request.UserId,
                request.StartDateTime,
                request.EndDateTime);

            await _unitOfWork.BeginTransactionAsync(ct);

            try
            {
                // Prevents concurrent create operations for the same resource.
                // Operations on different resources can still run in parallel.
                await _repository.AcquireResourceLockAsync(booking.ResourceId, ct);

                var overlaps = await _repository.HasOverlapAsync(
                    booking.ResourceId, booking.StartDateTime, booking.EndDateTime, ct);

                if (overlaps)
                    throw new BookingOverlapException(booking.ResourceId);

                await _repository.AddAsync(booking, ct);
                await _repository.SaveChangesAsync(ct);

                await _unitOfWork.CommitTransactionAsync(ct);
                return ToResponse(booking);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                throw;
            }
        }

        public async Task<PagedResult<BookingResponse>> GetByResourceAsync(GetBookingsQuery query, CancellationToken ct = default)
        {
            var (items, totalCount) = await _repository.GetByResourceAsync(query.ResourceId, query.From, query.To, query.Page, query.PageSize, ct);
            return new PagedResult<BookingResponse>
            {
                Items = items.Select(ToResponse).ToList(),
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };
        }

        private static BookingResponse ToResponse(Booking b) => new(
        b.Id, b.ResourceId, b.UserId, b.StartDateTime, b.EndDateTime,
        b.Status.ToString(), b.CreatedAt, b.CancelledAt);

        public async Task<BookingResponse> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var booking = await _repository.GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException($"Booking '{id}' was not found.");

            return ToResponse(booking);
        }
    }
}
