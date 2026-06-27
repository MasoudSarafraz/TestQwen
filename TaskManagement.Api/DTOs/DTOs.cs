namespace TaskManagement.Api.DTOs;

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Role { get; set; }
}

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Role { get; set; }
    public List<ProjectRoleDto>? Projects { get; set; }
}

public class ProjectRoleDto
{
    public int ProjectId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class CreateProjectRequest
{
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Budget { get; set; }
    public string Customer { get; set; } = string.Empty;
}

public class ProjectDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Budget { get; set; }
    public string Customer { get; set; } = string.Empty;
    public List<ProjectUserDto>? Users { get; set; }
}

public class ProjectUserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class AssignUserToProjectRequest
{
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty; // "ProjectManager" or "Expert"
}

public class LogTimeRequest
{
    public int ProjectId { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public int Minutes { get; set; }
}

public class TaskLogDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public TimeSpan TimeSpent { get; set; }
    public DateTime LoggedAt { get; set; }
    public bool IsApproved { get; set; }
    public string? ApprovedBy { get; set; }
}

public class ExpertTimeReportDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public TimeSpan TotalTime { get; set; }
    public List<ProjectTimeDto> ProjectTimes { get; set; } = new();
}

public class ProjectTimeDto
{
    public int ProjectId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public TimeSpan TotalTime { get; set; }
    public List<TaskTimeDto> TaskTimes { get; set; } = new();
}

public class TaskTimeDto
{
    public string TaskType { get; set; } = string.Empty;
    public TimeSpan TimeSpent { get; set; }
}

public class ProjectStatusDto
{
    public int ProjectId { get; set; }
    public string ProjectTitle { get; set; } = string.Empty;
    public string? LastApproval { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
