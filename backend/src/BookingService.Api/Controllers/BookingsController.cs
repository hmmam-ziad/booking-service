using BookingService.Application.DTOs;
using BookingService.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookingService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }


        /// <summary>
        /// Gets a single booking by id.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BookingResponse>> GetById(string id, CancellationToken ct)
        {
            // Reuses GetByResourceAsync's underlying repository via the service
            // would require a dedicated method; simplest is to expose GetById here.
            var result = await _bookingService.GetByIdAsync(id, ct);
            return Ok(result);
        }

        /// <summary>
        /// Creates a new booking.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<BookingResponse>> Create([FromBody] CreateBookingRequest request, CancellationToken ct)
        {
            var result = await _bookingService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>
        /// Gets bookings for a resource, optionally filtered by date range, with paging.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(Application.Common.PagedResult<BookingResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetByResource([FromQuery] string resourceId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        {
            var query = new GetBookingsQuery
            {
                ResourceId = resourceId,
                From = from,
                To = to,
                Page = page,
                PageSize = pageSize
            };

            var result = await _bookingService.GetByResourceAsync(query, ct);
            return Ok(result);
        }

        /// <summary>
        /// Cancels a booking (soft delete).
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancel(string id, CancellationToken ct)
        {
            await _bookingService.CancelAsync(id, ct);
            return NoContent();
        }
    }
}
