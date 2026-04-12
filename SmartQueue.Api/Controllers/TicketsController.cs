using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartQueue.Api.Data;
using SmartQueue.Api.DTOs;
using SmartQueue.Api.Enums;

namespace SmartQueue.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketsController : ControllerBase
    {
        private readonly SmartQueueDbContext dbContext;

        public TicketsController(SmartQueueDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [Authorize(Roles = "Operator")]
        [HttpPost("{id}/serve")]
        public async Task<ActionResult<NextTicketResponseDto>> ServeTicket(int id)
        {
            var ticket = await dbContext.QueueTickets
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
            {
                return NotFound("Ticket not found");
            }

            if (ticket.Status != QueueStatus.Called)
            {
                return BadRequest("Only called tickets can be marked as served");
            }

            ticket.Status = QueueStatus.Served;

            await dbContext.SaveChangesAsync();

            var response = new NextTicketResponseDto
            {
                Id = ticket.Id,
                CustomerName = ticket.CustomerName,
                Number = ticket.Number,
                Status = ticket.Status.ToString(),
                Priority = ticket.Priority.ToString(),
                CreatedOn = ticket.CreatedOn,
                CalledOn = ticket.CalledOn
            };

            return Ok(response);
        }
    }
}