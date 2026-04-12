namespace SmartQueue.Api.DTOs
{
    public class LoginResponseDto
    {
        public bool Succeeded { get; set; }

        public string Message { get; set; } = null!;

        public string? Token { get; set; }

        public DateTime? ExpiresAtUtc { get; set; }
    }
}