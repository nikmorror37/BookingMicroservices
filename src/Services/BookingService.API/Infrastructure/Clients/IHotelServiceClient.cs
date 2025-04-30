using System.Threading.Tasks;
using BookingService.API.Infrastructure.Clients;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace BookingService.API.Infrastructure.Clients
{
    public interface IHotelServiceClient
    {
        Task<HotelDto> GetHotelByIdAsync(int hotelId);
    }

    public class HotelDto
    {
        public int    Id                  { get; set; }
        public string Name                { get; set; } = default!;
        public string Address             { get; set; } = default!;
        public string City                { get; set; } = default!;
        public string Country             { get; set; } = default!;
        public int    Stars               { get; set; }
        public double DistanceFromCenter  { get; set; }
        public string? ImageUrl           { get; set; }
    }
}
