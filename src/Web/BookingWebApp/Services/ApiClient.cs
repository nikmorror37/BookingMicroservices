using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;
using System.Net;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace BookingWebApp.Services;

public class ApiClient : IApiClient
{
    private readonly HttpClient _client;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(HttpClient client, IHttpContextAccessor httpContextAccessor)
    {
        _client = client;
        _client.Timeout = TimeSpan.FromSeconds(30); // because default HttpClient waits for 100 seconds
        _httpContextAccessor = httpContextAccessor;
    }

    private async Task SetAuthHeader()
    {
        var accessToken = await _httpContextAccessor.HttpContext!.GetTokenAsync("access_token");
        if (!string.IsNullOrEmpty(accessToken))
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
    }

    public async Task<IList<HotelDto>> Hotels(HotelFilter filter)
    {
        await SetAuthHeader();

        var query = new StringBuilder($"api/hotels?page={filter.Page}&pageSize={filter.PageSize}");
        if (!string.IsNullOrEmpty(filter.Search))
            query.Append($"&search={filter.Search}");
        if (filter.MinStars.HasValue)
            query.Append($"&minStars={filter.MinStars}");
        if (filter.MaxDistance.HasValue)
            query.Append($"&maxDistance={filter.MaxDistance}");

        var response = await _client.GetAsync(query.ToString());
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var hotels = JsonSerializer.Deserialize<List<HotelDto>>(content, _jsonOptions) ?? new();

        return hotels;
    }

