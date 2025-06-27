// Controllers/AuthController.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using VerificationAPI.Models;

namespace VerificationAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        // In‐memory demo user
        private static readonly Dictionary<string, string> _users =
            new() { ["demo"] = "password123" };

        // Store refresh tokens → username
        private static readonly Dictionary<string, string> _refreshTokens =
            new(StringComparer.Ordinal);

        private readonly IConfiguration _config;
        public AuthController(IConfiguration config) => _config = config;

        [HttpPost("login")]
        public ActionResult<TokenResponse> Login([FromBody] LoginRequest req)
        {
            // 1) Validate credentials
            if (!_users.TryGetValue(req.Username, out var pwd) || pwd != req.Password)
                return Unauthorized("Invalid user or password");

            // 2) Create tokens
            var access = CreateJwtToken(req.Username, minutes: 15);
            var refresh = CreateJwtToken(req.Username, minutes: 60 * 24 * 7);

            // 3) Persist refresh token
            _refreshTokens[refresh] = req.Username;

            return Ok(new TokenResponse(access, refresh));
        }

        [HttpPost("refresh")]
        public ActionResult<TokenResponse> Refresh([FromBody] RefreshRequest req)
        {
            if (req.RefreshToken is null ||
                !_refreshTokens.Remove(req.RefreshToken, out var user))
                return Unauthorized("Invalid refresh token");

            var newAccess = CreateJwtToken(user, minutes: 15);
            var newRefresh = CreateJwtToken(user, minutes: 60 * 24 * 7);
            _refreshTokens[newRefresh] = user;

            return Ok(new TokenResponse(newAccess, newRefresh));
        }

        private string CreateJwtToken(string username, int minutes)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var now = DateTime.UtcNow;
            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti,
                          Guid.NewGuid().ToString())
            };
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(minutes),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
