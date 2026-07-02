using BookingService.Domain.Enums;
using BookingService.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace BookingService.Domain.Entities
{
    /// <summary>
    /// A Booking reserves a single Resource for a single User over a UTC time window.
    /// </summary>
    public class Booking
    {
        public string Id { get; private set; } = Guid.NewGuid().ToString();
        public string ResourceId { get; private set; } = default!;
        public string UserId { get; private set; } = default!;
        public DateTime StartDateTime { get; private set; }
        public DateTime EndDateTime { get; private set; }
        public BookingStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? CancelledAt { get; private set; }

        private Booking() { }


        private Booking(string resourceId, string userId, DateTime start, DateTime end)
        {
            ResourceId = resourceId;
            UserId = userId;
            StartDateTime = start;
            EndDateTime = end;
            Status = BookingStatus.Confirmed;
            CreatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Factory method that enforces all domain invariants at creation time.
        /// </summary>
        public static Booking Create(string resourceId, string userId, DateTime start, DateTime end)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
                throw new ArgumentException("ResourceId is required.", nameof(resourceId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required.", nameof(userId));

            if (start.Kind != DateTimeKind.Utc || end.Kind != DateTimeKind.Utc)
                throw new InvalidBookingWindowException("StartDateTime and EndDateTime must be in UTC.");

            if (end <= start)
                throw new InvalidBookingWindowException("EndDateTime must be after StartDateTime.");

            return new Booking(resourceId, userId, start, end);
        }

        /// <summary>
        /// Determines whether this booking's time window overlaps with the given window.
        /// Uses a half-open interval [Start, End): a booking ending exactly when
        /// another begins is NOT considered an overlap.
        /// </summary>
        public bool OverlapsWith(DateTime otherStart, DateTime otherEnd)
        {
            return StartDateTime < otherEnd && otherStart < EndDateTime;
        }

        public void Cancel()
        {
            if (Status == BookingStatus.Cancelled)
                return; // idempotent - cancelling twice is a no-op, not an error

            Status = BookingStatus.Cancelled;
            CancelledAt = DateTime.UtcNow;
        }

        public bool IsActive => Status == BookingStatus.Confirmed;
    }
}
