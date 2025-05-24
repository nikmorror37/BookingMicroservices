using PaymentService.API.Infrastructure.Clients;
using Microsoft.Extensions.Logging;

namespace PaymentService.API.Infrastructure.Clients
{
    public class PaymentGatewayClient : IPaymentGatewayClient
    {
        private readonly ILogger<PaymentGatewayClient> _logger;

        public PaymentGatewayClient(ILogger<PaymentGatewayClient> logger)
        {
            _logger = logger;
        }

        // Simulate payment processing with a delay
        public async Task<bool> ProcessPaymentAsync(int paymentId, decimal amount)
        {
            _logger.LogInformation("Processing payment {PaymentId} for amount {Amount}", paymentId, amount);
            
            // Simulate API call delay
            await Task.Delay(100);
            
            // For demo purposes, always return success for payments
            return true;
        }

        // Simulate refund processing with a delay
        public async Task<bool> RefundAsync(int paymentId, decimal amount)
        {
            _logger.LogInformation("Processing refund for payment {PaymentId} with amount {Amount}", paymentId, amount);
            
            // Simulate API call delay
            await Task.Delay(100);
            
            // For demo purposes, simulate a 20% chance of refund failure
            var random = new Random();
            var success = random.Next(100) < 80;
            
            if (!success)
            {
                _logger.LogWarning("Refund failed for payment {PaymentId}", paymentId);
            }
            
            return success;
        }
    }
}
