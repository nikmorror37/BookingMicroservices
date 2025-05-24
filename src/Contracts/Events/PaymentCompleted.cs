using System;

namespace BookingMicro.Contracts.Events
{
    public record PaymentCompleted
    {
        public int PaymentId  { get; init; }
        public int BookingId  { get; init; }
        public decimal Amount { get; init; }
        public string Currency { get; init; } = "USD";
        public DateTime PaidAt { get; init; }
    }
}
