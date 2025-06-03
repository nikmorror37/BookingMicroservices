using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.Threading.Tasks;
using CatalogService.API.Infrastructure.Data;
using System.Text.RegularExpressions;

namespace CatalogService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImagesController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly CatalogDbContext _context;

        public ImagesController(IWebHostEnvironment environment, CatalogDbContext context)
        {
            _environment = environment;
            _context = context;
        }

        private static string NormalizeFolderName(string text)
        => Regex.Replace(text, "[^A-Za-z0-9]", "");

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadImage(IFormFile file, [FromQuery] int? hotelId = null)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".gif")
                return BadRequest("Invalid file type. Only images (.jpg, .jpeg, .png, .gif) are allowed.");

            string fileName;
            string filePath;
            string imageUrl;

            if (hotelId.HasValue && hotelId.Value > 0)
            {
                var hotel = await _context.Hotels.FindAsync(hotelId.Value);
                if (hotel == null)
                    return NotFound("Hotel not found");

                // use the same logic for defining the folder as in UploadAdditionalImages
                var hotelFolderMap = new Dictionary<int, string>
                {
                    { 1, "HiltonWarsaw" },
                    { 2, "RafflesWarsaw" },
                    { 3, "WestinWarsaw" },
                    { 4, "IbisStylesCentrumWarsaw" },
                    { 5, "MercureGrandWarsaw" }
                };

                string folderName;
                if (!hotelFolderMap.TryGetValue(hotelId.Value, out folderName))
                {
                    folderName = NormalizeFolderName(hotel.Name);
                }

                var hotelFolderPath = Path.Combine(_environment.ContentRootPath, "Images", folderName);
                if (!Directory.Exists(hotelFolderPath))
                {
                    Directory.CreateDirectory(hotelFolderPath);
                }

                var baseFileName = folderName.ToLower();
                fileName = $"{baseFileName}_main{extension}";
                filePath = Path.Combine(hotelFolderPath, fileName);

                if (System.IO.File.Exists(filePath)) 
                {
                    fileName = $"{baseFileName}_main_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                    filePath = Path.Combine(hotelFolderPath, fileName);
                }

                imageUrl = $"/images/{folderName}/{fileName}";
            }
            else
            {
                // for downloads without hotelId is used the GUID in the root (backwards compatibility)
                fileName = $"{Guid.NewGuid()}{extension}";
                filePath = Path.Combine(_environment.ContentRootPath, "Images", fileName);
                imageUrl = $"/images/{fileName}";
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new { ImageUrl = imageUrl });
        }


        // method for uploading additional photos to a specific hotel's folder
        [HttpPost("hotel/{hotelId}/additional")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadAdditionalImages(int hotelId, List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest("No files uploaded");

            var hotel = await _context.Hotels.FindAsync(hotelId);
            if (hotel == null)
                return NotFound("Hotel not found");

            // Dictionary for mapping IDs of existing hotels to their folder names
            // This is needed for compatibility with already existing seed hotel folders
            var hotelFolderMap = new Dictionary<int, string>
            {
                { 1, "HiltonWarsaw" },
                { 2, "RafflesWarsaw" },
                { 3, "WestinWarsaw" },
                { 4, "IbisStylesCentrumWarsaw" },
                { 5, "MercureGrandWarsaw" }
            };

            string folderName;
            if (!hotelFolderMap.TryGetValue(hotelId, out folderName))
            {
                folderName = NormalizeFolderName(hotel.Name);
            }

            // form the full path to the hotel folder
            var hotelFolderPath = Path.Combine(_environment.ContentRootPath, "Images", folderName);

            if (!Directory.Exists(hotelFolderPath))
            {
                Directory.CreateDirectory(hotelFolderPath);
            }

            var uploadedUrls = new List<string>();

            // Find the number of already existing additional photos in the folder
            // Look for files with the pattern "hotel-*.jpg" to correctly number the new ones
            var existingAdditionalPhotos = Directory.GetFiles(hotelFolderPath, $"hotel-{hotelId}-*.jpg").Length;
            var photoCounter = existingAdditionalPhotos + 1;

            foreach (var file in files)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".gif")
                {
                    continue;
                }

                var fileName = $"hotel-{hotelId}-{photoCounter}{extension}";
                var filePath = Path.Combine(hotelFolderPath, fileName);

                // save file to the disk
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // add the URL of the saved image to the return list
                uploadedUrls.Add($"/images/{folderName}/{fileName}");

                photoCounter++;
            }

            return Ok(new { imageUrls = uploadedUrls });
        }

        
        [HttpPost("hotel/{hotelId}/room")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadRoomImage(
            int         hotelId,
            IFormFile   file,
            [FromQuery] string roomNumber,
            [FromQuery] string roomType)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var valid = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            if (!valid.Contains(ext))
                return BadRequest("Only jpg / jpeg / png / gif");

            var hotel = await _context.Hotels.FindAsync(hotelId);
            if (hotel == null) return NotFound("Hotel not found");

            var hotelFolderMap = new Dictionary<int,string>{
                {1,"HiltonWarsaw"},{2,"RafflesWarsaw"},{3,"WestinWarsaw"},
                {4,"IbisStylesCentrumWarsaw"},{5,"MercureGrandWarsaw"}
            };
            var folderName = hotelFolderMap.TryGetValue(hotelId,out var name)
                ? name
                : Regex.Replace(hotel.Name,"[^A-Za-z0-9]","");

            var folderPath = Path.Combine(_environment.ContentRootPath,"Images",folderName);
            if(!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            var safeType = roomType.ToLower();
            var fileName = $"{roomNumber}_{safeType}_{folderName}{ext}";
            var filePath = Path.Combine(folderPath,fileName);
            if (System.IO.File.Exists(filePath))
            {
                fileName = $"{roomNumber}_{safeType}_{folderName}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
                filePath = Path.Combine(folderPath,fileName);
            }

            await using var fs = new FileStream(filePath,FileMode.Create);
            await file.CopyToAsync(fs);

            var url = $"/images/{folderName}/{fileName}";
            return Ok(new { ImageUrl = url });
        }

    }
} 