using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartQueue.Api.ViewModels.Dashboard.Forms
{
    public class CallNextFormViewModel
    {
        [Required]
        [Display(Name = "Queue")]
        public int QueueId { get; set; }

        public List<SelectListItem> Queues { get; set; } = new();
    }
}