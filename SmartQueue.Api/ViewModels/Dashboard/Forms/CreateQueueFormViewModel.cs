using System.ComponentModel.DataAnnotations;

namespace SmartQueue.Api.ViewModels.Dashboard.Forms
{
    public class CreateQueueFormViewModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [StringLength(250)]
        public string? Description { get; set; }

        [Range(1, 120)]
        [Display(Name = "Average Service Time (minutes)")]
        public int AverageServiceTimeMinutes { get; set; } = 5;
    }
}