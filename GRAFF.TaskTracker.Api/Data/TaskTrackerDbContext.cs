using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GRAFF.TaskTracker.Models;

namespace GRAFF.TaskTracker.Api.Data
{
    // Specify User, Role (using IdentityRole<int>), and key type (int)
    public class TaskTrackerDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public TaskTrackerDbContext(DbContextOptions<TaskTrackerDbContext> options) : base(options)
        {
        }

        // DbSets for User and Role are handled by IdentityDbContext.
        // We only need to declare our custom ones.
        public DbSet<Project> Projects { get; set; }
        public DbSet<Column> Columns { get; set; }
        public DbSet<GRAFF.TaskTracker.Models.Task> Tasks { get; set; } // Kept fully qualified name to avoid ambiguity

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // This is crucial for Identity schema setup

            // Custom model configurations
            modelBuilder.Entity<User>(entity =>
            {
                // If you had custom configurations for User, they would go here.
                // For example, if you kept the Username, Email properties in User.cs
                // and wanted to ensure they map to the IdentityUser properties.
                // However, it's cleaner to rely on IdentityUser's properties directly.
            });

            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasOne(p => p.Owner)
                      .WithMany(u => u.Projects)
                      .HasForeignKey(p => p.OwnerUserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Column>(entity =>
            {
                entity.HasOne(c => c.Project)
                      .WithMany(p => p.Columns)
                      .HasForeignKey(c => c.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<GRAFF.TaskTracker.Models.Task>(entity => // Kept fully qualified name
            {
                entity.HasOne(t => t.Column)
                      .WithMany(c => c.Tasks)
                      .HasForeignKey(t => t.ColumnId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Identity table names if needed (optional, usually defaults are fine)
            // Example:
            // modelBuilder.Entity<User>().ToTable("Users"); // This is not Users as DbSet is handled by Identity
            // modelBuilder.Entity<IdentityRole<int>>().ToTable("Roles");
            // modelBuilder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
            // modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
            // modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
            // modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
            // modelBuilder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");
        }
    }
}
