namespace BookingMicro.Contracts.Events;

public record RoomReserveRejected
{
    public int BookingId { get; init; }
    public int RoomId    { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime RejectedAt { get; init; }
} 