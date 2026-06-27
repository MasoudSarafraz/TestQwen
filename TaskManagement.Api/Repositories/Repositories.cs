using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Data;
using TaskManagement.Api.Interfaces;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(int id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetUserWithProjectsAsync(int userId)
    {
        return await _dbSet
            .Include(u => u.ProjectUsers)
                .ThenInclude(pu => pu.Project)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<IEnumerable<User>> GetExpertsAsync()
    {
        return await _dbSet
            .Include(u => u.ProjectUsers)
            .Where(u => u.Role == Models.UserRole.Expert)
            .ToListAsync();
    }
}

public class ProjectRepository : Repository<Project>, IProjectRepository
{
    public ProjectRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Project?> GetProjectWithUsersAsync(int projectId)
    {
        return await _dbSet
            .Include(p => p.ProjectUsers)
                .ThenInclude(pu => pu.User)
            .FirstOrDefaultAsync(p => p.Id == projectId);
    }

    public async Task<IEnumerable<Project>> GetUserProjectsAsync(int userId)
    {
        return await _dbSet
            .Include(p => p.ProjectUsers)
            .Where(p => p.ProjectUsers.Any(pu => pu.UserId == userId))
            .ToListAsync();
    }
}

public class TaskLogRepository : Repository<TaskLog>, ITaskLogRepository
{
    public TaskLogRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<TaskLog>> GetProjectTaskLogsAsync(int projectId)
    {
        return await _dbSet
            .Include(t => t.User)
            .Include(t => t.ApprovedByUser)
            .Where(t => t.ProjectId == projectId)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskLog>> GetUserTaskLogsAsync(int userId)
    {
        return await _dbSet
            .Include(t => t.Project)
            .Where(t => t.UserId == userId)
            .ToListAsync();
    }

    public async Task<TaskLog?> GetLastTaskLogForProjectAsync(int projectId, TaskType taskType)
    {
        return await _dbSet
            .Where(t => t.ProjectId == projectId && t.TaskType == taskType)
            .OrderByDescending(t => t.LoggedAt)
            .FirstOrDefaultAsync();
    }
}

public class ProjectUserRepository : Repository<ProjectUser>, IProjectUserRepository
{
    public ProjectUserRepository(ApplicationDbContext context) : base(context) { }

    public async Task<ProjectUser?> GetUserProjectRoleAsync(int userId, int projectId)
    {
        return await _dbSet
            .Include(pu => pu.User)
            .Include(pu => pu.Project)
            .FirstOrDefaultAsync(pu => pu.UserId == userId && pu.ProjectId == projectId);
    }

    public async Task<IEnumerable<ProjectUser>> GetProjectUsersAsync(int projectId)
    {
        return await _dbSet
            .Include(pu => pu.User)
            .Where(pu => pu.ProjectId == projectId)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProjectUser>> GetUserProjectsAsync(int userId)
    {
        return await _dbSet
            .Include(pu => pu.Project)
            .Where(pu => pu.UserId == userId)
            .ToListAsync();
    }
}
