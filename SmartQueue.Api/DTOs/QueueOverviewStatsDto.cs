namespace SmartQueue.Api.DTOs
{
    public class QueueOverviewStatsDto
    {
        public int TotalTickets { get; set; }

        public int WaitingTickets { get; set; }

        public int CalledTickets { get; set; }

        public int ServedTickets { get; set; }

        public double AverageWaitTimeMinutes { get; set; }

        public string? MostRequestedQueueName { get; set; }
    }
}