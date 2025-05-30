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

        // [HttpPost]
        // [Authorize(Roles = "Admin")]
        // public async Task<IActionResult> UploadImage(IFormFile file)
        // {
        //     if (file == null || file.Length == 0)
        //         return BadRequest("No file uploaded");

        //     // Validate file is an image
        //     var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        //     if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".gif")
        //         return BadRequest("Invalid file type. Only images (.jpg, .jpeg, .png, .gif) are allowed.");

        //     // Create unique filename
        //     var fileName = $"{Guid.NewGuid()}{extension}";
        //     var filePath = Path.Combine(_environment.ContentRootPath, "Images", fileName);

        //     using (var stream = new FileStream(filePath, FileMode.Create))
        //     {
        //         await file.CopyToAsync(stream);
        //     }

        //     // Return the URL to the uploaded image
        //     var imageUrl = $"/images/{fileName}";
        //     return Ok(new { ImageUrl = imageUrl });
        // }

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

            // Если указан hotelId, сохраняем в папку отеля
            if (hotelId.HasValue && hotelId.Value > 0)
            {
                var hotel = await _context.Hotels.FindAsync(hotelId.Value);
                if (hotel == null)
                    return NotFound("Hotel not found");

                // Используем ту же логику определения папки, что и в UploadAdditionalImages
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
                    // folderName = hotel.Name
                    //     .Replace(" ", "")
                    //     .Replace("'", "")
                    //     .Replace(".", "")
                    //     .Replace(",", "")
                    //     .Replace("-", "");
                }

                var hotelFolderPath = Path.Combine(_environment.ContentRootPath, "Images", folderName);
                if (!Directory.Exists(hotelFolderPath))
                {
                    Directory.CreateDirectory(hotelFolderPath);
                }

                // Для главного фото используем понятное имя
                var baseFileName = folderName.ToLower();
                fileName = $"{baseFileName}_main{extension}";
                filePath = Path.Combine(hotelFolderPath, fileName);

                // Если файл уже существует, добавляем timestamp
                if (System.IO.File.Exists(filePath))  // Используем полное имя пространства имен
                {
                    fileName = $"{baseFileName}_main_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                    filePath = Path.Combine(hotelFolderPath, fileName);
                }

                imageUrl = $"/images/{folderName}/{fileName}";
            }
            else
            {
                // Для загрузок без hotelId используем GUID в корне (обратная совместимость)
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


        // НОВЫЙ метод для загрузки дополнительных фото в папку конкретного отеля
        [HttpPost("hotel/{hotelId}/additional")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadAdditionalImages(int hotelId, List<IFormFile> files)
        {
            // Проверяем, что файлы переданы
            if (files == null || files.Count == 0)
                return BadRequest("No files uploaded");

            // Находим отель в базе данных, чтобы получить его имя
            var hotel = await _context.Hotels.FindAsync(hotelId);
            if (hotel == null)
                return NotFound("Hotel not found");

            // Словарь для маппинга ID существующих отелей на имена их папок
            // Это нужно для совместимости с уже существующими папками seed-отелей
            var hotelFolderMap = new Dictionary<int, string>
            {
                { 1, "HiltonWarsaw" },
                { 2, "RafflesWarsaw" },
                { 3, "WestinWarsaw" },
                { 4, "IbisStylesCentrumWarsaw" },
                { 5, "MercureGrandWarsaw" }
            };

            string folderName;
            // Если это один из seed-отелей, используем существующее имя папки
            if (!hotelFolderMap.TryGetValue(hotelId, out folderName))
            {
                folderName = NormalizeFolderName(hotel.Name);
                // Для новых отелей создаем имя папки из названия отеля
                // Убираем пробелы и спецсимволы для безопасности файловой системы
                // folderName = hotel.Name
                //     .Replace(" ", "")
                //     .Replace("'", "")
                //     .Replace(".", "")
                //     .Replace(",", "")
                //     .Replace("-", "");
            }

            // Формируем полный путь к папке отеля
            var hotelFolderPath = Path.Combine(_environment.ContentRootPath, "Images", folderName);

            // Создаем папку, если она не существует (для новых отелей)
            if (!Directory.Exists(hotelFolderPath))
            {
                Directory.CreateDirectory(hotelFolderPath);
            }

            // Список URL загруженных изображений для возврата клиенту
            var uploadedUrls = new List<string>();

            // Находим количество уже существующих дополнительных фото в папке
            // Ищем файлы с паттерном "hotel-*.jpg" чтобы правильно пронумеровать новые
            var existingAdditionalPhotos = Directory.GetFiles(hotelFolderPath, $"hotel-{hotelId}-*.jpg").Length;
            var photoCounter = existingAdditionalPhotos + 1;

            // Обрабатываем каждый загруженный файл
            foreach (var file in files)
            {
                // Получаем расширение файла и проверяем, что это изображение
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".gif")
                {
                    // Пропускаем не-изображения, но продолжаем обработку остальных
                    continue;
                }

                // Формируем имя файла в формате "hotel-1.jpg", "hotel-2.jpg" и т.д.
                var fileName = $"hotel-{hotelId}-{photoCounter}{extension}";
                var filePath = Path.Combine(hotelFolderPath, fileName);

                // Сохраняем файл на диск
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Добавляем URL сохраненного изображения в список для возврата
                uploadedUrls.Add($"/images/{folderName}/{fileName}");

                // Увеличиваем счетчик для следующего фото
                photoCounter++;
            }

            // Возвращаем список URL загруженных изображений
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