namespace BookingService.API.Domain.Models
{
  public class Booking
  {
    public int Id { get; set; }
    public int HotelId { get; set; }
    public int RoomId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public string UserId { get; set; } = null!;
    public BookingStatus Status { get; set; }

    //new
    // public bool IsCanceled { get; set; }
    // public DateTime? CanceledAt { get; set; }
  }

  public enum BookingStatus { Pending, Confirmed, Cancelled }
}
