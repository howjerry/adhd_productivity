using AdhdProductivitySystem.Application.Common.Interfaces;
using AdhdProductivitySystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdhdProductivitySystem.Infrastructure.Data;

/// <summary>
/// Main database context for the ADHD productivity system
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Users in the system
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Tasks in the system
    /// </summary>
    public DbSet<TaskItem> Tasks { get; set; } = null!;

    /// <summary>
    /// Captured items (brain dump)
    /// </summary>
    public DbSet<CaptureItem> CaptureItems { get; set; } = null!;

    /// <summary>
    /// Time blocks for scheduling
    /// </summary>
    public DbSet<TimeBlock> TimeBlocks { get; set; } = null!;

    /// <summary>
    /// User progress tracking
    /// </summary>
    public DbSet<UserProgress> UserProgress { get; set; } = null!;

    /// <summary>
    /// Timer sessions
    /// </summary>
    public DbSet<TimerSession> TimerSessions { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(e => e.PasswordSalt).HasMaxLength(500).IsRequired();
            entity.Property(e => e.TimeZone).HasMaxLength(50);
            entity.Property(e => e.ProfilePictureUrl).HasMaxLength(500);
        });

        // Configure TaskItem entity
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.RecurrencePattern).HasMaxLength(100);

            // Configure relationships
            entity.HasOne(e => e.User)
                .WithMany(u => u.Tasks)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ParentTask)
                .WithMany(t => t.SubTasks)
                .HasForeignKey(e => e.ParentTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure indexes
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.DueDate);
        });

        // Configure CaptureItem entity
        modelBuilder.Entity<CaptureItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.Context).HasMaxLength(200);
            entity.Property(e => e.Mood).HasMaxLength(50);

            // Configure relationships
            entity.HasOne(e => e.User)
                .WithMany(u => u.CaptureItems)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Task)
                .WithMany()
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure indexes
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.IsProcessed);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure TimeBlock entity
        modelBuilder.Entity<TimeBlock>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Color).HasMaxLength(7);
            entity.Property(e => e.RecurrencePattern).HasMaxLength(100);
            entity.Property(e => e.CompletionNotes).HasMaxLength(1000);

            // Configure relationships
            entity.HasOne(e => e.User)
                .WithMany(u => u.TimeBlocks)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Task)
                .WithMany()
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure indexes
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.StartTime);
            entity.HasIndex(e => e.Type);
        });

        // Configure UserProgress entity
        modelBuilder.Entity<UserProgress>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.WentWell).HasMaxLength(1000);
            entity.Property(e => e.ToImprove).HasMaxLength(1000);
            entity.Property(e => e.TomorrowGoals).HasMaxLength(1000);
            entity.Property(e => e.HoursSlept).HasPrecision(3, 1);

            // Configure relationships
            entity.HasOne(e => e.User)
                .WithMany(u => u.ProgressRecords)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure indexes
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => new { e.UserId, e.Date }).IsUnique();
        });

        // Configure TimerSession entity
        modelBuilder.Entity<TimerSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.Accomplishments).HasMaxLength(1000);
            entity.Property(e => e.Challenges).HasMaxLength(1000);

            // Configure relationships
            entity.HasOne(e => e.User)
                .WithMany(u => u.TimerSessions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Task)
                .WithMany(t => t.TimerSessions)
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure indexes
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.StartTime);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Status);
        });

        // Configure base entity properties
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Domain.Common.BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(Domain.Common.BaseEntity.CreatedBy))
                    .HasMaxLength(256);

                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(Domain.Common.BaseEntity.UpdatedBy))
                    .HasMaxLength(256);
            }
        }
    }

    /// <summary>
    /// Override SaveChanges to automatically update timestamps
    /// </summary>
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    /// <summary>
    /// Override SaveChangesAsync to automatically update timestamps
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Updates CreatedAt and UpdatedAt timestamps for entities
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Domain.Common.BaseEntity && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (Domain.Common.BaseEntity)entry.Entity;
            
            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }
            
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}