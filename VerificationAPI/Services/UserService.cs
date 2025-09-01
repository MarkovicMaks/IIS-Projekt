using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using VerificationAPI.Data;
using VerificationAPI.Models;

namespace VerificationAPI.Services
{
    public class UserService
    {
        private readonly VerificationDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(VerificationDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<User?> ValidateUserAsync(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || !VerifyPassword(password, user.PasswordHash))
                return null;

            return user;
        }

        public async Task<User?> CreateUserAsync(RegisterRequest request)
        {
            // Check if username already exists
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return null;

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return null;

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                Role = "User"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new user: {Username}", user.Username);
            return user;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> UpdateUserAsync(int id, UpdateUserRequest request)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return null;

            if (!string.IsNullOrEmpty(request.Username))
            {
                // Check if new username already exists (excluding current user)
                if (await _context.Users.AnyAsync(u => u.Username == request.Username && u.Id != id))
                    return null;
                user.Username = request.Username;
            }

            if (!string.IsNullOrEmpty(request.Email))
            {
                // Check if new email already exists (excluding current user)
                if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.Id != id))
                    return null;
                user.Email = request.Email;
            }

            if (!string.IsNullOrEmpty(request.Password))
            {
                user.PasswordHash = HashPassword(request.Password);
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated user: {Username}", user.Username);
            return user;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted user: {Username}", user.Username);
            return true;
        }

        public async Task UpdateRefreshTokenAsync(int userId, string refreshToken, DateTime expiry)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiry = expiry;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<User?> GetUserByRefreshTokenAsync(string refreshToken)
        {
            return await _context.Users.FirstOrDefaultAsync(u =>
                u.RefreshToken == refreshToken &&
                u.RefreshTokenExpiry > DateTime.UtcNow);
        }

        public async Task RevokeRefreshTokenAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;
                await _context.SaveChangesAsync();
            }
        }

        // Keep synchronous versions for backward compatibility
        public User? ValidateUser(string username, string password) => ValidateUserAsync(username, password).Result;
        public User? CreateUser(RegisterRequest request) => CreateUserAsync(request).Result;
        public List<User> GetAllUsers() => GetAllUsersAsync().Result;
        public User? GetUserById(int id) => GetUserByIdAsync(id).Result;
        public User? UpdateUser(int id, UpdateUserRequest request) => UpdateUserAsync(id, request).Result;
        public bool DeleteUser(int id) => DeleteUserAsync(id).Result;
        public void UpdateRefreshToken(int userId, string refreshToken, DateTime expiry) => UpdateRefreshTokenAsync(userId, refreshToken, expiry).Wait();
        public User? GetUserByRefreshToken(string refreshToken) => GetUserByRefreshTokenAsync(refreshToken).Result;
        public void RevokeRefreshToken(int userId) => RevokeRefreshTokenAsync(userId).Wait();

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "salt123")); // Simple salt
            return Convert.ToBase64String(hashedBytes);
        }

        private static bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }

        public static UserResponse ToUserResponse(User user)
        {
            return new UserResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }
    }
}