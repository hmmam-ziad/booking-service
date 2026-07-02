using System;
using System.Collections.Generic;
using System.Text;

namespace BookingService.Domain.Exceptions
{
    /// <summary>
    /// Thrown when a booking would overlap with an existing confirmed booking
    /// for the same resource.
    /// </summary>
    public class BookingOverlapException : Exception
    {
        public string ResourceId { get; }

        public BookingOverlapException(string resourceId)
            : base($"$\"The resource '{resourceId}' is already booked for the requested time window.")
        {
            ResourceId = resourceId;
        }
    }
}
