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

    public BookingsController(
        BookingDbContext db,
        IRoomServiceClient roomClient,
        IHotelServiceClient hotelClient,
        IPaymentServiceClient paymentClient,
        ILogger<BookingsController> logger)
    {
        _db          = db;
        _roomClient  = roomClient;
        _hotelClient = hotelClient;
        _paymentClient = paymentClient;
        _logger = logger; //new
    }

        // GET api/bookings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BookingDetailDto>>> Get()
        {
          // get the current user from the token
          var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
              ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
          if (string.IsNullOrEmpty(userId))
              return Unauthorized();

          // filter by userId
          var own = await _db.Bookings
              .Where(b => b.UserId == userId)
              .ToListAsync();

            var result = new List<BookingDetailDto>();
            foreach (var b in own)
            {
                // Get room details
                var room = await _roomClient.GetRoomByIdAsync(b.RoomId);
                // Get hotel details
                var hotel = await _hotelClient.GetHotelByIdAsync(b.HotelId);

                result.Add(new BookingDetailDto
                {
                    Id       = b.Id,
                    CheckIn  = b.CheckIn,
                    CheckOut = b.CheckOut,
                    Status   = b.Status,
                    Room     = room,
                    Hotel    = hotel
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
                    //b.HotelId == hotelId &&
                    hotelRoomIds.Contains(b.RoomId) &&
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

    

    // [HttpPost]
    // public async Task<ActionResult<Booking>> Create(Booking b)
    // {
    //   var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
    //   b.UserId = userId;

    //   if (b.CheckOut <= b.CheckIn)
    //             return BadRequest("Check-out date must be later than check-in date.");

    //   //Make sure that such a room exists
    //   var room = await _roomClient.GetRoomByIdAsync(b.RoomId);
    //     if (room == null)
    //      return BadRequest("Room not found");

    //   // Check that there are no overlapping reservations for this period
    //   bool isTaken = await _db.Bookings
    //     .AnyAsync(x =>
    //         x.RoomId == b.RoomId &&
    //         x.CheckIn  < b.CheckOut &&
    //         x.CheckOut > b.CheckIn
    //     );
    //   if (isTaken)
    //     // 409 Conflict, if the room is booked
    //     return Conflict("Room is already booked for the selected period");

    //   b.Status = BookingStatus.Pending;

    //   _db.Bookings.Add(b);
    //   await _db.SaveChangesAsync();
    //   return CreatedAtRoute("GetBookingById", new { id = b.Id }, b);
    // }

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
            x.RoomId   == dto.RoomId &&
            x.CheckIn  < dto.CheckOut &&
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

        // fill in a detailed answer
        var hotelDto = await _hotelClient.GetHotelByIdAsync(booking.HotelId);
        var detail = new BookingDetailDto
        {
            Id       = booking.Id,
            CheckIn  = booking.CheckIn,
            CheckOut = booking.CheckOut,
            Status   = booking.Status,
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

        // Trying to get money back
        bool refundOk = true;
        // if (booking.PaymentId.HasValue)
        // {
        //     refundOk = await _paymentClient.RefundAsync(booking.PaymentId.Value);
        // }
        string? errorReason = null;
        try
        {
            if (booking.PaymentId.HasValue)
            {
                refundOk = await _paymentClient.RefundAsync(booking.PaymentId.Value);
                if (!refundOk)
                    errorReason = "Gateway returned failure";
            }
            else
            {
                refundOk = false;
                errorReason = "No payment to refund";
            }
        }
        catch (Exception ex)
        {
            refundOk    = false;
            errorReason = ex.Message;
            _logger.LogError(ex, "Refund failed for booking {BookingId}", booking.Id);
        }

        // Change status and date
        booking.Status     = refundOk ? BookingStatus.Cancelled : BookingStatus.RefundError;
        booking.IsCanceled = true;
        booking.CanceledAt = DateTime.UtcNow;
        booking.RefundErrorReason  = errorReason; //new
        await _db.SaveChangesAsync();
        

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
            Room              = roomDto,
            Hotel             = hotelDto
        };
        return Ok(result);
    }

  }
}


