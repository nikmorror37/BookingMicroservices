namespace BookingMicro.Contracts.Events;

public record CancelBookingTimeout
{
    public int BookingId { get; init; }
    public DateTime CreatedAt { get; init; }
} 