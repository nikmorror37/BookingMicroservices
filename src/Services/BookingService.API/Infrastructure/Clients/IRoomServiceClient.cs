using BookingService.API.Infrastructure.Clients;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookingService.API.Infrastructure.Clients
{
    public interface IRoomServiceClient
    {
        Task<IEnumerable<RoomDto>> GetRoomsByHotelAsync(int hotelId);
        Task<RoomDto> GetRoomByIdAsync(int roomId);
    }
    
    public enum RoomType
    {
        Single,
        Double,
        Twin,
        Suite
    }

    public class RoomDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("hotelId")]
        public int HotelId   { get; set; }

        [JsonPropertyName("number")]
        public string Number { get; set; } = default!;

        [JsonPropertyName("type")]
        public RoomType Type { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("isAvailable")]
        public bool IsAvailable { get; set; }
        
        [JsonPropertyName("roomImageUrl")]
        public string?  RoomImageUrl  { get; set; }

        [JsonPropertyName("numberOfBeds")]
        public int      NumberOfBeds  { get; set; }

        [JsonPropertyName("capacity")]
        public int      Capacity      { get; set; }
    }
}
