namespace SmartQueue.Api.DTOs
{
    public class QueueTicketResponseDto
    {
        public int Id { get; set; }

        public string CustomerName { get; set; } = null!;

        public int Number { get; set; }

        public string Status { get; set; } = null!;

        public string Priority { get; set; } = null!;

        public DateTime CreatedOn { get; set; }
        public int EstimatedWaitTimeMinutes { get; set; }
    }
}