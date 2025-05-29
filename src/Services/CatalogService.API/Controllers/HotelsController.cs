using CatalogService.API.Infrastructure.Data;
using CatalogService.API.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

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
        /// - search (filter by Name or City)
        /// - minStars
        /// - maxDistance (Maximum distance to center)
        /// - page, pageSize (pagination)
        [HttpGet("", Name = "GetHotels")]
        public async Task<ActionResult<IEnumerable<Hotel>>> Get(
            [FromQuery] string? search = null,
            [FromQuery] int? minStars = null,
            [FromQuery] string? maxDistance = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            // Parameter validation
            if (minStars is < 1 or > 5)
                return BadRequest("minStars should be in the range from 1 to 5.");
            if (page < 1 || pageSize < 1)
                return BadRequest("page and pageSize must be positive numbers.");

            // parse maxDistance considering comma or dot
            double? maxDist = null;
            if (!string.IsNullOrWhiteSpace(maxDistance))
            {
                var txt = maxDistance!.Replace(',', '.');
                if (!double.TryParse(txt, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                    return BadRequest("maxDistance is not a valid number.");
                if (parsed < 0)
                    return BadRequest("maxDistance must be non-negative.");
                maxDist = parsed;
            }

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
            if (maxDist.HasValue)
            {
                query = query.Where(h => h.DistanceFromCenter <= maxDist.Value);
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] Hotel hotel)
        {
            if (id != hotel.Id)
                return BadRequest("ID in the URL and in the body must match.");

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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var hotel = await _context.Hotels.FindAsync(id);
            if (hotel == null)
                return NotFound();

            _context.Hotels.Remove(hotel);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        
        [HttpGet("{id}/images")]
        public async Task<ActionResult<List<string>>> GetHotelImages(int id)
        {
            var hotel = await _context.Hotels.FindAsync(id);
            if (hotel == null)
                return NotFound();
            
            var images = new List<string>();

            // first add the main image of the hotel
            if (!string.IsNullOrEmpty(hotel.ImageUrl))
            {
                images.Add(hotel.ImageUrl);
            }
            
            // Mapping hotel IDs to folder names in the /Images/.. 
            var hotelFolderMap = new Dictionary<int, string>
            {
                { 1, "HiltonWarsaw" },
                { 2, "RafflesWarsaw" },
                { 3, "WestinWarsaw" },
                { 4, "IbisStylesCentrumWarsaw" },
                { 5, "MercureGrandWarsaw" }
            };
            
            if (!hotelFolderMap.TryGetValue(id, out var folderName))
            {
                //If there is no mapping, try to find a folder by the name of the hotel
                folderName = hotel.Name.Replace(" ", "").Replace("Styles", "").Replace("Centrum", "").Replace("City", "");
            }

            //Path to specific hotel folder 
            var hotelImagesPath = Path.Combine(Directory.GetCurrentDirectory(), "Images", folderName);
            
            if (Directory.Exists(hotelImagesPath))
            {
                // get all pictures of the hotel (except photos of rooms)
                var hotelImages = Directory.GetFiles(hotelImagesPath, "*.jpg")
                    .Where(f => {
                        var fileName = Path.GetFileName(f).ToLower();
                        var fullPath = $"/images/{folderName}/{Path.GetFileName(f)}";
                        
                        // сheck if it's a room image
                        bool isRoomImage = fileName.Contains("double_") || 
                                          fileName.Contains("single_") || 
                                          fileName.Contains("suite_") || 
                                          fileName.Contains("twin_");
                
                        // сheck if it's the main image (case-insensitive comparison)
                        bool isMainImage = !string.IsNullOrEmpty(hotel.ImageUrl) && 
                                          fullPath.Equals(hotel.ImageUrl, StringComparison.OrdinalIgnoreCase);
                
                        return !isRoomImage && !isMainImage;
                    })
                    .OrderBy(f => f)
                    .Select(f => $"/images/{folderName}/{Path.GetFileName(f)}")
                    .ToList();
                
                images.AddRange(hotelImages);
            }
            return Ok(images);
        }
    }
}
