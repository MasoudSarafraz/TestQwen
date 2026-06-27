namespace TaskManagement.Api.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}

public interface IUserRepository : IRepository<Models.User>
{
    Task<Models.User?> GetByUsernameAsync(string username);
    Task<Models.User?> GetUserWithProjectsAsync(int userId);
    Task<IEnumerable<Models.User>> GetExpertsAsync();
}

public interface IProjectRepository : IRepository<Models.Project>
{
    Task<Models.Project?> GetProjectWithUsersAsync(int projectId);
    Task<IEnumerable<Models.Project>> GetUserProjectsAsync(int userId);
}

public interface ITaskLogRepository : IRepository<Models.TaskLog>
{
    Task<IEnumerable<Models.TaskLog>> GetProjectTaskLogsAsync(int projectId);
    Task<IEnumerable<Models.TaskLog>> GetUserTaskLogsAsync(int userId);
    Task<Models.TaskLog?> GetLastTaskLogForProjectAsync(int projectId, Models.TaskType taskType);
}

public interface IProjectUserRepository : IRepository<Models.ProjectUser>
{
    Task<Models.ProjectUser?> GetUserProjectRoleAsync(int userId, int projectId);
    Task<IEnumerable<Models.ProjectUser>> GetProjectUsersAsync(int projectId);
    Task<IEnumerable<Models.ProjectUser>> GetUserProjectsAsync(int userId);
}
