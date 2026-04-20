using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartQueue.Api.Common;
using SmartQueue.Api.DTOs;
using SmartQueue.Api.Services.Contracts;

namespace SmartQueue.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketService ticketService;

        public TicketsController(ITicketService ticketService)
        {
            this.ticketService = ticketService;
        }

        [Authorize(Roles = "Operator")]
        [HttpPost("{id}/serve")]
        public async Task<ActionResult<NextTicketResponseDto>> ServeTicket(int id)
        {
            var result = await ticketService.ServeAsync(id);

            if (result == null)
            {
                return NotFound("Ticket not found");
            }

            return Ok(ApiResponse<NextTicketResponseDto>
                .SuccessResponse(result, "Ticket served successfully"));
        }
    }
}