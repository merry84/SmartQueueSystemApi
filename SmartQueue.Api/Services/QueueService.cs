using Microsoft.EntityFrameworkCore;
using SmartQueue.Api.Data;
using SmartQueue.Api.DTOs;
using SmartQueue.Api.Models;
using SmartQueue.Api.Services.Contracts;
using SmartQueue.Api.Enums;

namespace SmartQueue.Api.Services
{
    public class QueueService : IQueueService
    {
        private readonly SmartQueueDbContext dbContext;

        public QueueService(SmartQueueDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IEnumerable<QueueResponseDto>> GetAllAsync()
        {
            return await dbContext.Queues
                .Select(q => new QueueResponseDto
                {
                    Id = q.Id,
                    Name = q.Name,
                    Description = q.Description,
                    IsActive = q.IsActive,
                    CreatedOn = q.CreatedOn
                })
                .ToListAsync();
        }

        public async Task<QueueResponseDto?> GetByIdAsync(int id)
        {
            return await dbContext.Queues
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
        }

        public async Task<QueueResponseDto> CreateAsync(CreateQueueRequestDto model)
        {
            var queue = new Queue
            {
                Name = model.Name,
                Description = model.Description,
                IsActive = true,
                CreatedOn = DateTime.UtcNow
            };

            await dbContext.Queues.AddAsync(queue);
            await dbContext.SaveChangesAsync();

            return new QueueResponseDto
            {
                Id = queue.Id,
                Name = queue.Name,
                Description = queue.Description,
                IsActive = queue.IsActive,
                CreatedOn = queue.CreatedOn
            };
        }

        public async Task<QueueTicketResponseDto> JoinQueueAsync(int id, JoinQueueRequestDto model)
        {
            var lastNumber = await dbContext.QueueTickets
                .Where(t => t.QueueId == id)
                .OrderByDescending(t => t.Number)
                .Select(t => t.Number)
                .FirstOrDefaultAsync();

            var nextNumber = lastNumber + 1;

            var priority = model.Priority?.ToUpper() == "VIP"
                ? QueuePriority.VIP
                : QueuePriority.Normal;

            var ticket = new QueueTicket
            {
                CustomerName = model.CustomerName,
                Number = nextNumber,
                Status = QueueStatus.Waiting,
                Priority = priority,
                QueueId = id,
                CreatedOn = DateTime.UtcNow
            };

            await dbContext.QueueTickets.AddAsync(ticket);
            await dbContext.SaveChangesAsync();

            return new QueueTicketResponseDto
            {
                Id = ticket.Id,
                CustomerName = ticket.CustomerName,
                Number = ticket.Number,
                Status = ticket.Status.ToString(),
                Priority = ticket.Priority.ToString(),
                CreatedOn = ticket.CreatedOn
            };
        }

        public async Task<IEnumerable<QueueTicketListItemDto>> GetTicketsAsync(int id)
        {
            return await dbContext.QueueTickets
                .Where(t => t.QueueId == id)
                .OrderBy(t => t.Number)
                .Select(t => new QueueTicketListItemDto
                {
                    Id = t.Id,
                    CustomerName = t.CustomerName,
                    Number = t.Number,
                    Status = t.Status.ToString(),
                    Priority = t.Priority.ToString(),
                    CreatedOn = t.CreatedOn,
                    CalledOn = t.CalledOn
                })
                .ToListAsync();
        }

        public async Task<NextTicketResponseDto?> CallNextAsync(int id)
        {
            var nextTicket = await dbContext.QueueTickets
                .Where(t => t.QueueId == id && t.Status == QueueStatus.Waiting)
                .OrderBy(t => t.Priority == QueuePriority.VIP ? 0 : 1)
                .ThenBy(t => t.Number)
                .FirstOrDefaultAsync();

            if (nextTicket == null)
            {
                return null;
            }

            nextTicket.Status = QueueStatus.Called;
            nextTicket.CalledOn = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            return new NextTicketResponseDto
            {
                Id = nextTicket.Id,
                CustomerName = nextTicket.CustomerName,
                Number = nextTicket.Number,
                Status = nextTicket.Status.ToString(),
                Priority = nextTicket.Priority.ToString(),
                CreatedOn = nextTicket.CreatedOn,
                CalledOn = nextTicket.CalledOn
            };
        }
    }
}