using Microsoft.AspNetCore.Identity;

namespace SmartQueue.Api.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ICollection<QueueTicket> QueueTickets { get; set; } 
            = new List<QueueTicket>();
    }
}