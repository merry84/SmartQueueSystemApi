using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartQueue.Api.Data;
using SmartQueue.Api.Enums;
using SmartQueue.Api.ViewModels.Dashboard;

namespace SmartQueue.Api.Controllers
{
    public class DashboardController : Controller
    {
        private readonly SmartQueueDbContext dbContext;

        public DashboardController(SmartQueueDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var today = DateTime.UtcNow.Date;

            var waitTimes = await dbContext.QueueTickets
                .Where(t => t.Status == QueueStatus.Served && t.ServedOn.HasValue)
                .Select(t => EF.Functions.DateDiffMinute(t.CreatedOn, t.ServedOn!.Value))
                .ToListAsync();

            var averageWaitTimeMinutes = waitTimes.Any()
                ? waitTimes.Average()
                : 0;

            var mostRequestedQueueName = await dbContext.QueueTickets
                .Include(t => t.Queue)
                .GroupBy(t => t.Queue.Name)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync() ?? "N/A";

            var topQueues = await dbContext.Queues
                .Select(q => new DashboardQueueCardViewModel
                {
                    QueueId = q.Id,
                    QueueName = q.Name,
                    WaitingTickets = q.Tickets.Count(t => t.Status == QueueStatus.Waiting),
                    CalledTickets = q.Tickets.Count(t => t.Status == QueueStatus.Called),
                    ServedTickets = q.Tickets.Count(t => t.Status == QueueStatus.Served),
                    AverageServiceTimeMinutes = q.AverageServiceTimeMinutes,
                    IsActive = q.IsActive
                })
                .OrderByDescending(q => q.WaitingTickets)
                .ThenBy(q => q.QueueName)
                .Take(6)
                .ToListAsync();

            var recentTickets = await dbContext.QueueTickets
                .Include(t => t.Queue)
                .OrderByDescending(t => t.CreatedOn)
                .Take(10)
                .Select(t => new DashboardRecentTicketViewModel
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

            var totalQueues = await dbContext.Queues.CountAsync();
            var activeQueues = await dbContext.Queues.CountAsync(q => q.IsActive);
            var totalTickets = await dbContext.QueueTickets.CountAsync();
            var waitingTickets = await dbContext.QueueTickets.CountAsync(t => t.Status == QueueStatus.Waiting);
            var calledTickets = await dbContext.QueueTickets.CountAsync(t => t.Status == QueueStatus.Called);
            var servedTickets = await dbContext.QueueTickets.CountAsync(t => t.Status == QueueStatus.Served);

            var ticketsCreatedToday = await dbContext.QueueTickets
                .CountAsync(t => t.CreatedOn >= today);

            var ticketsCalledToday = await dbContext.QueueTickets
                .CountAsync(t => t.CalledOn.HasValue && t.CalledOn.Value >= today);

            var ticketsServedToday = await dbContext.QueueTickets
                .CountAsync(t => t.ServedOn.HasValue && t.ServedOn.Value >= today);

            var serviceRatePercent = ticketsCreatedToday > 0
                ? (double)ticketsServedToday / ticketsCreatedToday * 100
                : 0;
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.UtcNow.Date.AddDays(-i))
                .OrderBy(d => d)
                .ToList();

            var ticketsLast7Days = new List<int>();
            var labels = new List<string>();

            foreach (var day in last7Days)
            {
                var nextDay = day.AddDays(1);

                var count = await dbContext.QueueTickets
                    .CountAsync(t => t.CreatedOn >= day && t.CreatedOn < nextDay);

                ticketsLast7Days.Add(count);
                labels.Add(day.ToString("dd MMM"));
            }

            var queueStats = await dbContext.QueueTickets
                .Include(t => t.Queue)
                .GroupBy(t => t.Queue.Name)
                .Select(g => new
                {
                    Name = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();
            var model = new DashboardIndexViewModel
            {
                TotalQueues = totalQueues,
                ActiveQueues = activeQueues,
                TotalTickets = totalTickets,
                WaitingTickets = waitingTickets,
                CalledTickets = calledTickets,
                ServedTickets = servedTickets,
                AverageWaitTimeMinutes = averageWaitTimeMinutes,
                MostRequestedQueueName = mostRequestedQueueName,
                TicketsCreatedToday = ticketsCreatedToday,
                TicketsCalledToday = ticketsCalledToday,
                TicketsServedToday = ticketsServedToday,
                ServiceRatePercent = serviceRatePercent,
                TicketsLast7Days = ticketsLast7Days,
                Last7DaysLabels = labels,
                QueueNames = queueStats.Select(x => x.Name).ToList(),
                TicketsPerQueue = queueStats.Select(x => x.Count).ToList(),
                TopQueues = topQueues,
                RecentTickets = recentTickets
            };

            return View(model);
        }
    }
}