using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartQueue.Api.Data;
using SmartQueue.Api.Enums;
using SmartQueue.Api.ViewModels.Tickets;
using System.Security.Claims;

namespace SmartQueue.Api.Controllers
{
    [Authorize]
    public class MyTicketsController : Controller
    {
        private readonly SmartQueueDbContext dbContext;

        public MyTicketsController(SmartQueueDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userTickets = await dbContext.QueueTickets
                .Include(t => t.Queue)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.JoinedAt)
                .ToListAsync();

            var result = new List<MyTicketViewModel>();

            foreach (var ticket in userTickets)
            {
                var peopleAhead = 0;

                if (ticket.Status == TicketStatus.Waiting)
                {
                    peopleAhead = await dbContext.QueueTickets
                        .CountAsync(t =>
                            t.QueueId == ticket.QueueId &&
                            t.Status == TicketStatus.Waiting &&
                            (
                                t.Priority == QueuePriority.VIP && ticket.Priority != QueuePriority.VIP ||
                                t.Priority == ticket.Priority && t.Number < ticket.Number
                            ));
                }

                var estimatedWaitTimeMinutes = peopleAhead * ticket.Queue.AverageServiceTimeMinutes;

                result.Add(new MyTicketViewModel
                {
                    TicketId = ticket.Id,
                    Number = ticket.Number,
                    QueueName = ticket.Queue.Name,
                    CustomerName = ticket.CustomerName,
                    Status = ticket.Status.ToString(),
                    Priority = ticket.Priority.ToString(),
                    JoinedAt = ticket.JoinedAt,
                    PeopleAhead = peopleAhead,
                    EstimatedWaitTimeMinutes = estimatedWaitTimeMinutes
                });
            }

            return View(result);
        }
    }
}