using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartQueue.Api.ViewModels.Dashboard.Forms
{
    public class ServeTicketFormViewModel
    {
        [Required]
        [Display(Name = "Ticket")]
        public int TicketId { get; set; }

        public List<SelectListItem> Tickets { get; set; } = new();
    }
}