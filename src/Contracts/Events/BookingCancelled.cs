using System;

namespace BookingMicro.Contracts.Events
{
    public record BookingCancelled
    {
        public int BookingId { get; init; }
        public DateTime CanceledAt { get; init; }
        public int RoomId { get; init; }
        
        // Sign that there was a refund request, but payment is not tied
        public bool  HasRefund    { get; init; }
        public string? RefundErrorReason { get; init; }
    }
}