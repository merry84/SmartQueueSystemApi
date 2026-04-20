using SmartQueue.Api.Enums;

namespace SmartQueue.Api.Models
{
    public class QueueTicket
    {
        public int Id { get; set; }

        public string CustomerName { get; set; } = null!;

        public int Number { get; set; }

        public TicketStatus Status { get; set; } = TicketStatus.Waiting;

        public QueuePriority Priority { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CalledAt { get; set; }

        public DateTime? ServiceStartedAt { get; set; }

        public DateTime? ServedAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        public int QueueId { get; set; }

        public Queue Queue { get; set; } = null!;
    }
}