using PaymentService.API.Domain.Models;
using FluentAssertions;
using Xunit;

namespace BookingMicro.UnitTests;

public class PaymentTests
{
    [Fact]
    public void New_payment_should_be_pending_by_default()
    {
        var p = new Payment { BookingId = 1, Amount = 100m };
        p.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public void Can_mark_payment_completed()
    {
        var p = new Payment { BookingId = 1, Amount = 100m };
        p.Status = PaymentStatus.Completed;
        p.Status.Should().Be(PaymentStatus.Completed);
    }
} 