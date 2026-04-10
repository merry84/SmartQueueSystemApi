using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartQueue.Api.Data;
using SmartQueue.Api.DTOs;
using SmartQueue.Api.Models;

namespace SmartQueue.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QueuesController : ControllerBase
    {
        private readonly SmartQueueDbContext dbContext;

        public QueuesController(SmartQueueDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<QueueResponseDto>>> GetAll()
        {
            var queues = await dbContext.Queues
                .Select(q => new QueueResponseDto
                {
                    Id = q.Id,
                    Name = q.Name,
                    Description = q.Description,
                    IsActive = q.IsActive,
                    CreatedOn = q.CreatedOn
                })
                .ToListAsync();

            return Ok(queues);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<QueueResponseDto>> GetById(int id)
        {
            var queue = await dbContext.Queues
                .Where(q => q.Id == id)
                .Select(q => new QueueResponseDto
                {
                    Id = q.Id,
                    Name = q.Name,
                    Description = q.Description,
                    IsActive = q.IsActive,
                    CreatedOn = q.CreatedOn
                })
                .FirstOrDefaultAsync();

            if (queue == null)
            {
                return NotFound();
            }

            return Ok(queue);
        }

        [HttpPost]
        public async Task<ActionResult<QueueResponseDto>> Create(CreateQueueRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var queue = new Queue
            {
                Name = model.Name,
                Description = model.Description,
                IsActive = true,
                CreatedOn = DateTime.UtcNow
            };

            await dbContext.Queues.AddAsync(queue);
            await dbContext.SaveChangesAsync();

            var response = new QueueResponseDto
            {
                Id = queue.Id,
                Name = queue.Name,
                Description = queue.Description,
                IsActive = queue.IsActive,
                CreatedOn = queue.CreatedOn
            };

            return CreatedAtAction(nameof(GetById), new { id = queue.Id }, response);
        }

        [HttpPost("{id}/join")]
        public async Task<ActionResult<QueueTicketResponseDto>> JoinQueue(int id, JoinQueueRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var queue = await dbContext.Queues.FindAsync(id);

            if (queue == null)
            {
                return NotFound("Queue not found");
            }

            var lastNumber = await dbContext.QueueTickets
                .Where(t => t.QueueId == id)
                .OrderByDescending(t => t.Number)
                .Select(t => t.Number)
                .FirstOrDefaultAsync();

            var nextNumber = lastNumber + 1;

            var ticket = new QueueTicket
            {
                CustomerName = model.CustomerName,
                Number = nextNumber,
                Status = "Waiting",
                Priority = string.IsNullOrWhiteSpace(model.Priority) ? "Normal" : model.Priority,
                QueueId = id,
                CreatedOn = DateTime.UtcNow
            };

            await dbContext.QueueTickets.AddAsync(ticket);
            await dbContext.SaveChangesAsync();

            var response = new QueueTicketResponseDto
            {
                Id = ticket.Id,
                CustomerName = ticket.CustomerName,
                Number = ticket.Number,
                Status = ticket.Status,
                Priority = ticket.Priority,
                CreatedOn = ticket.CreatedOn
            };

            return Ok(response);
        }
        [HttpPost("{id}/next")]
        public async Task<ActionResult<NextTicketResponseDto>> CallNext(int id)
        {
            var queue = await dbContext.Queues.FindAsync(id);

            if (queue == null)
            {
                return NotFound("Queue not found");
            }

            var nextTicket = await dbContext.QueueTickets
                .Where(t => t.QueueId == id && t.Status == "Waiting")
                .OrderBy(t => t.Priority == "VIP" ? 0 : 1)
                .ThenBy(t => t.Number)
                .FirstOrDefaultAsync();

            if (nextTicket == null)
            {
                return NotFound("No waiting tickets in this queue");
            }

            nextTicket.Status = "Called";
            nextTicket.CalledOn = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            var response = new NextTicketResponseDto
            {
                Id = nextTicket.Id,
                CustomerName = nextTicket.CustomerName,
                Number = nextTicket.Number,
                Status = nextTicket.Status,
                Priority = nextTicket.Priority,
                CreatedOn = nextTicket.CreatedOn,
                CalledOn = nextTicket.CalledOn
            };

            return Ok(response);
        }
        [HttpGet("{id}/tickets")]
        public async Task<ActionResult<IEnumerable<QueueTicketListItemDto>>> GetQueueTickets(int id)
        {
            var queueExists = await dbContext.Queues.AnyAsync(q => q.Id == id);

            if (!queueExists)
            {
                return NotFound("Queue not found");
            }

            var tickets = await dbContext.QueueTickets
                .Where(t => t.QueueId == id)
                .OrderBy(t => t.Number)
                .Select(t => new QueueTicketListItemDto
                {
                    Id = t.Id,
                    CustomerName = t.CustomerName,
                    Number = t.Number,
                    Status = t.Status,
                    Priority = t.Priority,
                    CreatedOn = t.CreatedOn,
                    CalledOn = t.CalledOn
                })
                .ToListAsync();

            return Ok(tickets);
        }
    }
}