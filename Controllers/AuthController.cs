using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket_System.Data;
using Ticket_System.Models;

namespace Ticket_System.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var hash = AppDbContext.HashPassword(request.Password);
        var user = _context.Users.FirstOrDefault(u =>
            u.Username == request.Username &&
            u.PasswordHash == hash &&
            u.IsActive);

        if (user == null)
            return Unauthorized(new { erro = "Usuário ou senha inválidos." });

        HttpContext.Session.SetString("UserId", user.Id.ToString());
        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetString("DisplayName", user.DisplayName);
        HttpContext.Session.SetString("Role", user.Role.ToString());

        return Ok(new
        {
            user.Id,
            user.Username,
            user.DisplayName,
            Role = user.Role.ToString()
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Logout realizado com sucesso." });
    }

    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { erro = "Não autenticado." });

        return Ok(new
        {
            Id = userId,
            Username = HttpContext.Session.GetString("Username"),
            DisplayName = HttpContext.Session.GetString("DisplayName"),
            Role = HttpContext.Session.GetString("Role")
        });
    }

    // ── Admin: list users ──
    [HttpGet("users")]
    public IActionResult GetAllUsers()
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Admin")
            return Forbid();

        var users = _context.Users
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.DisplayName,
                Role = u.Role.ToString(),
                u.IsActive,
                u.CreatedAt
            })
            .ToList();

        return Ok(users);
    }

    // ── Admin: create user with role ──
    [HttpPost("users")]
    public IActionResult CreateUser([FromBody] CreateUserRequest request)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Admin")
            return Forbid();

        if (_context.Users.Any(u => u.Username == request.Username))
            return BadRequest(new { erro = "Nome de usuário já existe." });

        UserRole userRole = UserRole.Usuario;
        if (!string.IsNullOrEmpty(request.Role))
        {
            if (!Enum.TryParse<UserRole>(request.Role, true, out userRole))
                userRole = UserRole.Usuario;

            if (userRole == UserRole.Admin)
                return BadRequest(new { erro = "Não é possível criar outro administrador." });
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = AppDbContext.HashPassword(request.Password),
            DisplayName = request.DisplayName,
            Role = userRole,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        return StatusCode(201, new
        {
            user.Id,
            user.Username,
            user.DisplayName,
            Role = user.Role.ToString(),
            user.IsActive,
            user.CreatedAt
        });
    }

    // ── Admin: toggle user active/inactive ──
    [HttpPut("users/{id:guid}/toggle-active")]
    public IActionResult ToggleUserActive(Guid id)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Admin")
            return Forbid();

        var user = _context.Users.Find(id);
        if (user == null)
            return NotFound(new { erro = "Usuário não encontrado." });

        if (user.Role == UserRole.Admin)
            return BadRequest(new { erro = "Não é possível desativar o administrador." });

        user.IsActive = !user.IsActive;
        _context.SaveChanges();

        return Ok(new
        {
            user.Id,
            user.IsActive,
            message = user.IsActive ? "Usuário ativado." : "Usuário desativado."
        });
    }

    // ── Admin: update user role ──
    [HttpPut("users/{id:guid}/role")]
    public IActionResult UpdateUserRole(Guid id, [FromBody] UpdateRoleRequest request)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Admin")
            return Forbid();

        var user = _context.Users.Find(id);
        if (user == null)
            return NotFound(new { erro = "Usuário não encontrado." });

        if (user.Role == UserRole.Admin)
            return BadRequest(new { erro = "Não é possível alterar o perfil do administrador." });

        if (!Enum.TryParse<UserRole>(request.Role, true, out var newRole))
            return BadRequest(new { erro = "Perfil inválido." });

        if (newRole == UserRole.Admin)
            return BadRequest(new { erro = "Não é possível promover a administrador." });

        user.Role = newRole;
        _context.SaveChanges();

        return Ok(new
        {
            user.Id,
            Role = user.Role.ToString(),
            message = "Perfil atualizado para " + user.Role
        });
    }

    // ── Admin: delete user ──
    [HttpDelete("users/{id:guid}")]
    public IActionResult DeleteUser(Guid id)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Admin")
            return Forbid();

        var user = _context.Users.Find(id);
        if (user == null)
            return NotFound(new { erro = "Usuário não encontrado." });

        if (user.Role == UserRole.Admin)
            return BadRequest(new { erro = "Não é possível remover o administrador." });

        _context.Users.Remove(user);
        _context.SaveChanges();

        return Ok(new { message = "Usuário removido com sucesso." });
    }

    // ── Resolve user name by ID ──
    [HttpGet("users/{id:guid}/name")]
    public IActionResult GetUserName(Guid id)
    {
        var user = _context.Users.Find(id);
        if (user == null)
            return Ok(new { displayName = "Desconhecido" });

        return Ok(new { user.DisplayName, Role = user.Role.ToString() });
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class UpdateRoleRequest
{
    public string Role { get; set; } = string.Empty;
}
