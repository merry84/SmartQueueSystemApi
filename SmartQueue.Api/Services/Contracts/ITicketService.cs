using SmartQueue.Api.DTOs;

namespace SmartQueue.Api.Services.Contracts
{
    public interface ITicketService
    {
        Task<QueueTicketResponseDto> JoinQueueAsync(int queueId, JoinQueueRequestDto model);
        Task<IEnumerable<QueueTicketListItemDto>> GetTicketsAsync(int queueId);
        Task<NextTicketResponseDto?> CallNextAsync(int queueId);
        Task<NextTicketResponseDto?> ServeAsync(int ticketId);
    }
}