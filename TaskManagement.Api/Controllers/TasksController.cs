using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Api.DTOs;
using TaskManagement.Api.Interfaces;
using TaskManagement.Api.Middleware;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskLogRepository _taskLogRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IProjectUserRepository _projectUserRepository;
    private readonly ILogger<TasksController> _logger;

    public TasksController(
        ITaskLogRepository taskLogRepository,
        IProjectRepository projectRepository,
        IProjectUserRepository projectUserRepository,
        ILogger<TasksController> logger)
    {
        _taskLogRepository = taskLogRepository;
        _projectRepository = projectRepository;
        _projectUserRepository = projectUserRepository;
        _logger = logger;
    }

    [HttpPost("log-time")]
    public async Task<ActionResult> LogTime([FromBody] LogTimeRequest request)
    {
        var currentUserId = JwtTokenHelper.GetCurrentUserId(User);
        var currentUserRole = JwtTokenHelper.GetCurrentUserRole(User);

        if (!currentUserId.HasValue)
        {
            return Unauthorized(new { success = false, message = "User not authenticated" });
        }

        // Get user's role in the project
        var userProjectRole = await _projectUserRepository.GetUserProjectRoleAsync(currentUserId.Value, request.ProjectId);
        
        if (currentUserRole == "Expert")
        {
            // Expert can log time for: RequirementsAnalysis, Implementation, Documentation, Delivery
            if (!Enum.TryParse<TaskType>(request.TaskType, out var taskType) ||
                (taskType != TaskType.RequirementsAnalysis && 
                 taskType != TaskType.Implementation && 
                 taskType != TaskType.Documentation && 
                 taskType != TaskType.Delivery))
            {
                return BadRequest(new { success = false, message = "Invalid task type for Expert" });
            }

            // Check if previous stage is approved (except for first stage)
            if (taskType == TaskType.Implementation)
            {
                var lastRequirements = await _taskLogRepository.GetLastTaskLogForProjectAsync(request.ProjectId, TaskType.RequirementsAnalysis);
                if (lastRequirements == null || !lastRequirements.IsApproved)
                {
                    return BadRequest(new { success = false, message = "Requirements Analysis must be approved before Implementation" });
                }
            }
            else if (taskType == TaskType.Documentation)
            {
                var lastImplementation = await _taskLogRepository.GetLastTaskLogForProjectAsync(request.ProjectId, TaskType.Implementation);
                if (lastImplementation == null || !lastImplementation.IsApproved)
                {
                    return BadRequest(new { success = false, message = "Implementation must be approved before Documentation" });
                }
            }
            else if (taskType == TaskType.Delivery)
            {
                var lastDocumentation = await _taskLogRepository.GetLastTaskLogForProjectAsync(request.ProjectId, TaskType.Documentation);
                if (lastDocumentation == null || !lastDocumentation.IsApproved)
                {
                    return BadRequest(new { success = false, message = "Documentation must be approved before Delivery" });
                }
            }

            var taskLog = new TaskLog
            {
                ProjectId = request.ProjectId,
                UserId = currentUserId.Value,
                TaskType = taskType,
                TimeSpent = TimeSpan.FromMinutes(request.Minutes),
                LoggedAt = DateTime.UtcNow,
                IsApproved = false
            };

            await _taskLogRepository.AddAsync(taskLog);

            _logger.LogInformation("Expert {UserId} logged {Minutes} minutes for {TaskType} in project {ProjectId}",
                currentUserId, request.Minutes, taskType, request.ProjectId);

            return Ok(new { success = true, message = "Time logged successfully" });
        }
        else if (currentUserRole == "ProjectManager")
        {
            // Project Manager can approve: ApproveRequirements, ApproveImplementation, ApproveDocumentation, ApproveDelivery
            if (!Enum.TryParse<TaskType>(request.TaskType, out var taskType) ||
                (taskType != TaskType.ApproveRequirements && 
                 taskType != TaskType.ApproveImplementation && 
                 taskType != TaskType.ApproveDocumentation && 
                 taskType != TaskType.ApproveDelivery))
            {
                return BadRequest(new { success = false, message = "Invalid task type for ProjectManager" });
            }

            // Map approval types to corresponding expert tasks
            TaskType correspondingExpertTask = taskType switch
            {
                TaskType.ApproveRequirements => TaskType.RequirementsAnalysis,
                TaskType.ApproveImplementation => TaskType.Implementation,
                TaskType.ApproveDocumentation => TaskType.Documentation,
                TaskType.ApproveDelivery => TaskType.Delivery,
                _ => throw new ArgumentException("Invalid approval type")
            };

            // Check if expert has logged time for this task
            var expertTaskLog = await _taskLogRepository.GetLastTaskLogForProjectAsync(request.ProjectId, correspondingExpertTask);
            if (expertTaskLog == null || !expertTaskLog.IsApproved == false)
            {
                // Find any unapproved log for this task type
                var allLogs = await _taskLogRepository.GetProjectTaskLogsAsync(request.ProjectId);
                expertTaskLog = allLogs.FirstOrDefault(t => t.TaskType == correspondingExpertTask && !t.IsApproved);
                
                if (expertTaskLog == null)
                {
                    return BadRequest(new { success = false, message = $"Expert has not logged time for {correspondingExpertTask}" });
                }
            }

            // Approve the task
            expertTaskLog.IsApproved = true;
            expertTaskLog.ApprovedByUserId = currentUserId.Value;
            expertTaskLog.ApprovedAt = DateTime.UtcNow;
            await _taskLogRepository.UpdateAsync(expertTaskLog);

            _logger.LogInformation("ProjectManager {UserId} approved {TaskType} for project {ProjectId}",
                currentUserId, correspondingExpertTask, request.ProjectId);

            return Ok(new { success = true, message = "Task approved successfully" });
        }

        return Forbid();
    }

    [HttpGet("project/{projectId}/logs")]
    public async Task<ActionResult<IEnumerable<TaskLogDto>>> GetProjectTaskLogs(int projectId)
    {
        var logs = await _taskLogRepository.GetProjectTaskLogsAsync(projectId);
        var project = await _projectRepository.GetByIdAsync(projectId);

        var logDtos = logs.Select(log => new TaskLogDto
        {
            Id = log.Id,
            ProjectId = log.ProjectId,
            ProjectTitle = project?.Title ?? "",
            UserId = log.UserId,
            Username = log.User.Username,
            TaskType = log.TaskType.ToString(),
            TimeSpent = log.TimeSpent,
            LoggedAt = log.LoggedAt,
            IsApproved = log.IsApproved,
            ApprovedBy = log.ApprovedByUser?.Username
        }).ToList();

        return Ok(logDtos);
    }
}
