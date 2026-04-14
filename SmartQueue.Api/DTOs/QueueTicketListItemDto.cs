namespace SmartQueue.Api.DTOs
{
    public class QueueTicketListItemDto
    {
        public int Id { get; set; }

        public string CustomerName { get; set; } = null!;

        public int Number { get; set; }

        public string Status { get; set; } = null!;

        public string Priority { get; set; } = null!;
        public int EstimatedWaitTimeMinutes { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? CalledOn { get; set; }
    }
}