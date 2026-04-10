namespace SmartQueue.Api.DTOs
{
    public class QueueResponseDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedOn { get; set; }
    }
}