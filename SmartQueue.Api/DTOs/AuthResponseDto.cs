namespace SmartQueue.Api.DTOs
{
    public class AuthResponseDto
    {
        public bool Succeeded { get; set; }

        public string Message { get; set; } = null!;
    }
}