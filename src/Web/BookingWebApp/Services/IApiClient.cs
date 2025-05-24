using Refit;
using Microsoft.AspNetCore.Http;

namespace BookingWebApp.Services;

public interface IApiClient
{
    // account
    [Post("/api/account/login")] Task<LoginResponse> Login([Body] LoginRequest req);
    [Post("/api/account/register")] Task Register([Body] RegisterRequest req);
    [Get("/api/account/me")] Task<UserDto> Me();

    // hotels & rooms
    [Get("/api/hotels")] Task<IList<HotelDto>> Hotels([Query] HotelFilter f);
    [Get("/api/rooms")] Task<IList<RoomDto>> Rooms([Query] RoomFilter f);
    [Get("/api/rooms/{id}")] Task<RoomDto> GetRoom(int id);
    [Get("/api/hotels/{id}")] Task<HotelDto> GetHotel(int id);

    // bookings
    [Post("/api/bookings")] Task<BookingDto> CreateBooking([Body] NewBookingDto dto);
    [Get("/api/bookings")] Task<IList<BookingDto>> MyBookings([Query] int page = 1, [Query] int pageSize = 20);
    [Get("/api/bookings/{id}")] Task<BookingDto> GetBookingById(int id);
    [Get("/api/bookings/available")] Task<IList<RoomDto>> AvailableRooms([Query] AvailableFilter f);
    [Post("/api/payments/booking/{id}/pay")] Task PayBooking(int id);
    [Post("/api/bookings/{id}/cancel")] Task CancelBooking(int id);

    // profile
    [Put("/api/account/me")] Task EditProfile([Body] UpdateProfileRequest req);

    [Post("/api/images")] Task<ImageUploadResponse> UploadImage(IFormFile file);
    [Put("/api/hotels/{id}")] Task UpdateHotel(int id, [Body] HotelUpdateRequest hotel);

    [Post("/api/hotels")] Task<HotelDto> CreateHotel([Body] HotelUpdateRequest dto);
    [Delete("/api/hotels/{id}")] Task DeleteHotel(int id);

    [Post("/api/rooms")] Task<RoomDto> CreateRoom([Body] RoomUpdateRequest dto);
    [Put("/api/rooms/{id}")] Task UpdateRoom(int id,[Body] RoomUpdateRequest dto);
    [Delete("/api/rooms/{id}")] Task DeleteRoom(int id);
}

#region DTOs
public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token);
public record RegisterRequest(string Email,string Password,string FirstName,string LastName);
public record UserDto(string Email,string FirstName,string LastName,string? Address,string? City,string? Country);

public record HotelFilter(string? Search,int? MinStars,double? MaxDistance,int Page=1,int PageSize=20);

public enum RoomType{Single,Double,Twin,Suite}
public record RoomDto(int Id,int HotelId,string Number,RoomType Type,decimal Price,string? Description,bool IsAvailable,string? RoomImageUrl,int NumberOfBeds,int Capacity);
public record RoomFilter(int? HotelId,decimal? MinPrice,decimal? MaxPrice,RoomType? Type,int Page=1,int PageSize=20);

public record NewBookingDto(int HotelId,int RoomId,DateTime CheckIn,DateTime CheckOut);
public record BookingDto(int Id,
                          int Status,
                          DateTime CheckIn,
                          DateTime CheckOut,
                          int? PaymentId,
                          RoomDto Room,
                          HotelDto Hotel)
{
    // Удобные аксессоры, чтобы старый код не падал, пока мы постепенно мигрируем шаблоны/контроллеры
    public int RoomId  => Room?.Id  ?? 0;
    public int HotelId => Hotel?.Id ?? 0;
}
public enum BookingStatus{Pending=0,Confirmed=1,Cancelled=2,RefundError=3}
public record AvailableFilter(int HotelId,DateTime CheckIn,DateTime CheckOut);

public record UpdateProfileRequest(string? FirstName,string? LastName,string? Address,string? City,string? State,string? PostalCode,string? Country,DateTime? DateOfBirth);

public record ImageUploadResponse(string ImageUrl);
public record HotelUpdateRequest(int Id,string Name,string Address,string City,string Country,int Stars,double DistanceFromCenter,string? ImageUrl,string? Description);

public record RoomUpdateRequest(int Id,int HotelId,string Number,RoomType Type,decimal Price,string? Description,int Capacity,int NumberOfBeds,bool IsAvailable,string? RoomImageUrl);
#endregion 