using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartQueue.Api.Common;
using SmartQueue.Api.DTOs;
using SmartQueue.Api.Services.Contracts;

namespace SmartQueue.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class QueuesController : ControllerBase
    {
        private readonly IQueueService queueService;
        private readonly ITicketService ticketService;
        private readonly IDashboardService dashboardService;

        public QueuesController(
            IQueueService queueService,
            ITicketService ticketService,
            IDashboardService dashboardService)
        {
            this.queueService = queueService;
            this.ticketService = ticketService;
            this.dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<QueueResponseDto>>> GetAll()
        {
            var result = await queueService.GetAllAsync();

            return Ok(ApiResponse<IEnumerable<QueueResponseDto>>
                .SuccessResponse(result, "Queues retrieved successfully"));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<QueueResponseDto>> GetById(int id)
        {
            var queue = await queueService.GetByIdAsync(id);

            if (queue == null)
            {
                return NotFound();
            }

            return Ok(ApiResponse<QueueResponseDto>
                .SuccessResponse(queue, "Queue retrieved successfully"));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<QueueResponseDto>> Create(CreateQueueRequestDto model)
        {
            var result = await queueService.CreateAsync(model);

            return Ok(ApiResponse<QueueResponseDto>
                .SuccessResponse(result, "Queue created successfully"));
        }

        [AllowAnonymous]
        [HttpPost("{id}/join")]
        public async Task<ActionResult<QueueTicketResponseDto>> JoinQueue(int id, JoinQueueRequestDto model)
        {
            var result = await ticketService.JoinQueueAsync(id, model);

            return Ok(ApiResponse<QueueTicketResponseDto>
                .SuccessResponse(result, "Ticket created successfully"));
        }

        [Authorize(Roles = "Operator")]
        [HttpPost("{id}/next")]
        public async Task<ActionResult<NextTicketResponseDto>> CallNext(int id)
        {
            var result = await ticketService.CallNextAsync(id);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(ApiResponse<NextTicketResponseDto>
                .SuccessResponse(result, "Next ticket called"));
        }

        [HttpGet("{id}/tickets")]
        public async Task<ActionResult<IEnumerable<QueueTicketListItemDto>>> GetTickets(int id)
        {
            var result = await ticketService.GetTicketsAsync(id);

            return Ok(ApiResponse<IEnumerable<QueueTicketListItemDto>>
                .SuccessResponse(result, "Tickets retrieved successfully"));
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<QueueStatisticsDto>> GetStatistics()
        {
            var result = await dashboardService.GetStatisticsAsync();

            return Ok(ApiResponse<QueueStatisticsDto>
                .SuccessResponse(result, "Statistics retrieved successfully"));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin-summary")]
        public async Task<ActionResult<AdminSummaryDto>> GetAdminSummary()
        {
            var result = await dashboardService.GetAdminSummaryAsync();

            return Ok(ApiResponse<AdminSummaryDto>
                .SuccessResponse(result, "Admin summary retrieved successfully"));
        }
    }
}