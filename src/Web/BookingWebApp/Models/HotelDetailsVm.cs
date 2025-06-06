namespace BookingWebApp.Models;
using BookingWebApp.Services;

public record HotelDetailsVm(
    HotelDto Hotel,
    IList<RoomDto>? AvailableRooms,
    DateTime? CheckIn,
    DateTime? CheckOut) 
{
    public List<string> AdditionalImages { get; init; } = new();
}