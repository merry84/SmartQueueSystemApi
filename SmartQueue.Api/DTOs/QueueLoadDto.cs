namespace SmartQueue.Api.DTOs
{
    public class QueueLoadDto
    {
        public int QueueId { get; set; }

        public string QueueName { get; set; } = null!;

        public int WaitingTickets { get; set; }

        public int CalledTickets { get; set; }

        public int ServedTickets { get; set; }

        public bool IsActive { get; set; }
    }
}