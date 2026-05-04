using Microsoft.AspNetCore.Mvc;

namespace Ticket_System.Controllers;

public class HomeController : Controller
{
    // Login page (default)
    public IActionResult Index()
    {
        return View("~/Views/Auth/Login.cshtml");
    }

    // Admin login (secret route /admin)
    [Route("admin")]
    public IActionResult Admin()
    {
        return View("~/Views/Auth/AdminLogin.cshtml");
    }

    // Dashboard (tickets page — requires session)
    [Route("dashboard")]
    public IActionResult Dashboard()
    {
        return View("~/Views/Tickets/Index.cshtml");
    }

    // Admin panel
    [Route("admin/panel")]
    public IActionResult AdminPanel()
    {
        return View("~/Views/Auth/AdminPanel.cshtml");
    }

    // Ticket detail page
    [Route("ticket/{id:guid}")]
    public IActionResult TicketDetail(Guid id)
    {
        ViewData["TicketId"] = id.ToString();
        return View("~/Views/Tickets/Detail.cshtml");
    }
}