using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomService.API.Domain.Models;
using RoomService.API.Infrastructure.Data;
using RoomService.API.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace RoomService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoomsController : ControllerBase
    {
        private readonly RoomDbContext _context;

        public RoomsController(RoomDbContext context)
        {
            _context = context;
        }
        
        // GET: api/rooms
        [HttpGet("", Name = "GetRooms")]
        public async Task<ActionResult<IEnumerable<RoomDto>>> GetRooms(
            [FromQuery] int?    hotelId   = null,
            [FromQuery] decimal? minPrice  = null,
            [FromQuery] decimal? maxPrice  = null,
            [FromQuery] RoomType? type     = null,
            [FromQuery] int      page      = 1,
            [FromQuery] int      pageSize  = 20)
        {
            if (page < 1 || pageSize < 1)
                return BadRequest("page and pageSize should be >= 1");

            var q = _context.Rooms.AsQueryable();

            if (hotelId.HasValue)
                q = q.Where(r => r.HotelId == hotelId.Value);
            if (minPrice.HasValue)
                q = q.Where(r => r.Price >= minPrice.Value);
            if (maxPrice.HasValue)
                q = q.Where(r => r.Price <= maxPrice.Value);
            if (type.HasValue)
                q = q.Where(r => r.Type == type.Value);

            var total = await q.CountAsync();
            Response.Headers["X-Total-Count"] = total.ToString();

            var rooms = await q
                .OrderBy(r => r.Price)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new RoomDto {
                    Id            = r.Id,
                    HotelId       = r.HotelId,
                    Number        = r.Number,
                    Type          = r.Type,
                    Price         = r.Price,
                    Description   = r.Description,
                    IsAvailable   = r.IsAvailable,
                    RoomImageUrl  = r.RoomImageUrl,
                    NumberOfBeds  = r.NumberOfBeds,
                    Capacity      = r.Capacity
                })
                .ToListAsync();

            return Ok(rooms);
        }

        // GET: api/rooms/id
        [HttpGet("{id}", Name = "GetRoomById")]
        public async Task<ActionResult<Room>> GetById(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            return room == null ? NotFound() : Ok(room);
        }

        // POST: api/rooms
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Room>> Create(Room room)
        {
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();
            return CreatedAtAction("GetById", new { id = room.Id }, room);
        }

        // PUT: api/rooms/id
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, Room room)
        {
            if (id != room.Id) return BadRequest();
            _context.Entry(room).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/rooms/id
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();
            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
