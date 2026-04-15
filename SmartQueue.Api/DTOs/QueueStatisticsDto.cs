namespace SmartQueue.Api.DTOs
{
    public class QueueStatisticsDto
    {
        public QueueOverviewStatsDto Overview { get; set; } = new();

        public TodayQueueStatsDto Today { get; set; } = new();

        public IEnumerable<TopQueueStatsDto> TopQueues { get; set; } = new List<TopQueueStatsDto>();
    }
}