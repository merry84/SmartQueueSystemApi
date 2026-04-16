namespace SmartQueue.Api.ViewModels.Dashboard
{
    public class DashboardIndexViewModel
    {
        public int TotalQueues { get; set; }

        public int ActiveQueues { get; set; }

        public int TotalTickets { get; set; }

        public int WaitingTickets { get; set; }

        public int CalledTickets { get; set; }

        public int ServedTickets { get; set; }

        public double AverageWaitTimeMinutes { get; set; }

        public string MostRequestedQueueName { get; set; } = "N/A";

        public int TicketsCreatedToday { get; set; }

        public int TicketsCalledToday { get; set; }

        public int TicketsServedToday { get; set; }

        public double ServiceRatePercent { get; set; }
        public List<int> TicketsLast7Days { get; set; } = new();
        public List<string> Last7DaysLabels { get; set; } = new();

        public List<string> QueueNames { get; set; } = new();
        public List<int> TicketsPerQueue { get; set; } = new();

        public List<DashboardQueueCardViewModel> TopQueues { get; set; } = new();

        public List<DashboardRecentTicketViewModel> RecentTickets { get; set; } = new();
    }
}