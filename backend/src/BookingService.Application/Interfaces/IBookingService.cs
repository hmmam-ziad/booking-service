using BookingService.Application.Common;
using BookingService.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace BookingService.Application.Interfaces
{
    public interface IBookingService
    {
        Task<BookingResponse> CreateAsync(CreateBookingRequest request, CancellationToken ct = default);

        Task<PagedResult<BookingResponse>> GetByResourceAsync(GetBookingsQuery query, CancellationToken ct = default);

        Task CancelAsync(string id, CancellationToken ct = default);
    }
}
