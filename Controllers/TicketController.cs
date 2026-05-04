using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticket_System.Data;
using Ticket_System.Models;

namespace Ticket_System.Controllers;

[ApiController]
[Route("api/tickets")]
public class TicketController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;

    public TicketController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    private static readonly Dictionary<string, int> SlaHours = new()
    {
        { "Baixa", 24 },
        { "Média", 8 },
        { "Alta", 4 }
    };

    private Guid? GetCurrentUserId()
    {
        var id = HttpContext.Session.GetString("UserId");
        return string.IsNullOrEmpty(id) ? null : Guid.Parse(id);
    }

    private string GetCurrentRole()
    {
        return HttpContext.Session.GetString("Role") ?? "";
    }

    private string GetUserDisplayName(Guid userId)
    {
        var user = _context.Users.Find(userId);
        return user?.DisplayName ?? "Desconhecido";
    }

    private async Task<string?> SaveUploadedFile(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
        if (!allowed.Contains(ext)) return null;

        var fileName = Guid.NewGuid().ToString() + ext;
        var filePath = Path.Combine(uploadsDir, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return "/uploads/" + fileName;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTicket([FromForm] CreateTicketRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var imagePath = await SaveUploadedFile(request.Image);

        var ticket = new Ticket(request.Title, request.Description, request.Sector, request.Priority, userId.Value, imagePath);

        _context.Tickets.Add(ticket);
        _context.SaveChanges();

        return StatusCode(201, MapTicketResponse(ticket));
    }

    [HttpPut("{id:guid}/start")]
    public IActionResult StartAttendance(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var ticket = _context.Tickets.Find(id);
        if (ticket == null) return NotFound("Chamado não encontrado.");

        try
        {
            ticket.StartAttendance(DateTime.UtcNow, userId.Value);
            _context.SaveChanges();
            return Ok(MapTicketResponse(ticket));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpPut("{id:guid}/finish")]
    public IActionResult FinishAttendance(Guid id, [FromBody] FinishTicketRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var ticket = _context.Tickets.Find(id);
        if (ticket == null) return NotFound("Chamado não encontrado.");

        try
        {
            ticket.FinishAttendance(request.Solution, DateTime.UtcNow, userId.Value);
            _context.SaveChanges();
            return Ok(MapTicketResponse(ticket));
        }
        catch (Exception ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpPut("{id:guid}/cancel")]
    public IActionResult CancelTicket(Guid id, [FromBody] CancelTicketRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var ticket = _context.Tickets.Find(id);
        if (ticket == null) return NotFound("Chamado não encontrado.");

        try
        {
            ticket.CancelTicket(request.Reason, DateTime.UtcNow, userId.Value);
            _context.SaveChanges();
            return Ok(MapTicketResponse(ticket));
        }
        catch (Exception ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetTicketById(Guid id)
    {
        var ticket = _context.Tickets.Find(id);
        if (ticket == null) return NotFound("Chamado não encontrado.");
        return Ok(MapTicketResponse(ticket));
    }

    [HttpGet]
    public IActionResult GetAllTickets()
    {
        var userId = GetCurrentUserId();
        var role = GetCurrentRole();

        var query = _context.Tickets.AsQueryable();

        if (role == "Usuario" && userId.HasValue)
        {
            query = query.Where(t => t.CreatedByUserId == userId.Value);
        }

        var tickets = query.OrderByDescending(t => t.CreatedAt).ToList();
        var response = tickets.Select(MapTicketResponse);
        return Ok(response);
    }

    // ── Comments ──
    [HttpGet("{id:guid}/comments")]
    public IActionResult GetComments(Guid id)
    {
        var comments = _context.TicketComments
            .Where(c => c.TicketId == id)
            .OrderBy(c => c.CreatedAt)
            .ToList();

        var result = comments.Select(c => new
        {
            c.Id,
            c.TicketId,
            c.UserId,
            UserName = GetUserDisplayName(c.UserId),
            c.Content,
            c.ImagePath,
            c.CreatedAt
        });

        return Ok(result);
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid id, [FromForm] AddCommentRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var ticket = _context.Tickets.Find(id);
        if (ticket == null) return NotFound("Chamado não encontrado.");

        var imagePath = await SaveUploadedFile(request.Image);

        var comment = new TicketComment
        {
            Id = Guid.NewGuid(),
            TicketId = id,
            UserId = userId.Value,
            Content = request.Content ?? "",
            ImagePath = imagePath,
            CreatedAt = DateTime.UtcNow
        };

        _context.TicketComments.Add(comment);
        _context.SaveChanges();

        return StatusCode(201, new
        {
            comment.Id,
            comment.TicketId,
            comment.UserId,
            UserName = GetUserDisplayName(userId.Value),
            comment.Content,
            comment.ImagePath,
            comment.CreatedAt
        });
    }

    private object MapTicketResponse(Ticket t)
    {
        var totalTime = t.FinishedAt.HasValue && t.StartedAt.HasValue
            ? (t.FinishedAt.Value - t.StartedAt.Value).TotalHours
            : (t.StartedAt.HasValue ? (DateTime.UtcNow - t.StartedAt.Value).TotalHours : 0);

        var limitHours = SlaHours.TryGetValue(t.Priority, out var hours) ? hours : 24;
        var isSlaBreached = totalTime > limitHours;

        return new
        {
            t.Id,
            t.Title,
            t.Description,
            t.Sector,
            t.Priority,
            Status = t.Status.ToString(),
            TotalTimeHours = Math.Round(totalTime, 2),
            SlaLimitHours = limitHours,
            SlaBreached = isSlaBreached,
            t.CreatedAt,
            t.StartedAt,
            t.FinishedAt,
            t.ResolutionNotes,
            t.ImagePath,
            t.CreatedByUserId,
            CreatedByName = GetUserDisplayName(t.CreatedByUserId),
            t.StartedByUserId,
            StartedByName = t.StartedByUserId.HasValue ? GetUserDisplayName(t.StartedByUserId.Value) : null,
            t.FinishedByUserId,
            FinishedByName = t.FinishedByUserId.HasValue ? GetUserDisplayName(t.FinishedByUserId.Value) : null,
            t.CancelledByUserId,
            CancelledByName = t.CancelledByUserId.HasValue ? GetUserDisplayName(t.CancelledByUserId.Value) : null
        };
    }
}

public class CreateTicketRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public IFormFile? Image { get; set; }
}

public class FinishTicketRequest
{
    public string Solution { get; set; } = string.Empty;
}

public class CancelTicketRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class AddCommentRequest
{
    public string? Content { get; set; }
    public IFormFile? Image { get; set; }
}