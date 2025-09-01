using Microsoft.EntityFrameworkCore;
using VerificationAPI.Models;
using VerificationAPI.XmlModels;

namespace VerificationAPI.Data
{
    public class VerificationDbContext : DbContext
    {
        public VerificationDbContext(DbContextOptions<VerificationDbContext> options) : base(options)
        {
        }

        public DbSet<VideoMetadata> VideoMetadata { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Media> Media { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure VideoMetadata
            modelBuilder.Entity<VideoMetadata>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever(); // Use the original ID from TikTok
                entity.Property(e => e.Url).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Source).HasMaxLength(50);
                entity.Property(e => e.UniqueId).HasMaxLength(100);
                entity.Property(e => e.Author).HasMaxLength(200);
                entity.Property(e => e.Title).HasMaxLength(500);
                entity.Property(e => e.Thumbnail).HasMaxLength(1000);
                entity.Property(e => e.Type).HasMaxLength(50);

                // One-to-many relationship with Media
                entity.HasMany(v => v.Medias)
                      .WithOne()
                      .HasForeignKey("VideoMetadataId")
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Media
            modelBuilder.Entity<Media>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Url).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.Quality).HasMaxLength(100);
                entity.Property(e => e.Extension).HasMaxLength(10);
                entity.Property(e => e.Type).HasMaxLength(50);
                entity.Property("VideoMetadataId"); // Shadow property for foreign key
            });

            // Configure User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Role).HasMaxLength(50);
                entity.Property(e => e.RefreshToken).HasMaxLength(500);

                // Add unique indexes
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Seed default admin user
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Email = "admin@test.com",
                    PasswordHash = HashPassword("admin123"),
                    Role = "Admin",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
        }

        private static string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + "salt123"));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}