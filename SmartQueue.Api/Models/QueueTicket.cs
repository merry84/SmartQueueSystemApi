namespace SmartQueue.Api.Models
{
    public class QueueTicket
    {
        public int Id { get; set; }

        public string CustomerName { get; set; } = null!;

        public int Number { get; set; }

        public string Status { get; set; } = "Waiting";

        public string Priority { get; set; } = "Normal";

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public DateTime? CalledOn { get; set; }

        public int QueueId { get; set; }

        public Queue Queue { get; set; } = null!;
    }
}