using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartQueue.Api.Data;
using SmartQueue.Api.DTOs;

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

        [HttpPost("{id}/serve")]
        public async Task<ActionResult<NextTicketResponseDto>> ServeTicket(int id)
        {
            var ticket = await dbContext.QueueTickets
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
            {
                return NotFound("Ticket not found");
            }

            if (ticket.Status != "Called")
            {
                return BadRequest("Only called tickets can be marked as served");
            }

            ticket.Status = "Served";

            await dbContext.SaveChangesAsync();

            var response = new NextTicketResponseDto
            {
                Id = ticket.Id,
                CustomerName = ticket.CustomerName,
                Number = ticket.Number,
                Status = ticket.Status,
                Priority = ticket.Priority,
                CreatedOn = ticket.CreatedOn,
                CalledOn = ticket.CalledOn
            };

            return Ok(response);
        }
    }
}