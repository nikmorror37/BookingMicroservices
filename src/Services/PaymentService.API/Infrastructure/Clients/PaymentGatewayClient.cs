using PaymentService.API.Infrastructure.Clients;

namespace PaymentService.API.Infrastructure.Clients
{
    public class PaymentGatewayClient : IPaymentGatewayClient
    {
        // here we can call the real banking API. While just -->:
        public async Task<bool> RefundAsync(int paymentId, decimal amount)
        {
            // valid --> always successful
            await Task.Delay(100); 
            return true;
        }
    }
}
