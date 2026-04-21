using SmartQueue.Api.DTOs;
using SmartQueue.Api.ViewModels.Dashboard;

namespace SmartQueue.Api.Services.Contracts
{
    public interface IDashboardService
    {
        Task<QueueStatisticsDto> GetStatisticsAsync();
        Task<AdminSummaryDto> GetAdminSummaryAsync();
        Task<DashboardIndexViewModel> GetDashboardDataAsync();
    }
}