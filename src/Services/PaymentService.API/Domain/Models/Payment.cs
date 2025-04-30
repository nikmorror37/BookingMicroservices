namespace PaymentService.API.Domain.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public DateTime PaidAt { get; set; }
        // TODO: will add Status, Type
    }
}
