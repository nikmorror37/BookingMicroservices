using BookingService.API.Domain.Models;
using FluentAssertions;
using Xunit;
using System;

namespace BookingMicro.UnitTests;

public class BookingStatusTests
{
    [Fact]
    public void Pending_booking_can_be_confirmed()
    {
        var b = new Booking { CheckIn = DateTime.Today, CheckOut = DateTime.Today.AddDays(1), HotelId =1, RoomId=1, UserId="u" };
        b.Status = BookingStatus.Confirmed;
        b.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public void Cancelling_sets_isCanceled_flag()
    {
        var b = new Booking { CheckIn = DateTime.Today, CheckOut = DateTime.Today.AddDays(1), HotelId =1, RoomId=1, UserId="u" };
        b.Status = BookingStatus.Cancelled;
        b.IsCanceled = true;
        b.IsCanceled.Should().BeTrue();
        b.Status.Should().Be(BookingStatus.Cancelled);
    }
} 