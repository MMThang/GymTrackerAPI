using GymTracker.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymTracker.Data
{
    public class AppDBContext : DbContext
    {
        protected readonly IConfiguration Configuration;

        public AppDBContext(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // connect to postgres with connection string from app settings
            options.UseNpgsql(Configuration.GetConnectionString("WebApiDatabase"));
        }

        public DbSet<User> Users { get; set; }
        public DbSet<ExternalLogin> ExternalLogins { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<OAuthLoginCode> OAuthLoginCodes { get; set; }
        public DbSet<EmailVerificationCode> EmailVerificationCodes { get; set; }
        public DbSet<WorkoutSession> WorkoutSessions { get; set; }
        public DbSet<Exercise> Exercises { get; set; }
        public DbSet<Set> Sets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasIndex(x => x.Email)
                .IsUnique();
            modelBuilder.Entity<ExternalLogin>()
                .HasIndex(x => new
                {
                    x.Provider,
                    x.ProviderUserId
                })
                .IsUnique();
            modelBuilder.Entity<ExternalLogin>()
                .HasOne(x => x.User)
                .WithMany(u => u.ExternalLogins)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RefreshToken>()
                .HasOne(t => t.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RefreshToken>()
                .HasIndex(x => x.ExpiryDate);
            modelBuilder.Entity<OAuthLoginCode>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<OAuthLoginCode>()
                .HasIndex(t => t.CodeHash)
                .IsUnique();
            modelBuilder.Entity<OAuthLoginCode>()
                .HasIndex(x => x.ExpiresAt);
            modelBuilder.Entity<OAuthLoginCode>()
                .HasIndex(x => x.UsedAt);
            modelBuilder.Entity<EmailVerificationCode>()
                .HasOne(x => x.User)
                .WithMany(x => x.EmailVerificationCodes)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<EmailVerificationCode>()
                .HasIndex(x => x.ExpiresAt);
            modelBuilder.Entity<EmailVerificationCode>()
                .HasIndex(x => x.UsedAt);
            modelBuilder.Entity<WorkoutSession>()
                .HasOne(t => t.User)
                .WithMany(u => u.WorkoutSessions)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Exercise>()
                .HasOne(t => t.WorkoutSession)
                .WithMany(u => u.Exercises)
                .HasForeignKey(t => t.WorkoutSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Set>()
                .HasOne(t => t.Exercise)
                .WithMany(u => u.Sets)
                .HasForeignKey(t => t.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
