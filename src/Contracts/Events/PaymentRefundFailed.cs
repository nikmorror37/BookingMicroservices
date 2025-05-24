using System;

namespace BookingMicro.Contracts.Events
{
    public record PaymentRefundFailed
    {
        public int PaymentId  { get; init; }
        public int BookingId  { get; init; }
        public string Reason  { get; init; } = default!;
        public DateTime FailedAt { get; init; }
    }
}