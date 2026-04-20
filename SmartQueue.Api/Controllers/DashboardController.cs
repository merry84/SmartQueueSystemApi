using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SmartQueue.Api.Data;
using SmartQueue.Api.Enums;
using SmartQueue.Api.Hubs;
using SmartQueue.Api.Models;
using SmartQueue.Api.ViewModels.Dashboard;
using SmartQueue.Api.ViewModels.Dashboard.Forms;
using System.Net.Sockets;

namespace SmartQueue.Api.Controllers
{
    public class DashboardController : Controller
    {
        private readonly SmartQueueDbContext dbContext;
        private readonly IHubContext<QueueHub> hubContext;

        public DashboardController(SmartQueueDbContext dbContext,IHubContext<QueueHub> hubContext)
        {
            this.dbContext = dbContext;
            this.hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var today = DateTime.UtcNow.Date;

            var waitTimes = await dbContext.QueueTickets
                 .Where(t => t.Status == TicketStatus.Served && t.ServedAt.HasValue)
                 .Select(t => EF.Functions.DateDiffMinute(t.JoinedAt, t.ServedAt.Value))
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
                    WaitingTickets = q.Tickets.Count(t => t.Status == TicketStatus.Waiting),
                    CalledTickets = q.Tickets.Count(t => t.Status == TicketStatus.Called),
                    ServedTickets = q.Tickets.Count(t => t.Status == TicketStatus.Served),
                    AverageServiceTimeMinutes = q.AverageServiceTimeMinutes,
                    IsActive = q.IsActive
                })
                .OrderByDescending(q => q.WaitingTickets)
                .ThenBy(q => q.QueueName)
                .Take(6)
                .ToListAsync();

            var recentTickets = await dbContext.QueueTickets
                .Include(t => t.Queue)
                .OrderByDescending(t => t.JoinedAt)
                .Take(10)
                .Select(t => new DashboardRecentTicketViewModel
                {
                    TicketId = t.Id,
                    QueueId = t.QueueId,
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

            var totalQueues = await dbContext.Queues.CountAsync();
            var activeQueues = await dbContext.Queues.CountAsync(q => q.IsActive);
            var totalTickets = await dbContext.QueueTickets.CountAsync();
            var waitingTickets = await dbContext.QueueTickets.CountAsync(t => t.Status == TicketStatus.Waiting);
            var calledTickets = await dbContext.QueueTickets.CountAsync(t => t.Status == TicketStatus.Called);
            var servedTickets = await dbContext.QueueTickets.CountAsync(t => t.Status == TicketStatus.Served);

            var ticketsCreatedToday = await dbContext.QueueTickets
                .CountAsync(t => t.JoinedAt >= today);

            var ticketsCalledToday = await dbContext.QueueTickets
                .CountAsync(t => t.CalledAt.HasValue && t.CalledAt.Value >= today);

            var ticketsServedToday = await dbContext.QueueTickets
                .CountAsync(t => t.ServedAt.HasValue && t.ServedAt.Value >= today);

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
                    .CountAsync(t => t.JoinedAt >= day && t.JoinedAt < nextDay);

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

        [HttpGet]
        public IActionResult CreateQueue()
        {
            return View(new CreateQueueFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQueue(CreateQueueFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var queue = new Queue
            {
                Name = model.Name,
                Description = model.Description,
                AverageServiceTimeMinutes = model.AverageServiceTimeMinutes,
                IsActive = true,
                CreatedOn = DateTime.UtcNow
            };

            await dbContext.Queues.AddAsync(queue);
            await dbContext.SaveChangesAsync();
            await hubContext.Clients.All.SendAsync("QueueUpdated");

            TempData["SuccessMessage"] = "Queue created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> JoinQueue(int? queueId)
        {
            var model = new JoinQueueFormViewModel
            {
                QueueId = queueId ?? 0,
                Queues = await GetQueueSelectItemsAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinQueue(JoinQueueFormViewModel model)
        {
            model.Queues = await GetQueueSelectItemsAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var queue = await dbContext.Queues
                .FirstOrDefaultAsync(q => q.Id == model.QueueId && q.IsActive);

            if (queue == null)
            {
                ModelState.AddModelError(nameof(model.QueueId), "Selected queue was not found.");
                return View(model);
            }

            var lastNumber = await dbContext.QueueTickets
                .Where(t => t.QueueId == model.QueueId)
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
                Status = TicketStatus.Waiting,
                Priority = priority,
                QueueId = model.QueueId,
                JoinedAt = DateTime.UtcNow
            };

            await dbContext.QueueTickets.AddAsync(ticket);
            await dbContext.SaveChangesAsync();
            await hubContext.Clients.All.SendAsync("QueueUpdated");

            TempData["SuccessMessage"] = $"Ticket #{ticket.Number} created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> CallNext(int? queueId)
        {
            var model = new CallNextFormViewModel
            {
                QueueId = queueId ?? 0,
                Queues = await GetQueueSelectItemsAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CallNext(CallNextFormViewModel model)
        {
            model.Queues = await GetQueueSelectItemsAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var nextTicket = await dbContext.QueueTickets
                .Where(t => t.QueueId == model.QueueId && t.Status == TicketStatus.Waiting)
                .OrderBy(t => t.Priority == QueuePriority.VIP ? 0 : 1)
                .ThenBy(t => t.Number)
                .FirstOrDefaultAsync();

            if (nextTicket == null)
            {
                ModelState.AddModelError(string.Empty, "No waiting tickets found for this queue.");
                return View(model);
            }

            nextTicket.Status = TicketStatus.Called;
            nextTicket.CalledAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            var queue = await dbContext.Queues.FirstOrDefaultAsync(q => q.Id == model.QueueId);

            await dbContext.SaveChangesAsync();
            await hubContext.Clients.All.SendAsync("QueueUpdated");
            TempData["SuccessMessage"] = $"Ticket #{nextTicket.Number} was called.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ServeTicket(int? ticketId)
        {
            var model = new ServeTicketFormViewModel
            {
                TicketId = ticketId ?? 0,
                Tickets = await GetCallableTicketSelectItemsAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ServeTicket(ServeTicketFormViewModel model)
        {
            var ticket = await dbContext.QueueTickets
                .FirstOrDefaultAsync(t => t.Id == model.TicketId);

            if (ticket == null)
            {
                return NotFound();
            }

            ticket.Status = TicketStatus.Served;
            ticket.ServedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();
            await hubContext.Clients.All.SendAsync("QueueUpdated");

            TempData["SuccessMessage"] = $"Ticket #{ticket.Number} marked as served.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<List<SelectListItem>> GetQueueSelectItemsAsync()
        {
            return await dbContext.Queues
                .Where(q => q.IsActive)
                .OrderBy(q => q.Name)
                .Select(q => new SelectListItem
                {
                    Value = q.Id.ToString(),
                    Text = q.Name
                })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> GetCallableTicketSelectItemsAsync()
        {
            return await dbContext.QueueTickets
                .Include(t => t.Queue)
                .Where(t => t.Status == TicketStatus.Called)
                .OrderBy(t => t.Queue.Name)
                .ThenBy(t => t.Number)
                .Select(t => new SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = $"{t.Queue.Name} - #{t.Number} - {t.CustomerName}"
                })
                .ToListAsync();
        }
    }
}