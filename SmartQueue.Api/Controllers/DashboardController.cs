using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SmartQueue.Api.Data;
using SmartQueue.Api.Enums;
using SmartQueue.Api.Hubs;
using SmartQueue.Api.Models;
using SmartQueue.Api.Services.Contracts;
using SmartQueue.Api.ViewModels.Dashboard;
using SmartQueue.Api.ViewModels.Dashboard.Forms;
using System.Net.Sockets;

namespace SmartQueue.Api.Controllers
{
    public class DashboardController : Controller
    {
       
        private readonly IHubContext<QueueHub> hubContext;
        private readonly IDashboardService dashboardService;
        private readonly SmartQueueDbContext dbContext;
        public DashboardController(
                SmartQueueDbContext dbContext,
                IDashboardService dashboardService,
                IHubContext<QueueHub> hubContext)
        {
            this.dbContext = dbContext;
            this.dashboardService = dashboardService;
            this.hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = await dashboardService.GetDashboardDataAsync();
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