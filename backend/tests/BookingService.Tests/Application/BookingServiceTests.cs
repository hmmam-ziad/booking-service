using BookingService.Application.DTOs;
using BookingService.Application.Interfaces;
using BookingService.Domain.Entities;
using BookingService.Domain.Exceptions;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using ApplicationBookingService = BookingService.Application.Services.BookingService;

namespace BookingService.Tests.Application
{
    public class BookingServiceTests
    {
        private static readonly DateTime Start = new(2026, 7, 10, 10, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime End = new(2026, 7, 10, 11, 0, 0, DateTimeKind.Utc);

        private readonly Mock<IBookingRepository> _repositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly ApplicationBookingService _sut; // "system under test"

        public BookingServiceTests()
        {
            _repositoryMock = new Mock<IBookingRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _sut = new ApplicationBookingService(_repositoryMock.Object, _unitOfWorkMock.Object);
        }

        [Fact]
        public async Task CreateAsync_NoOverlap_CreatesAndReturnsBooking()
        {
            _repositoryMock
                .Setup(r => r.HasOverlapAsync("room-101", Start, End, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var request = new CreateBookingRequest("room-101", "user-1", Start, End);

            var result = await _sut.CreateAsync(request);

            Assert.Equal("room-101", result.ResourceId);
            Assert.Equal("user-1", result.UserId);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_Overlap_ThrowsBookingOverlapException_AndDoesNotSave()
        {
            _repositoryMock
                .Setup(r => r.HasOverlapAsync("room-101", Start, End, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var request = new CreateBookingRequest("room-101", "user-1", Start, End);

            await Assert.ThrowsAsync<BookingOverlapException>(() => _sut.CreateAsync(request));

            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_InvalidWindow_ThrowsBeforeCallingRepository()
        {
            // End before Start — Domain should reject this before we ever touch the repository
            var request = new CreateBookingRequest("room-101", "user-1", End, Start);

            await Assert.ThrowsAsync<InvalidBookingWindowException>(() => _sut.CreateAsync(request));

            _repositoryMock.Verify(
                r => r.HasOverlapAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingBooking_ReturnsResponse()
        {
            var booking = Booking.Create("room-101", "user-1", Start, End);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(booking);

            var result = await _sut.GetByIdAsync(booking.Id);

            Assert.Equal(booking.Id, result.Id);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistentBooking_ThrowsKeyNotFoundException()
        {
            _repositoryMock
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>().ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Booking?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.GetByIdAsync(Guid.NewGuid().ToString()));
        }

        [Fact]
        public async Task CancelAsync_ExistingBooking_CancelsAndSaves()
        {
            var booking = Booking.Create("room-101", "user-1", Start, End);

            _repositoryMock
                .Setup(r => r.GetByIdAsync(booking.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(booking);

            await _sut.CancelAsync(booking.Id);

            Assert.False(booking.IsActive);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CancelAsync_NonExistentBooking_ThrowsKeyNotFoundException()
        {
            _repositoryMock
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>().ToString(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Booking?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.CancelAsync(Guid.NewGuid().ToString()));

            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetByResourceAsync_ReturnsPagedResult_WithCorrectMapping()
        {
            var booking = Booking.Create("room-101", "user-1", Start, End);

            _repositoryMock
                .Setup(r => r.GetByResourceAsync("room-101", null, null, 1, 20, It.IsAny<CancellationToken>()))
                .ReturnsAsync((new List<Booking> { booking }, 1));

            var query = new GetBookingsQuery { ResourceId = "room-101", Page = 1, PageSize = 20 };

            var result = await _sut.GetByResourceAsync(query);

            Assert.Single(result.Items);
            Assert.Equal(1, result.TotalCount);
            Assert.Equal(booking.Id, result.Items[0].Id);
        }
    }
}
