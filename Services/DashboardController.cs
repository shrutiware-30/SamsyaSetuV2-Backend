using System.Security.Claims;
using G2CCRMPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace G2CCRMPortal.Controllers;

[ApiController]
[Route("api/v1/issues/dashboard")]
[Authorize(Roles = "Admin,Officer")]
public class DashboardController : ControllerBase
{
    private readonly ISlaService _slaService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ISlaService slaService, ILogger<DashboardController> logger)
    {
        _slaService = slaService;
        _logger = logger;
    }

    /// GET /api/v1/issues/dashboard/breached
    /// Returns all SLA-breached issues (shown in red on admin dashboard).
    /// Only accessible to Admin and Officer roles.
    [HttpGet("breached")]
    public async Task<IActionResult> GetBreachedIssues()
    {
        var breachedIssues = await _slaService.GetBreachedIssuesAsync();
        
        return Ok(new
        {
            status = "success",
            total = breachedIssues.Count,
            data = breachedIssues
        });
    }

    /// GET /api/v1/issues/dashboard/breached/count
    /// Returns count of breached issues for notification badge.
    [HttpGet("breached/count")]
    public async Task<IActionResult> GetBreachedCount()
    {
        var breachedIssues = await _slaService.GetBreachedIssuesAsync();
        
        return Ok(new
        {
            status = "success",
            breachedCount = breachedIssues.Count
        });
    }

    /// GET /api/v1/issues/dashboard/stats
    /// Returns dashboard statistics (total, breached, pending, etc.).
    [HttpGet("stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var breachedIssues = await _slaService.GetBreachedIssuesAsync();
        var pendingCount = breachedIssues.Count(i => i.Status != "Resolved");
        var criticalCount = breachedIssues.Count(i => i.Priority == "High");

        return Ok(new
        {
            status = "success",
            data = new
            {
                totalBreached = breachedIssues.Count,
                pending = pendingCount,
                critical = criticalCount,
                allBreached = breachedIssues
            }
        });
    }
}