using PaymentService.API.Infrastructure.Data;
using PaymentService.API.Infrastructure.Clients;
using PaymentService.API.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PaymentService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly PaymentDbContext _context;
        private readonly IPaymentGatewayClient _gateway; //Client for interaction with the bank

        public PaymentsController(PaymentDbContext context, IPaymentGatewayClient gateway)
        {
            _context = context;
            _gateway = gateway;
        }

        [HttpGet]
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
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            // вернём 201 Created + ссылку на GET
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
                return StatusCode(502, "Gateway refund failed");
            }

            // mark refund
            payment.Status      = PaymentStatus.Refunded;
            payment.RefundedAt  = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // return the updated object
            return Ok(new
            {
                payment.Id,
                payment.Status,
                payment.RefundedAt
            });
        }
    }
}
