using Microsoft.AspNetCore.Mvc;
using VerificationAPI.Models;
using VerificationAPI.Services;

namespace VerificationAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly JwtService _jwtService;
        private readonly UserService _userService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            JwtService jwtService,
            UserService userService,
            ILogger<AuthController> logger)
        {
            _jwtService = jwtService;
            _userService = userService;
            _logger = logger;
        }

        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TokenResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Username and password are required");
            }

            var user = _userService.ValidateUser(request.Username, request.Password);
            if (user == null)
            {
                _logger.LogWarning("Failed login attempt for username: {Username}", request.Username);
                return Unauthorized("Invalid username or password");
            }

            var accessToken = _jwtService.GenerateAccessToken(user.Id.ToString(), user.Username, user.Role);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7); // Refresh token valid for 7 days

            _userService.UpdateRefreshToken(user.Id, refreshToken, refreshTokenExpiry);

            _logger.LogInformation("User {Username} logged in successfully", user.Username);

            return Ok(new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });
        }

        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(UserResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) ||
                string.IsNullOrEmpty(request.Email) ||
                string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Username, email, and password are required");
            }

            if (request.Password.Length < 6)
            {
                return BadRequest("Password must be at least 6 characters long");
            }

            var user = _userService.CreateUser(request);
            if (user == null)
            {
                return Conflict("Username or email already exists");
            }

            _logger.LogInformation("New user registered: {Username}", user.Username);

            return CreatedAtAction(
                nameof(UsersController.GetById),
                "Users",
                new { id = user.Id },
                UserService.ToUserResponse(user));
        }

        [HttpPost("refresh")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TokenResponse))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest("Refresh token is required");
            }

            var user = _userService.GetUserByRefreshToken(request.RefreshToken);
            if (user == null)
            {
                _logger.LogWarning("Invalid refresh token attempted");
                return Unauthorized("Invalid refresh token");
            }

            var newAccessToken = _jwtService.GenerateAccessToken(user.Id.ToString(), user.Username, user.Role);
            var newRefreshToken = _jwtService.GenerateRefreshToken();
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            _userService.UpdateRefreshToken(user.Id, newRefreshToken, refreshTokenExpiry);

            _logger.LogInformation("Token refreshed for user: {Username}", user.Username);

            return Ok(new TokenResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                Expires = DateTime.UtcNow.AddMinutes(15)
            });
        }

        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Logout([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest("Refresh token is required");
            }

            var user = _userService.GetUserByRefreshToken(request.RefreshToken);
            if (user != null)
            {
                _userService.RevokeRefreshToken(user.Id);
                _logger.LogInformation("User {Username} logged out", user.Username);
            }

            return Ok(new { message = "Logged out successfully" });
        }
    }
}