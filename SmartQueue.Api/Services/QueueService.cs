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
                    AverageServiceTimeMinutes = q.AverageServiceTimeMinutes,
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
                    AverageServiceTimeMinutes = q.AverageServiceTimeMinutes,
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
                AverageServiceTimeMinutes = model.AverageServiceTimeMinutes,
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
                AverageServiceTimeMinutes = queue.AverageServiceTimeMinutes,
                CreatedOn = queue.CreatedOn
            };
        }

        public async Task<QueueTicketResponseDto> JoinQueueAsync(int id, JoinQueueRequestDto model)
        {
            var queue = await dbContext.Queues
                .FirstOrDefaultAsync(q => q.Id == id && q.IsActive);

            if (queue == null)
            {
                throw new ArgumentException("Queue not found or inactive.");
            }

            var lastNumber = await dbContext.QueueTickets
                .Where(t => t.QueueId == id)
                .OrderByDescending(t => t.Number)
                .Select(t => t.Number)
                .FirstOrDefaultAsync();

            var nextNumber = lastNumber + 1;

            var priority = model.Priority?.ToUpper() == "VIP"
                ? QueuePriority.VIP
                : QueuePriority.Normal;

            int peopleAhead;

            if (priority == QueuePriority.VIP)
            {
                peopleAhead = await dbContext.QueueTickets
                    .CountAsync(t => t.QueueId == id
                        && t.Status == QueueStatus.Waiting
                        && t.Priority == QueuePriority.VIP);
            }
            else
            {
                peopleAhead = await dbContext.QueueTickets
                    .CountAsync(t => t.QueueId == id
                        && t.Status == QueueStatus.Waiting);
            }

            var estimatedWait = peopleAhead * queue.AverageServiceTimeMinutes;

            var ticket = new QueueTicket
            {
                CustomerName = model.CustomerName,
                Number = nextNumber,
                Status = QueueStatus.Waiting,
                Priority = priority,
                QueueId = id,
                CreatedOn = DateTime.UtcNow,
                EstimatedWaitTimeMinutes = estimatedWait
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
                CreatedOn = ticket.CreatedOn,
                EstimatedWaitTimeMinutes = ticket.EstimatedWaitTimeMinutes
            };
        }

        public async Task<IEnumerable<QueueTicketListItemDto>> GetTicketsAsync(int id)
        {
            return await dbContext.QueueTickets
                .Where(t => t.QueueId == id)
                .OrderBy(t => t.Priority == QueuePriority.VIP ? 0 : 1)
                .ThenBy(t => t.Number)
                .Select(t => new QueueTicketListItemDto
                {
                    Id = t.Id,
                    CustomerName = t.CustomerName,
                    Number = t.Number,
                    Status = t.Status.ToString(),
                    Priority = t.Priority.ToString(),
                    EstimatedWaitTimeMinutes = t.EstimatedWaitTimeMinutes,
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

            await RecalculateEstimatedWaitTimesAsync(id);

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
        private async Task RecalculateEstimatedWaitTimesAsync(int queueId)
        {
            var queue = await dbContext.Queues.FirstOrDefaultAsync(q => q.Id == queueId);

            if (queue == null)
            {
                return;
            }

            var waitingTickets = await dbContext.QueueTickets
                .Where(t => t.QueueId == queueId && t.Status == QueueStatus.Waiting)
                .OrderBy(t => t.Priority == QueuePriority.VIP ? 0 : 1)
                .ThenBy(t => t.Number)
                .ToListAsync();

            for (int i = 0; i < waitingTickets.Count; i++)
            {
                waitingTickets[i].EstimatedWaitTimeMinutes = i * queue.AverageServiceTimeMinutes;
            }

            await dbContext.SaveChangesAsync();
        }
        public async Task<QueueStatisticsDto> GetStatisticsAsync()
        {
            var today = DateTime.UtcNow.Date;

            var totalTickets = await dbContext.QueueTickets.CountAsync();

            var waitingTickets = await dbContext.QueueTickets
                .CountAsync(t => t.Status == QueueStatus.Waiting);

            var calledTickets = await dbContext.QueueTickets
                .CountAsync(t => t.Status == QueueStatus.Called);

            var servedTickets = await dbContext.QueueTickets
                .CountAsync(t => t.Status == QueueStatus.Served);

            var averageWaitTimeMinutes = await dbContext.QueueTickets
                .Where(t => t.Status == QueueStatus.Served && t.ServedOn.HasValue)
                .Select(t => EF.Functions.DateDiffMinute(t.CreatedOn, t.ServedOn!.Value))
                .DefaultIfEmpty(0)
                .AverageAsync();

            var mostRequestedQueueName = await dbContext.QueueTickets
                .Include(t => t.Queue)
                .GroupBy(t => t.Queue.Name)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync();

            var ticketsCreatedToday = await dbContext.QueueTickets
                .CountAsync(t => t.CreatedOn >= today);

            var ticketsCalledToday = await dbContext.QueueTickets
                .CountAsync(t => t.CalledOn.HasValue && t.CalledOn.Value >= today);

            var ticketsServedToday = await dbContext.QueueTickets
                .CountAsync(t => t.ServedOn.HasValue && t.ServedOn.Value >= today);

            var topQueues = await dbContext.Queues
                .Select(q => new TopQueueStatsDto
                {
                    QueueId = q.Id,
                    QueueName = q.Name,
                    TotalTickets = q.Tickets.Count(),
                    WaitingTickets = q.Tickets.Count(t => t.Status == QueueStatus.Waiting),
                    ServedTickets = q.Tickets.Count(t => t.Status == QueueStatus.Served),
                    AverageServiceTimeMinutes = q.AverageServiceTimeMinutes
                })
                .OrderByDescending(q => q.TotalTickets)
                .ThenBy(q => q.QueueName)
                .Take(5)
                .ToListAsync();

            return new QueueStatisticsDto
            {
                Overview = new QueueOverviewStatsDto
                {
                    TotalTickets = totalTickets,
                    WaitingTickets = waitingTickets,
                    CalledTickets = calledTickets,
                    ServedTickets = servedTickets,
                    AverageWaitTimeMinutes = averageWaitTimeMinutes,
                    MostRequestedQueueName = mostRequestedQueueName
                },
                Today = new TodayQueueStatsDto
                {
                    TicketsCreatedToday = ticketsCreatedToday,
                    TicketsCalledToday = ticketsCalledToday,
                    TicketsServedToday = ticketsServedToday
                },
                TopQueues = topQueues
            };
        }
        public async Task<AdminSummaryDto> GetAdminSummaryAsync()
        {
            var totalQueues = await dbContext.Queues.CountAsync();

            var activeQueues = await dbContext.Queues
                .CountAsync(q => q.IsActive);

            var totalTickets = await dbContext.QueueTickets.CountAsync();

            var waitingTickets = await dbContext.QueueTickets
                .CountAsync(t => t.Status == QueueStatus.Waiting);

            var averageWaitTimeMinutes = await dbContext.QueueTickets
                .Where(t => t.Status == QueueStatus.Served && t.ServedOn.HasValue)
                .Select(t => EF.Functions.DateDiffMinute(t.CreatedOn, t.ServedOn!.Value))
                .DefaultIfEmpty(0)
                .AverageAsync();

            var queueLoads = await dbContext.Queues
                .Select(q => new QueueLoadDto
                {
                    QueueId = q.Id,
                    QueueName = q.Name,
                    WaitingTickets = q.Tickets.Count(t => t.Status == QueueStatus.Waiting),
                    CalledTickets = q.Tickets.Count(t => t.Status == QueueStatus.Called),
                    ServedTickets = q.Tickets.Count(t => t.Status == QueueStatus.Served),
                    IsActive = q.IsActive
                })
                .OrderByDescending(q => q.WaitingTickets)
                .ThenBy(q => q.QueueName)
                .ToListAsync();

            var recentTickets = await dbContext.QueueTickets
                .Include(t => t.Queue)
                .OrderByDescending(t => t.CreatedOn)
                .Take(10)
                .Select(t => new RecentTicketDto
                {
                    TicketId = t.Id,
                    CustomerName = t.CustomerName,
                    Number = t.Number,
                    QueueName = t.Queue.Name,
                    Status = t.Status.ToString(),
                    Priority = t.Priority.ToString(),
                    CreatedOn = t.CreatedOn,
                    EstimatedWaitTimeMinutes = t.EstimatedWaitTimeMinutes
                })
                .ToListAsync();

            return new AdminSummaryDto
            {
                TotalQueues = totalQueues,
                ActiveQueues = activeQueues,
                TotalTickets = totalTickets,
                WaitingTickets = waitingTickets,
                AverageWaitTimeMinutes = averageWaitTimeMinutes,
                QueueLoads = queueLoads,
                RecentTickets = recentTickets
            };
        }
    }
}