    public async Task<HotelDto> GetHotel(int id)
    {
        await SetAuthHeader();
        var response = await _client.GetAsync($"api/hotels/{id}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var hotel = JsonSerializer.Deserialize<HotelDto>(content, _jsonOptions)
            ?? throw new Exception($"Failed to deserialize hotel with id {id}");

        return hotel;
    }

    public async Task<RoomDto> GetRoom(int id)
    {
        await SetAuthHeader();
        var response = await _client.GetAsync($"api/rooms/{id}");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<RoomDto>(content, _jsonOptions) ?? throw new Exception("Failed to deserialize room");
    }

    public async Task<IList<RoomDto>> Rooms(RoomFilter filter)
    {
        await SetAuthHeader();
        var query = new StringBuilder($"api/rooms?page={filter.Page}&pageSize={filter.PageSize}");
        if (filter.HotelId.HasValue) query.Append($"&hotelId={filter.HotelId}");
        if (filter.MinPrice.HasValue) query.Append($"&minPrice={filter.MinPrice}");
        if (filter.MaxPrice.HasValue) query.Append($"&maxPrice={filter.MaxPrice}");
        if (filter.Type.HasValue) query.Append($"&type={filter.Type}");

        var response = await _client.GetAsync(query.ToString());
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var rooms = JsonSerializer.Deserialize<List<RoomDto>>(content, _jsonOptions) ?? new();
        return rooms;
    }

    public async Task<IList<RoomDto>> AvailableRooms(AvailableFilter filter)
    {
        await SetAuthHeader();
        var checkIn = filter.CheckIn.ToString("yyyy-MM-dd");
        var checkOut = filter.CheckOut.ToString("yyyy-MM-dd");
        var response = await _client.GetAsync($"api/bookings/available?hotelId={filter.HotelId}&checkIn={checkIn}&checkOut={checkOut}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var rooms = JsonSerializer.Deserialize<List<RoomDto>>(content, _jsonOptions) ?? new();

        return rooms;
    }

    public async Task<IList<BookingDto>> MyBookings(int page = 1, int pageSize = 20)
    {
        await SetAuthHeader();
        var response = await _client.GetAsync($"api/bookings?page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var bookings = JsonSerializer.Deserialize<List<BookingDto>>(content, _jsonOptions) ?? new();

        return bookings;
    }

    public async Task<LoginResponse> Login(LoginRequest req)
    {
        var response = await _client.PostAsync("api/account/login", new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<LoginResponse>(content, _jsonOptions) ?? throw new Exception("Invalid login response");
    }

    public async Task Register(RegisterRequest req)
    {
        var response = await _client.PostAsync("api/account/register", new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
    }

    public async Task<UserDto> Me()
    {
        await SetAuthHeader();
        var response = await _client.GetAsync("api/account/me");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<UserDto>(content, _jsonOptions) ?? throw new Exception("Failed to deserialize user");
    }

    public async Task<BookingDto> CreateBooking(NewBookingDto dto)
    {
        await SetAuthHeader();
        var response = await _client.PostAsync("api/bookings", new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<BookingDto>(json, _jsonOptions) ?? throw new Exception("Failed to create booking");
    }

    public async Task PayBooking(int id)
    {
        await SetAuthHeader();
        var response = await _client.PostAsync($"api/payments/booking/{id}/pay", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task CancelBooking(int id)
    {
        await SetAuthHeader();
        var response = await _client.PostAsync($"api/bookings/{id}/cancel", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task<BookingDto> GetBookingById(int id)
    {
        await SetAuthHeader();
        var response = await _client.GetAsync($"api/bookings/{id}");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<BookingDto>(content, _jsonOptions) ?? throw new Exception("Failed deserialize booking");
    }

    public async Task EditProfile(UpdateProfileRequest req)
    {
        await SetAuthHeader();
        var response = await _client.PutAsync("api/account/me", new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
    }

    // public async Task<ImageUploadResponse> UploadImage(IFormFile file)
    // {
    //     await SetAuthHeader();
    //     using var ms = new MemoryStream();
    //     await file.CopyToAsync(ms);
    //     ms.Position = 0;
    //     var content = new MultipartFormDataContent();
    //     var fileContent = new ByteArrayContent(ms.ToArray());
    //     fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
    //     content.Add(fileContent, "file", file.FileName);
    //     var response = await _client.PostAsync("/api/images", content);
    //     response.EnsureSuccessStatusCode();
    //     var json = await response.Content.ReadAsStringAsync();
    //     return JsonSerializer.Deserialize<ImageUploadResponse>(json, _jsonOptions) ?? throw new Exception("Cannot deserialize image response");
    // }
    
    public async Task<ImageUploadResponse> UploadImage(IFormFile file, int? hotelId = null)
    {
        await SetAuthHeader();
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        ms.Position = 0;
        
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(ms.ToArray());
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.FileName);
        
        // Добавляем hotelId в URL если указан
        var url = hotelId.HasValue ? $"/api/images?hotelId={hotelId}" : "/api/images";
        
        var response = await _client.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ImageUploadResponse>(json, _jsonOptions) ?? throw new Exception("Cannot deserialize image response");
    }

    // НОВЫЙ метод для загрузки дополнительных изображений в папку отеля
    public async Task<UploadAdditionalImagesResponse> UploadAdditionalImages(int hotelId, List<IFormFile> files)
    {
        // Устанавливаем JWT токен для авторизации запроса
        await SetAuthHeader();

        // Создаем multipart/form-data контент для передачи файлов
        using var content = new MultipartFormDataContent();

        // Обрабатываем каждый файл из списка
        foreach (var file in files)
        {
            // Создаем поток памяти для временного хранения файла
            using var ms = new MemoryStream();
            // Копируем содержимое файла в поток
            await file.CopyToAsync(ms);
            // Возвращаем указатель потока в начало
            ms.Position = 0;

            // Создаем контент из массива байтов файла
            var fileContent = new ByteArrayContent(ms.ToArray());
            // Устанавливаем правильный Content-Type для файла
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            // Добавляем файл в multipart контент с именем "files" (должно совпадать с параметром на сервере)
            content.Add(fileContent, "files", file.FileName);
        }

        // Отправляем POST запрос на сервер
        var response = await _client.PostAsync($"/api/images/hotel/{hotelId}/additional", content);
        // Проверяем успешность запроса, иначе выбросит исключение
        response.EnsureSuccessStatusCode();

        // Читаем JSON ответ от сервера
        var json = await response.Content.ReadAsStringAsync();
        // Десериализуем ответ в объект UploadAdditionalImagesResponse
        return JsonSerializer.Deserialize<UploadAdditionalImagesResponse>(json, _jsonOptions)
            ?? new UploadAdditionalImagesResponse(new List<string>());
    }

    public async Task<ImageUploadResponse> UploadRoomImage(
                                            IFormFile file,
                                            int       hotelId,
                                            string    roomNumber,
                                            RoomType  type)
    {
        await SetAuthHeader();

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        ms.Position = 0;

        var content = new MultipartFormDataContent();
        var fc = new ByteArrayContent(ms.ToArray());
        fc.Headers.ContentType =
            new MediaTypeHeaderValue(file.ContentType);
        content.Add(fc, "file", file.FileName);

        var url =
            $"/api/images/hotel/{hotelId}/room" +
            $"?roomNumber={Uri.EscapeDataString(roomNumber)}" +
            $"&roomType={type}";

        var resp = await _client.PostAsync(url, content);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ImageUploadResponse>(json, _jsonOptions)
            ?? throw new Exception("Cannot deserialize image response");
    }

    public async Task UpdateHotel(int id, HotelUpdateRequest hotel)
    {
        await SetAuthHeader();
        var json = JsonSerializer.Serialize(hotel);
        var response = await _client.PutAsync($"api/hotels/{id}", new StringContent(json, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
    }

    public async Task<HotelDto> CreateHotel(HotelUpdateRequest dto)
    {
        await SetAuthHeader();
        var json = JsonSerializer.Serialize(dto);
        var resp = await _client.PostAsync("/api/hotels", new StringContent(json, Encoding.UTF8, "application/json"));
        resp.EnsureSuccessStatusCode();
        var content = await resp.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<HotelDto>(content, _jsonOptions)!;
    }

    public async Task DeleteHotel(int id)
    {
        await SetAuthHeader();
        var resp = await _client.DeleteAsync($"/api/hotels/{id}");
        resp.EnsureSuccessStatusCode();
    }

    public async Task<RoomDto> CreateRoom(RoomUpdateRequest dto) { await SetAuthHeader(); var resp = await _client.PostAsync("/api/rooms", new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json")); resp.EnsureSuccessStatusCode(); var json = await resp.Content.ReadAsStringAsync(); return JsonSerializer.Deserialize<RoomDto>(json, _jsonOptions)!; }
    public async Task UpdateRoom(int id, RoomUpdateRequest dto) { await SetAuthHeader(); var resp = await _client.PutAsync($"/api/rooms/{id}", new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json")); resp.EnsureSuccessStatusCode(); }
    public async Task DeleteRoom(int id) { await SetAuthHeader(); var resp = await _client.DeleteAsync($"/api/rooms/{id}"); resp.EnsureSuccessStatusCode(); }
    public async Task<List<string>> GetHotelImagesAsync(int hotelId)
    {
        try
        {
            var response = await _client.GetAsync($"/api/hotels/{hotelId}/images");
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return new List<string>();
                
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<string>>(json, _jsonOptions) ?? new List<string>();
        }
        catch (Exception)
        {
            return new List<string>();
        }
    }
} 