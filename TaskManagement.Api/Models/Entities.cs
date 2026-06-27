namespace TaskManagement.Api.Models;

public enum UserRole
{
    SystemAdmin = 1,
    ProjectManager = 2,
    Expert = 3
}

public enum TaskType
{
    // Expert tasks
    RequirementsAnalysis = 1,
    Implementation = 2,
    Documentation = 3,
    Delivery = 4,
    
    // Project Manager tasks (approvals)
    ApproveRequirements = 5,
    ApproveImplementation = 6,
    ApproveDocumentation = 7,
    ApproveDelivery = 8
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole? Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<ProjectUser> ProjectUsers { get; set; } = new List<ProjectUser>();
}

public class Project
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Budget { get; set; }
    public string Customer { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<ProjectUser> ProjectUsers { get; set; } = new List<ProjectUser>();
    public ICollection<TaskLog> TaskLogs { get; set; } = new List<TaskLog>();
}

public class ProjectUser
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProjectId { get; set; }
    public UserRole Role { get; set; }
    
    public User User { get; set; } = null!;
    public Project Project { get; set; } = null!;
}

public class TaskLog
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int UserId { get; set; }
    public TaskType TaskType { get; set; }
    public TimeSpan TimeSpent { get; set; }
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
    public bool IsApproved { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    
    public Project Project { get; set; } = null!;
    public User User { get; set; } = null!;
    public User? ApprovedByUser { get; set; }
}
