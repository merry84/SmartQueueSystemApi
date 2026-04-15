namespace SmartQueue.Api.DTOs
{
    public class AdminSummaryDto
    {
        public int TotalQueues { get; set; }

        public int ActiveQueues { get; set; }

        public int TotalTickets { get; set; }

        public int WaitingTickets { get; set; }

        public double AverageWaitTimeMinutes { get; set; }

        public IEnumerable<QueueLoadDto> QueueLoads { get; set; } = new List<QueueLoadDto>();

        public IEnumerable<RecentTicketDto> RecentTickets { get; set; } = new List<RecentTicketDto>();
    }
}