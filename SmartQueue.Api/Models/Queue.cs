namespace SmartQueue.Api.Models
{
    public class Queue
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public ICollection<QueueTicket> Tickets { get; set; } = new List<QueueTicket>();
    }
}