using PaymentService.API.Infrastructure.Data;
using PaymentService.API.Infrastructure.Clients;
using PaymentService.API.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using BookingMicro.Contracts.Events;

namespace PaymentService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly PaymentDbContext _context;
        private readonly IPaymentGatewayClient _gateway; //Client for interaction with the bank
        private readonly IPublishEndpoint _publishEndpoint;

        public PaymentsController(
            PaymentDbContext context, 
            IPaymentGatewayClient gateway,
            IPublishEndpoint publishEndpoint)
        {
            _context = context;
            _gateway = gateway;
            _publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<Payment>>> GetAll()
        {
            return await _context.Payments.ToListAsync();
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Payment>> GetById(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null) return NotFound();
            return payment;
        }

        [HttpPost]
        public async Task<ActionResult<Payment>> Create([FromBody] Payment payment)
        {
            // Set initial status to Pending
            payment.Status = PaymentStatus.Pending;
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Process payment through gateway
            var success = await _gateway.ProcessPaymentAsync(payment.Id, payment.Amount);
            
            // Update payment status based on gateway response
            payment.Status = success ? PaymentStatus.Completed : PaymentStatus.RefundError;
            payment.PaidAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Publish payment created event
            await _publishEndpoint.Publish(new PaymentCreated
            {
                PaymentId = payment.Id,
                BookingId = payment.BookingId,
                Amount = payment.Amount,
                Status = (int)payment.Status
            });

            return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Payment payment)
        {
            if (id != payment.Id) return BadRequest();

            _context.Entry(payment).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Payments.AnyAsync(p => p.Id == id))
                    return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null) return NotFound();

            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST api/Payments/{id}/refund
        [HttpPost("{id}/refund")]
        [Authorize]  
        public async Task<IActionResult> Refund(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return NotFound();

            // allow return only for completed transactions
            if (payment.Status != PaymentStatus.Completed)
                return BadRequest("Only completed payments can be refunded.");

            // call gateway
            var ok = await _gateway.RefundAsync(payment.Id, payment.Amount);
            if (!ok)
            {
                payment.Status = PaymentStatus.RefundError;
                await _context.SaveChangesAsync();

                // publish failure event so BookingService can update its state
                await _publishEndpoint.Publish(new PaymentRefundFailed
                {
                    PaymentId = payment.Id,
                    BookingId = payment.BookingId,
                    Reason    = "Gateway refund failed",
                    FailedAt  = DateTime.UtcNow
                });

                return StatusCode(502, "Gateway refund failed");
            }

            // mark refund
            payment.Status      = PaymentStatus.Refunded;
            payment.RefundedAt  = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // publish success event
            await _publishEndpoint.Publish(new PaymentRefunded
            {
                PaymentId  = payment.Id,
                BookingId  = payment.BookingId,
                Amount     = payment.Amount,
                RefundedAt = payment.RefundedAt!.Value
            });

            // return the updated object
            return Ok(new
            {
                payment.Id,
                payment.Status,
                payment.RefundedAt
            });
        }

        [HttpPost("booking/{bookingId}/pay")]
        public async Task<IActionResult> PayBooking(int bookingId)
        {
            // Try to find an existing payment for this booking
            // Prefer a Pending one, otherwise take the latest of any status
            var payment = await _context.Payments
                .Where(p => p.BookingId == bookingId)
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();

            // If payment record has not yet been created by BookingCreatedConsumer (eventual consistency),
            // wait a moment for it to appear before giving up.
            if (payment == null)
            {
                for (int retry = 0; retry < 10 && payment == null; retry++)
                {
                    await Task.Delay(200);
                    payment = await _context.Payments
                        .Where(p => p.BookingId == bookingId)
                        .OrderByDescending(p => p.Id)
                        .FirstOrDefaultAsync();
                }
            }

            if (payment == null)
                return NotFound();

            // If payment already completed, simply acknowledge success
            if (payment.Status == PaymentStatus.Completed)
            {
                return Ok(new { payment.Id, payment.Status });
            }

            // For RefundError or Pending we re-attempt processing
            if (payment.Status is PaymentStatus.Pending or PaymentStatus.RefundError)
            {
                var ok = await _gateway.ProcessPaymentAsync(payment.Id, payment.Amount);
                payment.Status = ok ? PaymentStatus.Completed : PaymentStatus.RefundError;
                payment.PaidAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await _publishEndpoint.Publish(new PaymentCreated
                {
                    PaymentId = payment.Id,
                    BookingId = payment.BookingId,
                    Amount    = payment.Amount,
                    Status    = (int)payment.Status
                });

                return Ok(new { payment.Id, payment.Status });
            }

            // Other statuses (Refunded etc.) cannot be paid again
            return BadRequest($"Cannot pay booking with payment status {payment.Status}.");
        }
    }
}
