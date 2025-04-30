using CatalogService.API.Infrastructure.Data;
using CatalogService.API.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CatalogService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HotelsController : ControllerBase
    {
        private readonly CatalogDbContext _context;

        public HotelsController(CatalogDbContext context)
        {
            _context = context;
        }


        /// GET api/hotels
        /// Support:
        /// - search (fitter by Name или City)
        /// - minStars
        /// - maxDistance (Maximum distance to center)
        /// - page, pageSize (pagination)
        [HttpGet("", Name = "GetHotels")]
        public async Task<ActionResult<IEnumerable<Hotel>>> Get(
            [FromQuery] string? search       = null,
            [FromQuery] int?    minStars     = null,
            [FromQuery] double? maxDistance  = null,
            [FromQuery] int     page         = 1,
            [FromQuery] int     pageSize     = 20)
        {
            // Parameter validation
            if (minStars is < 1 or > 5)
                return BadRequest("minStars should be in the range from 1 to 5.");
            if (page < 1 || pageSize < 1)
                return BadRequest("page and pageSize must be positive numbers.");

            // baseline IQueryable
            var query = _context.Hotels.AsQueryable();

            // filtration
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(h =>
                    EF.Functions.Like(h.Name, $"%{search}%") ||
                    EF.Functions.Like(h.City, $"%{search}%"));
            }
            if (minStars.HasValue)
            {
                query = query.Where(h => h.Stars >= minStars.Value);
            }
            if (maxDistance.HasValue)
            {
                query = query.Where(h => h.DistanceFromCenter <= maxDistance.Value);
            }

            // total number (for front-line pagination)
            var totalCount = await query.CountAsync();
            Response.Headers.Add("X-Total-Count", totalCount.ToString());

            // apply sorting, paining and execute the request
            var hotels = await query
                .OrderBy(h => h.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(hotels);
        }


        // GET: api/Hotels/5
        // [HttpGet("{id}")]
        // public async Task<ActionResult<Hotel>> GetHotel(int id)
        // {
        //     var hotel = await _context.Hotels.FindAsync(id);
        //     if (hotel == null) return NotFound();
        //     return hotel;
        // }


        /// GET api/hotels/{id}
        [HttpGet("{id:int}", Name = "GetHotelById")]
        public async Task<ActionResult<Hotel>> GetById(int id)
        {
            var hotel = await _context.Hotels.FindAsync(id);
            if (hotel == null)
                return NotFound();
            return Ok(hotel);
        }


        // POST: api/Hotels
        // [HttpPost]
        // public async Task<ActionResult<Hotel>> CreateHotel(Hotel hotel)
        // {
        //     _context.Hotels.Add(hotel);
        //     await _context.SaveChangesAsync();
        //     return CreatedAtAction(nameof(GetHotel), new { id = hotel.Id }, hotel);
        // }


        /// POST api/hotels
        [HttpPost]
        public async Task<ActionResult<Hotel>> Create([FromBody] Hotel hotel)
        {
            _context.Hotels.Add(hotel);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = hotel.Id }, hotel);
        }


        // PUT: api/Hotels/5
        // [HttpPut("{id}")]
        // public async Task<IActionResult> UpdateHotel(int id, Hotel hotel)
        // {
        //     if (id != hotel.Id) return BadRequest();
        //     _context.Entry(hotel).State = EntityState.Modified;
        //     await _context.SaveChangesAsync();
        //     return NoContent();
        // }

        /// PUT api/hotels/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Hotel hotel)
        {
            if (id != hotel.Id)
                return BadRequest("ID в URL и в теле должны совпадать.");

            _context.Entry(hotel).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Hotels/5
        // [HttpDelete("{id}")]
        // public async Task<IActionResult> DeleteHotel(int id)
        // {
        //     var hotel = await _context.Hotels.FindAsync(id);
        //     if (hotel == null) return NotFound();
        //     _context.Hotels.Remove(hotel);
        //     await _context.SaveChangesAsync();
        //     return NoContent();
        // }

        /// DELETE api/hotels/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var hotel = await _context.Hotels.FindAsync(id);
            if (hotel == null)
                return NotFound();

            _context.Hotels.Remove(hotel);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
