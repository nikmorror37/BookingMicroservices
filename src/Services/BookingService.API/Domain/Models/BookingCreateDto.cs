namespace BookingService.API.Domain.Models
{
  public class BookingCreateDto
  {
    public int     HotelId   { get; set; }
    public int     RoomId    { get; set; }
    public DateTime CheckIn  { get; set; }
    public DateTime CheckOut { get; set; }
  }
}
