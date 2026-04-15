using SmartQueue.Api.DTOs;

namespace SmartQueue.Api.Services.Contracts
{
    public interface IQueueService
    {
        Task<IEnumerable<QueueResponseDto>> GetAllAsync();

        Task<QueueResponseDto?> GetByIdAsync(int id);

        Task<QueueResponseDto> CreateAsync(CreateQueueRequestDto model);

        Task<QueueTicketResponseDto> JoinQueueAsync(int id, JoinQueueRequestDto model);

        Task<IEnumerable<QueueTicketListItemDto>> GetTicketsAsync(int id);

        Task<NextTicketResponseDto?> CallNextAsync(int id);
        Task<QueueStatisticsDto> GetStatisticsAsync();
        Task<AdminSummaryDto> GetAdminSummaryAsync();
    }
}