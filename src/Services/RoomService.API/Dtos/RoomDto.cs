using RoomService.API.Domain.Models;

namespace RoomService.API.Dtos
{
    public class RoomDto
    {
        public int    Id            { get; set; }
        public int    HotelId       { get; set; }
        public string Number        { get; set; } = default!;
        public RoomType Type        { get; set; }
        public decimal Price        { get; set; }
        public string? Description  { get; set; }
        public bool   IsAvailable   { get; set; }
        public string? RoomImageUrl { get; set; }
        public int     NumberOfBeds { get; set; }
        public int     Capacity     { get; set; }
    }
}