using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Api.DTOs;
using TaskManagement.Api.Interfaces;
using TaskManagement.Api.Middleware;
using TaskManagement.Api.Models;

namespace TaskManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SystemAdmin")]
public class ReportsController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ITaskLogRepository _taskLogRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IUserRepository userRepository,
        ITaskLogRepository taskLogRepository,
        IProjectRepository projectRepository,
        ILogger<ReportsController> logger)
    {
        _userRepository = userRepository;
        _taskLogRepository = taskLogRepository;
        _projectRepository = projectRepository;
        _logger = logger;
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
    {
        var users = await _userRepository.GetAllAsync();
        
        var userDtos = new List<UserDto>();
        foreach (var user in users)
        {
            var userWithProjects = await _userRepository.GetUserWithProjectsAsync(user.Id);
            userDtos.Add(new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role?.ToString(),
                Projects = userWithProjects?.ProjectUsers.Select(pu => new ProjectRoleDto
                {
                    ProjectId = pu.ProjectId,
                    ProjectTitle = pu.Project.Title,
                    Role = pu.Role.ToString()
                }).ToList()
            });
        }

        return Ok(userDtos);
    }

    [HttpGet("experts-time")]
    public async Task<ActionResult<IEnumerable<ExpertTimeReportDto>>> GetExpertsTimeReport()
    {
        var experts = await _userRepository.GetExpertsAsync();
        var reports = new List<ExpertTimeReportDto>();

        foreach (var expert in experts)
        {
            var taskLogs = await _taskLogRepository.GetUserTaskLogsAsync(expert.Id);
            
            var report = new ExpertTimeReportDto
            {
                UserId = expert.Id,
                Username = expert.Username,
                TotalTime = taskLogs.Sum(t => t.TimeSpent),
                ProjectTimes = taskLogs
                    .GroupBy(t => t.ProjectId)
                    .Select(g => new ProjectTimeDto
                    {
                        ProjectId = g.Key,
                        ProjectTitle = g.First().Project.Title,
                        TotalTime = g.Sum(t => t.TimeSpent),
                        TaskTimes = g
                            .GroupBy(t => t.TaskType)
                            .Select(tg => new TaskTimeDto
                            {
                                TaskType = tg.Key.ToString(),
                                TimeSpent = tg.Sum(t => t.TimeSpent)
                            }).ToList()
                    }).ToList()
            };

            reports.Add(report);
        }

        return Ok(reports);
    }

    [HttpGet("projects-status")]
    public async Task<ActionResult<IEnumerable<ProjectStatusDto>>> GetProjectsStatus()
    {
        var projects = await _projectRepository.GetAllAsync();
        var statusReports = new List<ProjectStatusDto>();

        foreach (var project in projects)
        {
            var taskLogs = await _taskLogRepository.GetProjectTaskLogsAsync(project.Id);
            
            // Find the last approval done by project manager
            var lastApproval = taskLogs
                .Where(t => t.IsApproved && t.ApprovedAt.HasValue)
                .OrderByDescending(t => t.ApprovedAt)
                .FirstOrDefault();

            string? lastApprovalStatus = null;
            if (lastApproval != null)
            {
                lastApprovalStatus = lastApproval.TaskType switch
                {
                    TaskType.RequirementsAnalysis => "Requirements Approved",
                    TaskType.Implementation => "Implementation Approved",
                    TaskType.Documentation => "Documentation Approved",
                    TaskType.Delivery => "Delivery Approved",
                    _ => lastApproval.TaskType.ToString()
                };
            }

            statusReports.Add(new ProjectStatusDto
            {
                ProjectId = project.Id,
                ProjectTitle = project.Title,
                LastApproval = lastApprovalStatus
            });
        }

        return Ok(statusReports);
    }
}
