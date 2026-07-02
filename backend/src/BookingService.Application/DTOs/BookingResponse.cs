using BookingService.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace BookingService.Application.DTOs
{
    public record BookingResponse(
        string Id,
        string ResourceId,
        string UserId,
        DateTime StartDateTime,
        DateTime EndDateTime,
        BookingStatus Status,
        DateTime CreatedAt,
        DateTime? CancelledAt
    );
}
