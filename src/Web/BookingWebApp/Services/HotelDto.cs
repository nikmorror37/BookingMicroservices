namespace BookingWebApp.Services
{
    public record HotelDto(
        int Id, 
        string Name, 
        string Address,
        string City, 
        string Country, 
        int Stars, 
        double DistanceFromCenter,
        string? ImageUrl,
        string? Description
    );
} 