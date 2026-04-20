using Microsoft.EntityFrameworkCore;
using SmartQueue.Api.Data;
using SmartQueue.Api.DTOs;
using SmartQueue.Api.Enums;
using SmartQueue.Api.Models;
using SmartQueue.Api.Services.Contracts;

namespace SmartQueue.Api.Services
{
    public class TicketService : ITicketService
    {
        private readonly SmartQueueDbContext dbContext;

        public TicketService(SmartQueueDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<QueueTicketResponseDto> JoinQueueAsync(int queueId, JoinQueueRequestDto model)
        {
            var queue = await dbContext.Queues
                .FirstOrDefaultAsync(q => q.Id == queueId && q.IsActive);

            if (queue == null)
            {
                throw new ArgumentException("Queue not found or inactive.");
            }

            var lastNumber = await dbContext.QueueTickets
                .Where(t => t.QueueId == queueId)
                .OrderByDescending(t => t.Number)
                .Select(t => t.Number)
                .FirstOrDefaultAsync();

            var nextNumber = lastNumber + 1;

            var priority = model.Priority?.ToUpper() == "VIP"
                ? QueuePriority.VIP
                : QueuePriority.Normal;

            var estimatedWaitTimeMinutes = await CalculateEstimatedWaitTimeAsync(queueId, priority, queue.AverageServiceTimeMinutes);

            var ticket = new QueueTicket
            {
                CustomerName = model.CustomerName,
                Number = nextNumber,
                Status = TicketStatus.Waiting,
                Priority = priority,
                QueueId = queueId,
                JoinedAt = DateTime.UtcNow
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
                CreatedOn = ticket.JoinedAt,
                EstimatedWaitTimeMinutes = estimatedWaitTimeMinutes
            };
        }

        public async Task<IEnumerable<QueueTicketListItemDto>> GetTicketsAsync(int queueId)
        {
            var queue = await dbContext.Queues
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == queueId);

            if (queue == null)
            {
                return Enumerable.Empty<QueueTicketListItemDto>();
            }

            var tickets = await dbContext.QueueTickets
                .Where(t => t.QueueId == queueId)
                .OrderBy(t => t.Priority == QueuePriority.VIP ? 0 : 1)
                .ThenBy(t => t.Number)
                .ToListAsync();

            var waitingTickets = tickets
                .Where(t => t.Status == TicketStatus.Waiting)
                .OrderBy(t => t.Priority == QueuePriority.VIP ? 0 : 1)
                .ThenBy(t => t.Number)
                .ToList();

            var waitMap = new Dictionary<int, int>();
            for (int i = 0; i < waitingTickets.Count; i++)
            {
                waitMap[waitingTickets[i].Id] = i * queue.AverageServiceTimeMinutes;
            }

            return tickets.Select(t => new QueueTicketListItemDto
            {
                Id = t.Id,
                CustomerName = t.CustomerName,
                Number = t.Number,
                Status = t.Status.ToString(),
                Priority = t.Priority.ToString(),
                EstimatedWaitTimeMinutes = waitMap.TryGetValue(t.Id, out var wait) ? wait : 0,
                CreatedOn = t.JoinedAt,
                CalledOn = t.CalledAt
            });
        }

        public async Task<NextTicketResponseDto?> CallNextAsync(int queueId)
        {
            var nextTicket = await dbContext.QueueTickets
                .Where(t => t.QueueId == queueId && t.Status == TicketStatus.Waiting)
                .OrderBy(t => t.Priority == QueuePriority.VIP ? 0 : 1)
                .ThenBy(t => t.Number)
                .FirstOrDefaultAsync();

            if (nextTicket == null)
            {
                return null;
            }

            nextTicket.Status = TicketStatus.Called;
            nextTicket.CalledAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            return new NextTicketResponseDto
            {
                Id = nextTicket.Id,
                CustomerName = nextTicket.CustomerName,
                Number = nextTicket.Number,
                Status = nextTicket.Status.ToString(),
                Priority = nextTicket.Priority.ToString(),
                CreatedOn = nextTicket.JoinedAt,
                CalledOn = nextTicket.CalledAt
            };
        }

        public async Task<NextTicketResponseDto?> ServeAsync(int ticketId)
        {
            var ticket = await dbContext.QueueTickets
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null)
            {
                return null;
            }

            if (ticket.Status != TicketStatus.Called && ticket.Status != TicketStatus.Serving)
            {
                throw new InvalidOperationException("Only called or serving tickets can be marked as served.");
            }

            ticket.Status = TicketStatus.Served;
            ticket.ServedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            return new NextTicketResponseDto
            {
                Id = ticket.Id,
                CustomerName = ticket.CustomerName,
                Number = ticket.Number,
                Status = ticket.Status.ToString(),
                Priority = ticket.Priority.ToString(),
                CreatedOn = ticket.JoinedAt,
                CalledOn = ticket.CalledAt
            };
        }

        private async Task<int> CalculateEstimatedWaitTimeAsync(int queueId, QueuePriority priority, int averageServiceTimeMinutes)
        {
            int peopleAhead;

            if (priority == QueuePriority.VIP)
            {
                peopleAhead = await dbContext.QueueTickets
                    .CountAsync(t => t.QueueId == queueId
                        && t.Status == TicketStatus.Waiting
                        && t.Priority == QueuePriority.VIP);
            }
            else
            {
                peopleAhead = await dbContext.QueueTickets
                    .CountAsync(t => t.QueueId == queueId
                        && t.Status == TicketStatus.Waiting);
            }

            return peopleAhead * averageServiceTimeMinutes;
        }
    }
}