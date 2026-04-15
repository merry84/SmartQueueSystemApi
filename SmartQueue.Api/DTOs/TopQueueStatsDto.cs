namespace SmartQueue.Api.DTOs
{
    public class TopQueueStatsDto
    {
        public int QueueId { get; set; }

        public string QueueName { get; set; } = null!;

        public int TotalTickets { get; set; }

        public int WaitingTickets { get; set; }

        public int ServedTickets { get; set; }

        public int AverageServiceTimeMinutes { get; set; }
    }
}