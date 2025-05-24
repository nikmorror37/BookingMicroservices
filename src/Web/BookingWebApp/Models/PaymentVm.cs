namespace BookingWebApp.Models;
using BookingWebApp.Services;

public enum CardType{Visa,MasterCard}
public record PaymentVm(BookingDto Booking,decimal TotalAmount); 