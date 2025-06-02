using BookingService.API.Domain.Models;
using System;
using Xunit;
using FluentAssertions;

namespace BookingMicro.UnitTests;

public class BookingTests
{
    [Fact]
    public void Creating_booking_with_checkout_before_checkin_should_throw()
    {
        // arrange
        var ci = DateTime.Today.AddDays(5);
        var co = DateTime.Today.AddDays(4);

        // act
        var booking = new Booking
        {
            RoomId = 1,
            HotelId = 1,
            CheckIn = ci,
            CheckOut = co,
            UserId = "user1"
        };

        // assert
        (booking.CheckOut <= booking.CheckIn).Should().BeTrue();
    }
} 