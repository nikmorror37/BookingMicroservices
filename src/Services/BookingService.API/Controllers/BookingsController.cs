using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using BookingService.API.Infrastructure.Data;
using BookingService.API.Infrastructure.Clients;
using BookingService.API.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using BookingMicro.Contracts.Events;
using MassTransit;
using System.Net.Http.Headers;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace BookingService.API.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  [Authorize]
  public class BookingsController : ControllerBase
  {
    private readonly BookingDbContext _db;
    private readonly IRoomServiceClient _roomClient;
    private readonly IHotelServiceClient _hotelClient;
    private readonly IPaymentServiceClient _paymentClient;
    private readonly ILogger<BookingsController> _logger;
    private readonly IPublishEndpoint      _publishEndpoint;
    private readonly IMessageScheduler     _scheduler;

    public BookingsController(
        BookingDbContext db,
        IRoomServiceClient roomClient,
        IHotelServiceClient hotelClient,
        IPaymentServiceClient paymentClient,
        ILogger<BookingsController> logger,
        IPublishEndpoint publishEndpoint,
        IMessageScheduler scheduler)
    {
        _db          = db;
        _roomClient  = roomClient;
        _hotelClient = hotelClient;
        _paymentClient = paymentClient;
        _logger = logger; //new
        _publishEndpoint = publishEndpoint;
        _scheduler       = scheduler;
    }

    // GET api/bookings
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookingDetailDto>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        var isAdmin = User.IsInRole("Admin");

        if (page < 1 || pageSize < 1)
            return BadRequest("page and pageSize must be positive numbers.");

        var q = _db.Bookings.AsQueryable();
        if (!isAdmin)
            q = q.Where(b => b.UserId == userId);

        var totalCount = await q.CountAsync();

        if (!Response.Headers.ContainsKey("X-Total-Count"))
            Response.Headers.Append("X-Total-Count", totalCount.ToString());

        var bookings = await q
            .OrderByDescending(b => b.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new List<BookingDetailDto>();
        foreach (var b in bookings)
        {
            var roomDto  = await _roomClient.GetRoomByIdAsync(b.RoomId);
            var hotelDto = await _hotelClient.GetHotelByIdAsync(b.HotelId);

            result.Add(new BookingDetailDto
            {
                Id       = b.Id,
                CheckIn  = b.CheckIn,
                CheckOut = b.CheckOut,
                Status   = b.Status,
                CanceledAt        = b.CanceledAt,
                RefundErrorReason = b.RefundErrorReason,
                PaymentId         = b.PaymentId,
                Room              = roomDto,
                Hotel             = hotelDto
            });
        }

        return Ok(result);
    }

    [HttpGet("{id}", Name = "GetBookingById")]
    public async Task<ActionResult<BookingDetailDto>> Get(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
          ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        var b = await _db.Bookings.FindAsync(id);
        if (b == null || b.UserId != userId) 
          return NotFound();

        var room  = await _roomClient.GetRoomByIdAsync(b.RoomId);
        var hotel = await _hotelClient.GetHotelByIdAsync(b.HotelId);

        var dto = new BookingDetailDto
        {
            Id       = b.Id,
            CheckIn  = b.CheckIn,
            CheckOut = b.CheckOut,
            Status   = b.Status,
            CanceledAt = b.CanceledAt,
            RefundErrorReason = b.RefundErrorReason,
            PaymentId = b.PaymentId,
            Room     = room,
            Hotel    = hotel
        };

        return Ok(dto);
    }

    // GET api/bookings/available?hotelId=2&checkIn=2025-04-29&checkOut=2025-04-30
    [HttpGet("available", Name = "GetAvailableRooms")]
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetAvailable(
        [FromQuery] int hotelId,
        [FromQuery] DateTime checkIn,
        [FromQuery] DateTime checkOut)
    {
          var ci = checkIn.Date;
          var co = checkOut.Date;
          if (co <= ci)
                return BadRequest("Check-out date must be later than check-in date.");

          // RoomService all DTO rooms of the desired hotel
          var allRooms = await _roomClient.GetRoomsByHotelAsync(hotelId);

          // Build a HashSet of all the ID rooms in this hotel
          var hotelRoomIds = new HashSet<int>(allRooms.Select(r => r.Id));

          //Collect ID of rooms already booked in this range
          var overlappingRoomIds = await _db.Bookings
                .Where(b =>
                    hotelRoomIds.Contains(b.RoomId) &&
                    // считаем занятыми только активные брони (Pending/Confirmed)
                    (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed) &&
                    b.CheckIn.Date < co &&
                    b.CheckOut.Date > ci
                )
          .Select(b => b.RoomId)
          .Distinct()
          .ToListAsync();

          //Select only available, display the flag IsAvailable
          var available = allRooms
                .Where(r => !overlappingRoomIds.Contains(r.Id))
                .Select(r => { r.IsAvailable = true; return r; })
                .ToList();

          return Ok(available);
    }

    [HttpPost]
    public async Task<ActionResult<BookingDetailDto>> Create([FromBody] BookingCreateDto dto)
    {
        // take from JWT claim "sub"
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (dto.CheckOut <= dto.CheckIn)
            return BadRequest("Check-out date must be later than check-in date.");

        // check that the room exists
        var roomDto = await _roomClient.GetRoomByIdAsync(dto.RoomId);
        if (roomDto is null)
            return BadRequest("Room not found.");

        // have already booked for these dates?
        bool isTaken = await _db.Bookings.AnyAsync(x =>
            x.RoomId == dto.RoomId &&
            (x.Status == BookingStatus.Pending || x.Status == BookingStatus.Confirmed) &&
            x.CheckIn < dto.CheckOut &&
            x.CheckOut > dto.CheckIn
        );
        if (isTaken)
            return Conflict("Room is already booked for the selected period.");

        // create and save
        var booking = new Booking
        {
            HotelId  = dto.HotelId,
            RoomId   = dto.RoomId,
            CheckIn  = dto.CheckIn,
            CheckOut = dto.CheckOut,
            UserId   = userId,
            Status   = BookingStatus.Pending
        };
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        // NEW: publish domain events -------------------------------------------------
        await _publishEndpoint.Publish(new BookingCreated
        {
            BookingId = booking.Id,
            HotelId   = booking.HotelId,
            RoomId    = booking.RoomId,
            UserId    = booking.UserId,
            CheckIn   = booking.CheckIn,
            CheckOut  = booking.CheckOut
        });

        await _publishEndpoint.Publish(new RoomReserveRequested
        {
            BookingId = booking.Id,
            HotelId   = booking.HotelId,
            RoomId    = booking.RoomId,
            CheckIn   = booking.CheckIn,
            CheckOut  = booking.CheckOut
        });

        // schedule auto-cancellation after 10 minutes if the payment has not been completed
        await _scheduler.SchedulePublish<CancelBookingTimeout>(
            DateTime.UtcNow.AddMinutes(10),
            new CancelBookingTimeout {
                BookingId = booking.Id,
                CreatedAt = DateTime.UtcNow
            });
        // ---------------------------------------------------------------------------

        // fill in a detailed answer
        var hotelDto = await _hotelClient.GetHotelByIdAsync(booking.HotelId);
        var detail = new BookingDetailDto
        {
            Id       = booking.Id,
            CheckIn  = booking.CheckIn,
            CheckOut = booking.CheckOut,
            Status   = booking.Status,
            CanceledAt = booking.CanceledAt,
            RefundErrorReason = booking.RefundErrorReason,
            PaymentId = booking.PaymentId,
            Room     = roomDto,
            Hotel    = hotelDto
        };

        return CreatedAtRoute("GetBookingById", new { id = detail.Id }, detail);
    }

    // PUT /api/bookings/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] BookingUpdateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // extract your user as in your other methods
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        var existing = await _db.Bookings.FindAsync(id);
        if (existing == null || existing.UserId != userId)
            return NotFound();

        if (dto.CheckOut <= dto.CheckIn)
            return BadRequest("Check-out date must be later than check-in date.");

        // only update the fields you allow
        existing.CheckIn  = dto.CheckIn;
        existing.CheckOut = dto.CheckOut;
        // if let users change status:
        // if (dto.Status.HasValue) existing.Status = dto.Status.Value;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/bookings/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        var booking = await _db.Bookings.FindAsync(id);
        if (booking == null || booking.UserId != userId)
            return NotFound();

        _db.Bookings.Remove(booking);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST api/bookings/{id}/cancel
    [HttpPost("{id}/cancel")]
    //[HttpPatch("{id}/cancel")]
    public async Task<ActionResult<BookingDetailDto>> Cancel(int id)
    {
        // Verify the user (same code as in Create/Get)
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Load the booking
        var booking = await _db.Bookings.FindAsync(id);
        if (booking == null || booking.UserId != userId)
            return NotFound();

        // Status must be Pending or Confirmed
        if (booking.Status is not (BookingStatus.Pending or BookingStatus.Confirmed))
            return BadRequest("Booking cannot be cancelled in its current status.");

        // Mark booking as cancelled and publish event
        booking.Status     = BookingStatus.Cancelled;
        booking.IsCanceled = true;
        booking.CanceledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _publishEndpoint.Publish(new BookingCancelled
        {
            BookingId  = booking.Id,
            CanceledAt = booking.CanceledAt!.Value,
            RoomId     = booking.RoomId,
            HasRefund  = booking.PaymentId.HasValue
        });

        // Return detail-DTO
        var roomDto  = await _roomClient.GetRoomByIdAsync(booking.RoomId);
        var hotelDto = await _hotelClient.GetHotelByIdAsync(booking.HotelId);
        var result = new BookingDetailDto
        {
            Id                = booking.Id,
            CheckIn           = booking.CheckIn,
            CheckOut          = booking.CheckOut,
            Status            = booking.Status,
            CanceledAt        = booking.CanceledAt,
            RefundErrorReason = booking.RefundErrorReason,
            PaymentId         = booking.PaymentId,
            Room              = roomDto,
            Hotel             = hotelDto
        };
        return Ok(result);
    }

  }
}


