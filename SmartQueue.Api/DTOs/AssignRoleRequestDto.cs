using System.ComponentModel.DataAnnotations;

namespace SmartQueue.Api.DTOs
{
    public class AssignRoleRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string Role { get; set; } = null!;
    }
}