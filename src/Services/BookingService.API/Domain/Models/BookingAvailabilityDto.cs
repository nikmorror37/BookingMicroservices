namespace BookingService.API.Domain.Models
{
    public class BookingAvailabilityDto
    {
        public int RoomId { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
    }
}
