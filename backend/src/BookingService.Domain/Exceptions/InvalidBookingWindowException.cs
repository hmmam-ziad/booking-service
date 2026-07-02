using System;
using System.Collections.Generic;
using System.Text;

namespace BookingService.Domain.Exceptions
{
    /// <summary>
    /// Thrown when a booking's start/end time window is invalid
    /// (e.g. end is not after start).
    /// </summary>
    public class InvalidBookingWindowException : Exception
    {
        public InvalidBookingWindowException(string message) : base(message) { }
    }
}
