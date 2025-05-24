using System;

namespace BookingMicro.Contracts.Events
{
    public record PaymentRefunded
    {
        public int PaymentId   { get; init; }
        public int BookingId   { get; init; }
        public decimal Amount  { get; init; }
        public DateTime RefundedAt { get; init; }
    }
}












