using System;
using System.Collections.Generic;
using System.Text;

namespace BookingService.Application.DTOs
{
    public record CreateBookingRequest(
        string ResourceId,
        string UserId,
        DateTime StartDateTime,
        DateTime EndDateTime
    );
}
