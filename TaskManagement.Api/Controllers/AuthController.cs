using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Api.Data;
using TaskManagement.Api.DTOs;
using TaskManagement.Api.Interfaces;
using TaskManagement.Api.Middleware;
using TaskManagement.Api.Models;
using TaskManagement.Api.Services;

namespace TaskManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserRepository userRepository, IAuthService authService, ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        
        if (user == null || !_authService.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { success = false, message = "Invalid username or password" });
        }

        var token = _authService.GenerateToken(user);

        _logger.LogInformation("User {Username} logged in successfully", user.Username);

        return Ok(new LoginResponse
        {
            Token = token,
            UserId = user.Id,
            Username = user.Username,
            Role = user.Role?.ToString()
        });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> Register([FromBody] CreateUserRequest request)
    {
        var existingUser = await _userRepository.GetByUsernameAsync(request.Username);
        if (existingUser != null)
        {
            return BadRequest(new { success = false, message = "Username already exists" });
        }

        var user = new User
        {
            Username = request.Username,
            PasswordHash = _authService.HashPassword(request.Password),
            Role = null, // No role assigned initially
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);

        _logger.LogInformation("New user {Username} registered successfully", user.Username);

        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role?.ToString()
        });
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetUserById(int id)
    {
        var user = await _userRepository.GetUserWithProjectsAsync(id);
        if (user == null)
        {
            return NotFound(new { success = false, message = "User not found" });
        }

        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role?.ToString(),
            Projects = user.ProjectUsers.Select(pu => new ProjectRoleDto
            {
                ProjectId = pu.ProjectId,
                ProjectTitle = pu.Project.Title,
                Role = pu.Role.ToString()
            }).ToList()
        };

        return Ok(userDto);
    }
}
