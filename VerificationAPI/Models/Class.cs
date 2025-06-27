namespace VerificationAPI.Models
{
    public record LoginRequest(string Username, string Password);
    public record TokenResponse(string AccessToken, string RefreshToken);
    public record RefreshRequest(string RefreshToken);
}