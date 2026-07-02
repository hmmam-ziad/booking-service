using BookingService.Domain.Entities;
using BookingService.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace BookingService.Tests.Domain
{
    public class BookingOverlapTests
    {
        private static readonly DateTime BaseStart = new(2026, 7, 10, 10, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime BaseEnd = new(2026, 7, 10, 11, 0, 0, DateTimeKind.Utc);

        private static Booking CreateBaseBooking() => Booking.Create("room-101", "user-1", BaseStart, BaseEnd);

        [Fact]
        public void OverlapsWith_IdenticalWindow_ReturnsTrue()
        {
            var booking = CreateBaseBooking();

            var result = booking.OverlapsWith(BaseStart, BaseEnd);

            Assert.True(result);
        }

        [Fact]
        public void OverlapsWith_PartialOverlapAtStart_ReturnsTrue()
        {
            var booking = CreateBaseBooking(); // 10:00 - 11:00

            // 09:30 - 10:30 overlaps the first half hour
            var result = booking.OverlapsWith(
                BaseStart.AddMinutes(-30),
                BaseStart.AddMinutes(30));

            Assert.True(result);
        }

        [Fact]
        public void OverlapsWith_PartialOverlapAtEnd_ReturnsTrue()
        {
            var booking = CreateBaseBooking(); // 10:00 - 11:00

            // 10:30 - 11:30 overlaps the last half hour
            var result = booking.OverlapsWith(
                BaseEnd.AddMinutes(-30),
                BaseEnd.AddMinutes(30));

            Assert.True(result);
        }

        [Fact]
        public void OverlapsWith_OtherWindowFullyInsideBooking_ReturnsTrue()
        {
            var booking = CreateBaseBooking(); // 10:00 - 11:00

            // 10:15 - 10:45 is fully contained
            var result = booking.OverlapsWith(
                BaseStart.AddMinutes(15),
                BaseStart.AddMinutes(45));

            Assert.True(result);
        }

        [Fact]
        public void OverlapsWith_BookingFullyInsideOtherWindow_ReturnsTrue()
        {
            var booking = CreateBaseBooking(); // 10:00 - 11:00

            // 09:00 - 12:00 fully contains the booking
            var result = booking.OverlapsWith(
                BaseStart.AddHours(-1),
                BaseEnd.AddHours(1));

            Assert.True(result);
        }

        [Fact]
        public void OverlapsWith_OtherEndsExactlyWhenBookingStarts_ReturnsFalse()
        {
            var booking = CreateBaseBooking(); // 10:00 - 11:00

            // 09:00 - 10:00 → ends exactly when booking starts
            var result = booking.OverlapsWith(
                BaseStart.AddHours(-1),
                BaseStart);

            Assert.False(result); // half-open interval: touching edges do NOT overlap
        }

        [Fact]
        public void OverlapsWith_OtherStartsExactlyWhenBookingEnds_ReturnsFalse()
        {
            var booking = CreateBaseBooking(); // 10:00 - 11:00

            // 11:00 - 12:00 → starts exactly when booking ends
            var result = booking.OverlapsWith(
                BaseEnd,
                BaseEnd.AddHours(1));

            Assert.False(result); // half-open interval: touching edges do NOT overlap
        }

        [Fact]
        public void OverlapsWith_CompletelyBefore_ReturnsFalse()
        {
            var booking = CreateBaseBooking(); // 10:00 - 11:00

            // 08:00 - 09:00 → well before
            var result = booking.OverlapsWith(
                BaseStart.AddHours(-2),
                BaseStart.AddHours(-1));

            Assert.False(result);
        }

        [Fact]
        public void OverlapsWith_CompletelyAfter_ReturnsFalse()
        {
            var booking = CreateBaseBooking(); // 10:00 - 11:00

            // 12:00 - 13:00 → well after
            var result = booking.OverlapsWith(
                BaseEnd.AddHours(1),
                BaseEnd.AddHours(2));

            Assert.False(result);
        }

        [Theory]
        [InlineData("", "user-1")]
        [InlineData(" ", "user-1")]
        [InlineData("room-101", "")]
        [InlineData("room-101", " ")]
        public void Create_MissingRequiredFields_ThrowsArgumentException(string resourceId, string userId)
        {
            Assert.Throws<ArgumentException>(() =>
                Booking.Create(resourceId, userId, BaseStart, BaseEnd));
        }

        [Fact]
        public void Create_EndBeforeStart_ThrowsInvalidBookingWindowException()
        {
            Assert.Throws<InvalidBookingWindowException>(() =>
                Booking.Create("room-101", "user-1", BaseEnd, BaseStart));
        }

        [Fact]
        public void Create_EndEqualsStart_ThrowsInvalidBookingWindowException()
        {
            Assert.Throws<InvalidBookingWindowException>(() =>
                Booking.Create("room-101", "user-1", BaseStart, BaseStart));
        }

        [Fact]
        public void Create_NonUtcDateTime_ThrowsInvalidBookingWindowException()
        {
            var localStart = DateTime.SpecifyKind(BaseStart, DateTimeKind.Local);

            Assert.Throws<InvalidBookingWindowException>(() =>
                Booking.Create("room-101", "user-1", localStart, BaseEnd));
        }

        [Fact]
        public void Cancel_ConfirmedBooking_SetsStatusToCancelled()
        {
            var booking = CreateBaseBooking();

            booking.Cancel();

            Assert.False(booking.IsActive);
            Assert.NotNull(booking.CancelledAt);
        }

        [Fact]
        public void Cancel_AlreadyCancelledBooking_IsIdempotent()
        {
            var booking = CreateBaseBooking();
            booking.Cancel();
            var firstCancelledAt = booking.CancelledAt;

            booking.Cancel(); // cancel again — should not throw or change CancelledAt

            Assert.Equal(firstCancelledAt, booking.CancelledAt);
        }
    }
}
