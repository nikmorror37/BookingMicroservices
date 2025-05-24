namespace BookingMicro.Contracts.Events;

public record RoomReserved
{
    public int BookingId  { get; init; }
    public int RoomId     { get; init; }
    public DateTime ReservedAt { get; init; }
} 