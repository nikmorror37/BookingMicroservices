namespace BookingMicro.Contracts.Events;

public record RoomReserveRequested
{
    public int BookingId { get; init; }
    public int HotelId   { get; init; }
    public int RoomId    { get; init; }
    public DateTime CheckIn  { get; init; }
    public DateTime CheckOut { get; init; }
} 