using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartQueue.Api.ViewModels.Dashboard.Forms
{
    public class JoinQueueFormViewModel
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = null!;

        [Required]
        [Display(Name = "Queue")]
        public int QueueId { get; set; }

        [Display(Name = "Priority")]
        public string Priority { get; set; } = "Normal";

        public List<SelectListItem> Queues { get; set; } = new();
    }
}