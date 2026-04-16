namespace SmartQueue.Api.ViewModels.Dashboard
{
    public class DashboardRecentTicketViewModel
    {
        public int TicketId { get; set; }

        public string CustomerName { get; set; } = null!;

        public int Number { get; set; }

        public string QueueName { get; set; } = null!;

        public string Status { get; set; } = null!;

        public string Priority { get; set; } = null!;
        public int QueueId { get; set; }

        public DateTime CreatedOn { get; set; }

        public int EstimatedWaitTimeMinutes { get; set; }
    }
}