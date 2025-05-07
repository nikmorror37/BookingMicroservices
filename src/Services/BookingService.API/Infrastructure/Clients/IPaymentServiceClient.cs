using System.Threading.Tasks;
using BookingService.API.Infrastructure.Clients;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace BookingService.API.Infrastructure.Clients
{
    public interface IPaymentServiceClient
    {
        Task<bool> RefundAsync(int paymentId);
    }
}
