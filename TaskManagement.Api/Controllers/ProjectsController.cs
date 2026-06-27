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
public class ProjectsController : ControllerBase
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectUserRepository _projectUserRepository;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(
        IProjectRepository projectRepository,
        IUserRepository userRepository,
        IProjectUserRepository projectUserRepository,
        ILogger<ProjectsController> logger)
    {
        _projectRepository = projectRepository;
        _userRepository = userRepository;
        _projectUserRepository = projectUserRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProjectDto>>> GetProjects([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var allProjects = await _projectRepository.GetAllAsync();
        var projects = allProjects.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        var totalCount = allProjects.Count();

        var projectDtos = projects.Select(p => new ProjectDto
        {
            Id = p.Id,
            Title = p.Title,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            Budget = p.Budget,
            Customer = p.Customer
        }).ToList();

        return Ok(new PagedResult<ProjectDto>
        {
            Items = projectDtos,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProjectDto>> GetProject(int id)
    {
        var project = await _projectRepository.GetProjectWithUsersAsync(id);
        if (project == null)
        {
            return NotFound(new { success = false, message = "Project not found" });
        }

        var projectDto = new ProjectDto
        {
            Id = project.Id,
            Title = project.Title,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Budget = project.Budget,
            Customer = project.Customer,
            Users = project.ProjectUsers.Select(pu => new ProjectUserDto
            {
                UserId = pu.UserId,
                Username = pu.User.Username,
                Role = pu.Role.ToString()
            }).ToList()
        };

        return Ok(projectDto);
    }

    [HttpPost]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult<ProjectDto>> CreateProject([FromBody] CreateProjectRequest request)
    {
        var project = new Project
        {
            Title = request.Title,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Budget = request.Budget,
            Customer = request.Customer,
            CreatedAt = DateTime.UtcNow
        };

        await _projectRepository.AddAsync(project);

        _logger.LogInformation("Project {ProjectTitle} created by SystemAdmin", project.Title);

        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, new ProjectDto
        {
            Id = project.Id,
            Title = project.Title,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            Budget = project.Budget,
            Customer = project.Customer
        });
    }

    [HttpPost("{projectId}/assign-user")]
    [Authorize(Roles = "SystemAdmin")]
    public async Task<ActionResult> AssignUserToProject(int projectId, [FromBody] AssignUserToProjectRequest request)
    {
        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project == null)
        {
            return NotFound(new { success = false, message = "Project not found" });
        }

        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null)
        {
            return NotFound(new { success = false, message = "User not found" });
        }

        // Only ProjectManager or Expert roles can be assigned
        if (!Enum.TryParse<UserRole>(request.Role, out var role) || 
            (role != UserRole.ProjectManager && role != UserRole.Expert))
        {
            return BadRequest(new { success = false, message = "Invalid role. Only ProjectManager or Expert roles can be assigned." });
        }

        // Check if user already has a role in this project
        var existingAssignment = await _projectUserRepository.GetUserProjectRoleAsync(request.UserId, projectId);
        if (existingAssignment != null)
        {
            return BadRequest(new { success = false, message = "User already has a role in this project" });
        }

        var projectUser = new ProjectUser
        {
            UserId = request.UserId,
            ProjectId = projectId,
            Role = role
        };

        await _projectUserRepository.AddAsync(projectUser);

        _logger.LogInformation("User {Username} assigned as {Role} to project {ProjectTitle}", 
            user.Username, role, project.Title);

        return Ok(new { success = true, message = "User assigned successfully" });
    }
}
