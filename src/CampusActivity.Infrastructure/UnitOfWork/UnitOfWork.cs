using Microsoft.EntityFrameworkCore.Storage;
using CampusActivity.Domain.Entities;
using CampusActivity.Infrastructure.Data;
using CampusActivity.Infrastructure.Repositories;

namespace CampusActivity.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;
    private bool _disposed = false;

    private IRepository<User>? _users;
    private IRepository<Activity>? _activities;
    private IRepository<ActivityCategory>? _activityCategories;
    private IRepository<ActivityRegistration>? _activityRegistrations;
    private IRepository<ActivityTag>? _activityTags;
    private IRepository<ActivityRecommendation>? _activityRecommendations;
    private IRepository<UserActivityPreference>? _userActivityPreferences;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IRepository<User> Users => _users ??= new Repository<User>(_context);
    public IRepository<Activity> Activities => _activities ??= new Repository<Activity>(_context);
    public IRepository<ActivityCategory> ActivityCategories => _activityCategories ??= new Repository<ActivityCategory>(_context);
    public IRepository<ActivityRegistration> ActivityRegistrations => _activityRegistrations ??= new Repository<ActivityRegistration>(_context);
    public IRepository<ActivityTag> ActivityTags => _activityTags ??= new Repository<ActivityTag>(_context);
    public IRepository<ActivityRecommendation> ActivityRecommendations => _activityRecommendations ??= new Repository<ActivityRecommendation>(_context);
    public IRepository<UserActivityPreference> UserActivityPreferences => _userActivityPreferences ??= new Repository<UserActivityPreference>(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
} 