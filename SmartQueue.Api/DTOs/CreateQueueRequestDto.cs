using System.ComponentModel.DataAnnotations;

namespace SmartQueue.Api.DTOs
{
    public class CreateQueueRequestDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [StringLength(250)]
        public string? Description { get; set; }
    }
}