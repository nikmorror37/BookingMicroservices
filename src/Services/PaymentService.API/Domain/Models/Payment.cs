namespace PaymentService.API.Domain.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public DateTime PaidAt { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public DateTime? RefundedAt { get; set; }
        // TODO: will add Type
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Refunded,
        RefundError
    }
}
