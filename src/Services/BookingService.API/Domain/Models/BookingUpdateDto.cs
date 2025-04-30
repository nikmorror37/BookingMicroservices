namespace BookingService.API.Domain.Models
{
    public class BookingUpdateDto
    {
        public DateTime CheckIn  { get; set; }
        public DateTime CheckOut { get; set; }
    }
}
