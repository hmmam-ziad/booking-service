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

        public BookingService(IBookingRepository repository)
        {
            _repository = repository;
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
            var booking = Booking.Create(
                request.ResourceId,
                request.UserId,
                request.StartDateTime,
                request.EndDateTime);

            var overlaps = await _repository.HasOverlapAsync(
                booking.ResourceId,
                booking.StartDateTime,
                booking.EndDateTime,
                ct);

            if (overlaps)
                throw new BookingOverlapException(booking.ResourceId);
            await _repository.AddAsync(booking, ct);
            await _repository.SaveChangesAsync(ct);

            return ToResponse(booking);
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
        b.Status, b.CreatedAt, b.CancelledAt);
    }
}
