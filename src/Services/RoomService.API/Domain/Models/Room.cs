namespace RoomService.API.Domain.Models
{
    public class Room
    {
        public int Id { get; set; }
        public int HotelId   { get; set; }
        public string Number { get; set; } = null!;            
        public RoomType Type { get; set; }                     
        public decimal Price { get; set; }                     
        public string? Description { get; set; }               
        public bool IsAvailable { get; set; }
        public string? RoomImageUrl   { get; set; }
        public int     NumberOfBeds   { get; set; }
        public int     Capacity       { get; set; }                  
    }

    public enum RoomType
    {
        Single,
        Double,
        Twin,
        Suite
    }
}
