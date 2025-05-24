using BookingService.API.Infrastructure.Clients; 
namespace BookingService.API.Domain.Models
{
    public class BookingDetailDto
    {
        public int    Id       { get; set; }
        public DateTime CheckIn  { get; set; }
        public DateTime CheckOut { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime? CanceledAt { get; set; }
        public string? RefundErrorReason { get; set; } //new
        public int? PaymentId { get; set; }
        public RoomDto Room     { get; set; } = default!;
        public HotelDto Hotel { get; set; } = default!;
    }
}