using System.ComponentModel.DataAnnotations;

namespace SmartQueue.Api.DTOs
{
    public class JoinQueueRequestDto
    {
        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = null!;

        [StringLength(20)]
        public string? Priority { get; set; } = "Normal";
    }
}