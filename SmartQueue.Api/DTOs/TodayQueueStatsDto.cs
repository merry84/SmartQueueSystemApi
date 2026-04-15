namespace SmartQueue.Api.DTOs
{
    public class TodayQueueStatsDto
    {
        public int TicketsCreatedToday { get; set; }

        public int TicketsServedToday { get; set; }

        public int TicketsCalledToday { get; set; }
    }
}