using Microsoft.EntityFrameworkCore;
using CampusActivity.Domain.Entities;

namespace CampusActivity.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Activity> Activities { get; set; }
    public DbSet<ActivityCategory> ActivityCategories { get; set; }
    public DbSet<ActivityRegistration> ActivityRegistrations { get; set; }
    public DbSet<ActivityTag> ActivityTags { get; set; }
    public DbSet<ActivityRecommendation> ActivityRecommendations { get; set; }
    public DbSet<UserActivityPreference> UserActivityPreferences { get; set; }
    public DbSet<ScheduleItem> ScheduleItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 用户实体配置
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Avatar).HasMaxLength(500);
            entity.Property(e => e.StudentId).HasMaxLength(20);
            entity.Property(e => e.Major).HasMaxLength(100);
            entity.Property(e => e.EmployeeId).HasMaxLength(20);
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(100);
            
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // 活动实体配置
        modelBuilder.Entity<Activity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Location).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            
            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Activities)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
                  
            entity.HasOne(e => e.Creator)
                  .WithMany(u => u.CreatedActivities)
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 活动分类实体配置
        modelBuilder.Entity<ActivityCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IconUrl).HasMaxLength(500);
        });

        // 活动报名实体配置
        modelBuilder.Entity<ActivityRegistration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Note).HasMaxLength(500);
            
            entity.HasOne(e => e.Activity)
                  .WithMany(a => a.Registrations)
                  .HasForeignKey(e => e.ActivityId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Registrations)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasIndex(e => new { e.ActivityId, e.UserId }).IsUnique();
        });

        // 活动标签实体配置
        modelBuilder.Entity<ActivityTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TagName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Color).HasMaxLength(7);
            
            entity.HasOne(e => e.Activity)
                  .WithMany(a => a.Tags)
                  .HasForeignKey(e => e.ActivityId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 活动推荐实体配置
        modelBuilder.Entity<ActivityRecommendation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(500);
            
            entity.HasOne(e => e.Activity)
                  .WithMany(a => a.Recommendations)
                  .HasForeignKey(e => e.ActivityId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasIndex(e => new { e.ActivityId, e.UserId }).IsUnique();
        });

        // 用户活动偏好实体配置
        modelBuilder.Entity<UserActivityPreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Preferences)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Category)
                  .WithMany()
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasIndex(e => new { e.UserId, e.CategoryId }).IsUnique();
        });

        // 日程表实体配置
        modelBuilder.Entity<ScheduleItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.Color).HasMaxLength(7);
            entity.Property(e => e.Note).HasMaxLength(500);
            
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.Activity)
                  .WithMany()
                  .HasForeignKey(e => e.ActivityId)
                  .OnDelete(DeleteBehavior.SetNull);
                  
            entity.HasIndex(e => new { e.UserId, e.StartTime });
            entity.HasIndex(e => new { e.UserId, e.Type });
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;
            
            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }
            
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
} 