using System;

namespace BookingMicro.Contracts.Events
{
    public record BookingCreated
    {
        public int BookingId { get; init; }
        public int HotelId   { get; init; }
        public int RoomId    { get; init; }
        public string UserId { get; init; } = default!;
        public DateTime CheckIn  { get; init; }
        public DateTime CheckOut { get; init; }
    }
}
