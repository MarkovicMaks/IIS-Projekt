namespace VerificationAPI.Models
{
    public class LoginRequest
    {
        public string Username { get; init; } = default!;
        public string Password { get; init; } = default!;
    }
    public class RefreshRequest
    {
        public string RefreshToken { get; init; } = default!;
    }
    public record TokenResponse(string AccessToken, string RefreshToken);


}