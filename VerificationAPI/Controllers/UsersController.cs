using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VerificationAPI.Models;
using VerificationAPI.Services;

namespace VerificationAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All endpoints require authentication
    public class UsersController : Controller
    {
        private readonly UserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(UserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")] // Only admins can see all users
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<UserResponse>))]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public ActionResult<List<UserResponse>> GetAll()
        {
            var users = _userService.GetAllUsers();
            var userResponses = users.Select(UserService.ToUserResponse).ToList();

            _logger.LogInformation("Admin {Username} retrieved all users", User.Identity?.Name);
            return Ok(userResponses);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public ActionResult<UserResponse> GetById(int id)
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Users can only see their own profile, admins can see anyone's
            if (currentUserRole != "Admin" && currentUserId != id)
            {
                _logger.LogWarning("User {UserId} attempted to access user {RequestedId}", currentUserId, id);
                return Forbid("You can only access your own profile");
            }

            var user = _userService.GetUserById(id);
            if (user == null)
            {
                return NotFound($"User with ID {id} not found");
            }

            return Ok(UserService.ToUserResponse(user));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")] // Only admins can create users via this endpoint
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(UserResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult Create([FromBody] RegisterRequest request)
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

            _logger.LogInformation("Admin {Username} created new user: {NewUsername}",
                User.Identity?.Name, user.Username);

            return CreatedAtAction(nameof(GetById), new { id = user.Id }, UserService.ToUserResponse(user));
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public IActionResult Update(int id, [FromBody] UpdateUserRequest request)
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Users can only update their own profile, admins can update anyone's
            if (currentUserRole != "Admin" && currentUserId != id)
            {
                _logger.LogWarning("User {UserId} attempted to update user {RequestedId}", currentUserId, id);
                return Forbid("You can only update your own profile");
            }

            var existingUser = _userService.GetUserById(id);
            if (existingUser == null)
            {
                return NotFound($"User with ID {id} not found");
            }

            if (request.Password != null && request.Password.Length < 6)
            {
                return BadRequest("Password must be at least 6 characters long");
            }

            var updatedUser = _userService.UpdateUser(id, request);
            if (updatedUser == null)
            {
                return Conflict("Username or email already exists");
            }

            _logger.LogInformation("User {UserId} updated user {UpdatedUserId}", currentUserId, id);

            return Ok(UserService.ToUserResponse(updatedUser));
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult Delete(int id)
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Users can only delete their own account, admins can delete anyone's (except themselves)
            if (currentUserRole != "Admin" && currentUserId != id)
            {
                return Forbid("You can only delete your own account");
            }

            // Prevent admin from deleting themselves
            if (currentUserRole == "Admin" && currentUserId == id)
            {
                return BadRequest("Admins cannot delete their own account");
            }

            var success = _userService.DeleteUser(id);
            if (!success)
            {
                return NotFound($"User with ID {id} not found");
            }

            _logger.LogInformation("User {UserId} deleted user {DeletedUserId}", currentUserId, id);

            return NoContent();
        }

        [HttpGet("me")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<UserResponse> GetCurrentUser()
        {
            var currentUserId = GetCurrentUserId();
            var user = _userService.GetUserById(currentUserId);

            if (user == null)
            {
                return NotFound("Current user not found");
            }

            return Ok(UserService.ToUserResponse(user));
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }
}