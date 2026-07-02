using System;
using System.Collections.Generic;
using System.Text;

namespace BookingService.Application.DTOs
{
    public class GetBookingsQuery
    {
        public string ResourceId { get; init; } = default!;
        public DateTime? From { get; init; }
        public DateTime? To { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;
    }
}
