using CampusActivity.Domain.Entities;
using CampusActivity.Infrastructure.Repositories;

namespace CampusActivity.Infrastructure.UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Activity> Activities { get; }
    IRepository<ActivityCategory> ActivityCategories { get; }
    IRepository<ActivityRegistration> ActivityRegistrations { get; }
    IRepository<ActivityTag> ActivityTags { get; }
    IRepository<ActivityRecommendation> ActivityRecommendations { get; }
    IRepository<UserActivityPreference> UserActivityPreferences { get; }
    IRepository<ScheduleItem> ScheduleItems { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
} 