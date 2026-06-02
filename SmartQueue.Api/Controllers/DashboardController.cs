using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SmartQueue.Api.Common;
using SmartQueue.Api.Data;
using SmartQueue.Api.DTOs;
using SmartQueue.Api.Enums;
using SmartQueue.Api.Hubs;
using SmartQueue.Api.Models;
using SmartQueue.Api.Services.Contracts;
using SmartQueue.Api.ViewModels.Dashboard.Forms;
using System.Net.Sockets;
using System.Security.Claims;

namespace SmartQueue.Api.Controllers
{
    public class DashboardController : Controller
    {
       
        private readonly IHubContext<QueueHub> hubContext;
        private readonly IDashboardService dashboardService;
        private readonly IQueueService queueService;
        private readonly ITicketService ticketService;
        private readonly SmartQueueDbContext dbContext;
        public DashboardController(
            SmartQueueDbContext dbContext,
            IDashboardService dashboardService,
            IQueueService queueService,
            ITicketService ticketService,
            IHubContext<QueueHub> hubContext)
        {
            this.dbContext = dbContext;
            this.dashboardService = dashboardService;
            this.queueService = queueService;
            this.ticketService = ticketService;
            this.hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = await dashboardService.GetDashboardDataAsync();
            return View(model);
        
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.Admin)]
        public IActionResult CreateQueue()
        {
            return View(new CreateQueueFormViewModel());
        }

        [Authorize(Roles = RoleNames.Admin)]
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

            await queueService.CreateAsync(new CreateQueueRequestDto
            {
                Name = model.Name,
                Description = model.Description,
                AverageServiceTimeMinutes = model.AverageServiceTimeMinutes
            });
            await hubContext.Clients.All.SendAsync("QueueUpdated");

            TempData[NotificationConstants.SuccessMessage] = "Queue created successfully.";
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

            var userId = User.Identity?.IsAuthenticated == true
                ? User.FindFirstValue(ClaimTypes.NameIdentifier)
                : null;

            await ticketService.JoinQueueAsync(model.QueueId, new JoinQueueRequestDto
            {
                CustomerName = model.CustomerName,
                Priority = model.Priority
            }, userId);

            await hubContext.Clients.All.SendAsync("QueueUpdated");

            TempData[NotificationConstants.SuccessMessage] = "Ticket created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Operator}")]
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

        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Operator}")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CallNext(CallNextFormViewModel model)
        {
            var result = await ticketService.CallNextAsync(model.QueueId);

            if (result == null)
            {
                ModelState.AddModelError(string.Empty, "No waiting tickets found.");
                return View(model);
            }

            await hubContext.Clients.All.SendAsync("QueueUpdated");

            TempData[NotificationConstants.SuccessMessage] = $"Ticket #{result.Number} was called.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Operator}")]
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
            var result = await ticketService.ServeAsync(model.TicketId);

            if (result == null)
            {
                return NotFound();
            }

            await hubContext.Clients.All.SendAsync("QueueUpdated");

            TempData[NotificationConstants.SuccessMessage] = $"Ticket #{result.Number} marked as served.";
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