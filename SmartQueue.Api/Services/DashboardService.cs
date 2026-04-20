using Microsoft.EntityFrameworkCore;
using SmartQueue.Api.Data;
using SmartQueue.Api.DTOs;
using SmartQueue.Api.Enums;
using SmartQueue.Api.Services.Contracts;


namespace SmartQueue.Api.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly SmartQueueDbContext dbContext;

        public DashboardService(SmartQueueDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<QueueStatisticsDto> GetStatisticsAsync()
        {
            var today = DateTime.UtcNow.Date;

            var totalTickets = await dbContext.QueueTickets.CountAsync();

            var waitingTickets = await dbContext.QueueTickets
                .CountAsync(t => t.Status == TicketStatus.Waiting);

            var calledTickets = await dbContext.QueueTickets
                .CountAsync(t => t.Status == TicketStatus.Called);

            var servedTickets = await dbContext.QueueTickets
                .CountAsync(t => t.Status == TicketStatus.Served);

            var averageWaitTimeMinutes = await dbContext.QueueTickets
                .Where(t => t.Status ==TicketStatus.Served && t.ServedAt.HasValue)
                .Select(t => EF.Functions.DateDiffMinute(t.JoinedAt, t.ServedAt!.Value))
                .DefaultIfEmpty(0)
                .AverageAsync();


            var mostRequestedQueueName = await dbContext.QueueTickets
                .Include(t => t.Queue)
                .GroupBy(t => t.Queue.Name)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync();

            var ticketsCreatedToday = await dbContext.QueueTickets
                .CountAsync(t => t.JoinedAt >= today);

            var ticketsCalledToday = await dbContext.QueueTickets
                .CountAsync(t => t.CalledAt.HasValue && t.CalledAt.Value >= today);

            var ticketsServedToday = await dbContext.QueueTickets
                .CountAsync(t => t.ServedAt.HasValue && t.ServedAt.Value >= today);

            var topQueues = await dbContext.Queues
                .Select(q => new TopQueueStatsDto
                {
                    QueueId = q.Id,
                    QueueName = q.Name,
                    TotalTickets = q.Tickets.Count(),
                    WaitingTickets = q.Tickets.Count(t => t.Status == TicketStatus.Waiting),
                    ServedTickets = q.Tickets.Count(t => t.Status == TicketStatus.Served),
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
                .CountAsync(t => t.Status == TicketStatus.Waiting);

            var averageWaitTimeMinutes = await dbContext.QueueTickets
                .Where(t => t.Status == TicketStatus.Served && t.ServedAt.HasValue)
                .Select(t => EF.Functions.DateDiffMinute(t.JoinedAt, t.ServedAt!.Value))
                .DefaultIfEmpty(0)
                .AverageAsync();

            var queueLoads = await dbContext.Queues
                .Select(q => new QueueLoadDto
                {
                    QueueId = q.Id,
                    QueueName = q.Name,
                    WaitingTickets = q.Tickets.Count(t => t.Status == TicketStatus.Waiting),
                    CalledTickets = q.Tickets.Count(t => t.Status == TicketStatus.Called),
                    ServedTickets = q.Tickets.Count(t => t.Status == TicketStatus.Served),
                    IsActive = q.IsActive
                })
                .OrderByDescending(q => q.WaitingTickets)
                .ThenBy(q => q.QueueName)
                .ToListAsync();

                 var recentTickets = await dbContext.QueueTickets
                .Include(t => t.Queue)
                .OrderByDescending(t => t.JoinedAt)
                .Take(10)
                .Select(t => new RecentTicketDto
                {
                    TicketId = t.Id,
                    CustomerName = t.CustomerName,
                    Number = t.Number,
                    QueueName = t.Queue.Name,
                    Status = t.Status.ToString(),
                    Priority = t.Priority.ToString(),
                    CreatedOn = t.JoinedAt,
                    EstimatedWaitTimeMinutes = t.ServedAt.HasValue
                    ? EF.Functions.DateDiffMinute(t.JoinedAt, t.ServedAt.Value)
                    : 0
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