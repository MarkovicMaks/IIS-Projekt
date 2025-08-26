using System.Security.Cryptography;
using System.Text;
using VerificationAPI.Models;

namespace VerificationAPI.Services
{
    public class UserService
    {
        private readonly List<User> _users;
        private int _nextId = 1;

        public UserService()
        {
            _users = new List<User>();

            // Add a default admin user for testing
            _users.Add(new User
            {
                Id = _nextId++,
                Username = "admin",
                Email = "admin@test.com",
                PasswordHash = HashPassword("admin123"),
                Role = "Admin"
            });
        }

        public User? ValidateUser(string username, string password)
        {
            var user = _users.FirstOrDefault(u => u.Username == username);
            if (user == null || !VerifyPassword(password, user.PasswordHash))
                return null;

            return user;
        }

        public User? CreateUser(RegisterRequest request)
        {
            // Check if username already exists
            if (_users.Any(u => u.Username == request.Username))
                return null;

            // Check if email already exists
            if (_users.Any(u => u.Email == request.Email))
                return null;

            var user = new User
            {
                Id = _nextId++,
                Username = request.Username,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                Role = "User"
            };

            _users.Add(user);
            return user;
        }

        public List<User> GetAllUsers()
        {
            return _users.ToList();
        }

        public User? GetUserById(int id)
        {
            return _users.FirstOrDefault(u => u.Id == id);
        }

        public User? UpdateUser(int id, UpdateUserRequest request)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return null;

            if (!string.IsNullOrEmpty(request.Username))
            {
                // Check if new username already exists (excluding current user)
                if (_users.Any(u => u.Username == request.Username && u.Id != id))
                    return null;
                user.Username = request.Username;
            }

            if (!string.IsNullOrEmpty(request.Email))
            {
                // Check if new email already exists (excluding current user)
                if (_users.Any(u => u.Email == request.Email && u.Id != id))
                    return null;
                user.Email = request.Email;
            }

            if (!string.IsNullOrEmpty(request.Password))
            {
                user.PasswordHash = HashPassword(request.Password);
            }

            user.UpdatedAt = DateTime.UtcNow;
            return user;
        }

        public bool DeleteUser(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user == null)
                return false;

            _users.Remove(user);
            return true;
        }

        public void UpdateRefreshToken(int userId, string refreshToken, DateTime expiry)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiry = expiry;
            }
        }

        public User? GetUserByRefreshToken(string refreshToken)
        {
            return _users.FirstOrDefault(u => u.RefreshToken == refreshToken &&
                                             u.RefreshTokenExpiry > DateTime.UtcNow);
        }

        public void RevokeRefreshToken(int userId)
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;
            }
        }

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