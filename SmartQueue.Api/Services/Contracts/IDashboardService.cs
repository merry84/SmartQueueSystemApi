using SmartQueue.Api.DTOs;

namespace SmartQueue.Api.Services.Contracts
{
    public interface IDashboardService
    {
        Task<QueueStatisticsDto> GetStatisticsAsync();
        Task<AdminSummaryDto> GetAdminSummaryAsync();
    }
}