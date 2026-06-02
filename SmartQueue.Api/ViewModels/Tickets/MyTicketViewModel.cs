namespace SmartQueue.Api.ViewModels.Tickets
{
    public class MyTicketViewModel
    {
        public int TicketId { get; set; }

        public int Number { get; set; }

        public string QueueName { get; set; } = null!;

        public string CustomerName { get; set; } = null!;

        public string Status { get; set; } = null!;

        public string Priority { get; set; } = null!;

        public DateTime JoinedAt { get; set; }

        public int PeopleAhead { get; set; }

        public int EstimatedWaitTimeMinutes { get; set; }
    }
}