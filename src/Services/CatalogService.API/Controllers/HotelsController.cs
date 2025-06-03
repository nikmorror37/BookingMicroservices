using CatalogService.API.Infrastructure.Data;
using CatalogService.API.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Text.RegularExpressions; 

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

        private static string NormalizeFolderName(string text)
        => Regex.Replace(text, "[^A-Za-z0-9]", "");

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
            [FromQuery] string? sort = null, //add sort parameter
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
                var normalizedSearch = Regex.Replace(search.Trim(), @"\s+", " ").ToLower();
                var words = normalizedSearch.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (words.Length == 1)
                {
                    var w = words[0];
                    query = query.Where(h =>
                        EF.Functions.Like(h.Name, $"%{w}%") ||
                        EF.Functions.Like(h.City, $"%{w}%"));
                }
                else
                {
                    foreach (var w in words)
                    {
                        var term = w;             
                        query = query.Where(h =>
                            EF.Functions.Like(h.Name, $"%{term}%") ||
                            EF.Functions.Like(h.City, $"%{term}%"));
                    }
                }
            }

            if (minStars.HasValue)
            {
                query = query.Where(h => h.Stars >= minStars.Value);
            }
            if (maxDist.HasValue)
            {
                query = query.Where(h => h.DistanceFromCenter <= maxDist.Value);
            }

            // using sorting BEFORE pagination
            switch (sort?.ToLowerInvariant()) 
            {
                case "stars_desc":
                    query = query.OrderByDescending(h => h.Stars).ThenBy(h => h.Name);
                    break;
                case "stars_asc":
                    query = query.OrderBy(h => h.Stars).ThenBy(h => h.Name);
                    break;
                case "name":
                    query = query.OrderBy(h => h.Name);
                    break;
                default: 
                    query = query.OrderBy(h => h.Name);
                    break;
            }

            // total number (for front-line pagination)
            var totalCount = await query.CountAsync();
            Response.Headers.Add("X-Total-Count", totalCount.ToString());

            // apply sorting, paining and execute the request
            var hotels = await query
                //.OrderBy(h => h.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(hotels);
        }

        /// GET api/hotels/{id}
        [HttpGet("{id:int}", Name = "GetHotelById")]
        public async Task<ActionResult<Hotel>> GetById(int id)
        {
            var hotel = await _context.Hotels.FindAsync(id);
            if (hotel == null)
                return NotFound();
            return Ok(hotel);
        }

        /// POST api/hotels
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Hotel>> Create([FromBody] Hotel hotel)
        {
            // Add the hotel to the database context
            _context.Hotels.Add(hotel);
            // Save to the database to get the generated ID
            await _context.SaveChangesAsync();

            // Create a folder for images of the new hotel
            // Generate the folder name from the hotel name, removing wildcards (spec symbols)
            var folderName = hotel.Name
                .Replace(" ", "")
                .Replace("'", "")
                .Replace("_", "")
                .Replace(".", "")
                .Replace(",", "")
                .Replace("-", "");

            // Get the path to the Images folder and add the name of the hotel folder
            var hotelFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Images", folderName);

            if (!Directory.Exists(hotelFolderPath))
            {
                Directory.CreateDirectory(hotelFolderPath);
            }

            // Return the created hotel with the location in the header
            return CreatedAtAction(nameof(GetById), new { id = hotel.Id }, hotel);
        }

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
                folderName = NormalizeFolderName(hotel.Name);
            }

            //Path to specific hotel folder 
            var hotelImagesPath = Path.Combine(Directory.GetCurrentDirectory(), "Images", folderName);

            if (Directory.Exists(hotelImagesPath))
            {
                // get all pictures of the hotel (except photos of rooms)
                var validExt = new[] { ".jpg", ".jpeg", ".png", ".gif" };

                var hotelImages = Directory.EnumerateFiles(hotelImagesPath)
                    .Where(f =>
                    {
                        var ext = Path.GetExtension(f).ToLower();
                        if (!validExt.Contains(ext)) return false;

                        var fileName = Path.GetFileName(f).ToLower();
                        var fullPath = $"/images/{folderName}/{Path.GetFileName(f)}".ToLower(); //before was without .ToLower()

                        // сheck if it's a room image
                        bool isRoomImage = fileName.Contains("double_") ||
                                          fileName.Contains("single_") ||
                                          fileName.Contains("suite_") ||
                                          fileName.Contains("twin_");

                        // сheck if it's the main image (case-insensitive comparison)
                        bool isMainImage = !string.IsNullOrEmpty(hotel.ImageUrl) &&
                                           fullPath == hotel.ImageUrl.ToLower();
                                           
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
