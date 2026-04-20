using SmartQueue.Api.DTOs;

namespace SmartQueue.Api.Services.Contracts
{
    public interface IQueueService
    {
        Task<IEnumerable<QueueResponseDto>> GetAllAsync();
        Task<QueueResponseDto?> GetByIdAsync(int id);
        Task<QueueResponseDto> CreateAsync(CreateQueueRequestDto model);
    }
}