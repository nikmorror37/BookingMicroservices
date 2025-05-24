using System;

namespace BookingMicro.Contracts.Events
{
    public class PaymentCreated
    {
        public int PaymentId { get; set; }
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public int Status { get; set; }
    }
} 