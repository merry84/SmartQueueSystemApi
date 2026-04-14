using SmartQueue.Api.Enums;

namespace SmartQueue.Api.Models
{
    public class QueueTicket
    {
        public int Id { get; set; }

        public string CustomerName { get; set; } = null!;

        public int Number { get; set; }

        public QueueStatus Status { get; set; }

        public QueuePriority Priority { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime? CalledOn { get; set; }

        public DateTime? ServedOn { get; set; }

        public int EstimatedWaitTimeMinutes { get; set; }

        public int QueueId { get; set; }

        public Queue Queue { get; set; } = null!;
    }
}