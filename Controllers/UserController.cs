using System.Security.Claims;
using G2CCRMPortal.Data;
using G2CCRMPortal.DTOs.Auth;
using G2CCRMPortal.DTOs.User;
using G2CCRMPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace G2CCRMPortal.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly G2CCrmDbContext _db;

    public UserController(G2CCrmDbContext db)
    {
        _db = db;
    }

    // GET api/v1/users/me — mirrors: router.get('/me', getme, getUser)
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var user = await _db.Users
            .Include(u => u.Ward)
            .FirstOrDefaultAsync(u => u.Id == GetCurrentUserId() && u.IsActive);

        if (user is null) return NotFound(new { status = "fail", message = "User not found." });

        return Ok(new { status = "success", data = MapUser(user) });
    }

    // GET api/v1/users/dashboard — Officer dashboard with assigned issues stats
    [HttpGet("dashboard")]
    [Authorize(Roles = "Officer")]
    public async Task<IActionResult> GetOfficerDashboard()
    {
        var userId = GetCurrentUserId();
        var user = await _db.Users.FindAsync(userId);

        if (user is null || user.Role != "Officer")
            return Forbid();

        // Get officer's assigned issues
        var assignedIssues = await _db.IssueRequests
            .Where(i => i.AssignedToId == userId)
            .ToListAsync();

        var dashboard = new
        {
            OfficerId = user.Id,
            OfficerName = user.Name,
            Ward = user.Ward?.Name,
            TotalAssigned = assignedIssues.Count,
            Assigned = assignedIssues.Count(i => i.Status == "Assigned"),
            InProgress = assignedIssues.Count(i => i.Status == "InProgress"),
            Resolved = assignedIssues.Count(i => i.Status == "Resolved"),
            ClosedByAdmin = assignedIssues.Count(i => i.Status == "Closed"),
            BreachedSLA = assignedIssues.Count(i => i.IsBreached)
        };

        return Ok(new { status = "success", data = dashboard });
    }

    // PATCH api/v1/users/updateMe — mirrors: router.patch('/updateMe', ...)
    [HttpPatch("updateMe")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateMeDto dto)
    {
        var user = await _db.Users.FindAsync(GetCurrentUserId());
        if (user is null) return NotFound();

        if (dto.Name is not null) user.Name = dto.Name;
        if (dto.Email is not null) user.Email = dto.Email;
        if (dto.MobileNumber is not null) user.MobileNumber = dto.MobileNumber;

        await _db.SaveChangesAsync();

        return Ok(new { status = "success", data = MapUser(user) });
    }

    // DELETE api/v1/users/deleteMe — mirrors: router.delete('/deleteMe', deleteme)
    [HttpDelete("deleteMe")]
    public async Task<IActionResult> DeleteMe()
    {
        var user = await _db.Users.FindAsync(GetCurrentUserId());
        if (user is null) return NotFound();

        user.IsActive = false; // soft delete
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ── Admin-only routes ─────────────────────────────────────
    // mirrors: router.use(authController.restrictTo('admin'))

    // GET api/v1/users — mirrors: router.get('/', getAllUsers)
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? role,
        [FromQuery] int? wardId,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        var query = _db.Users.Where(u => u.IsActive);

        if (!string.IsNullOrEmpty(role)) query = query.Where(u => u.Role == role);
        if (wardId.HasValue) query = query.Where(u => u.WardId == wardId);

        var total = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Include(u => u.Ward)
            .ToListAsync();

        return Ok(new
        {
            status = "success",
            results = users.Count,
            total,
            data = users.Select(MapUser)
        });
    }

    // GET api/v1/users/{id} — mirrors: router.get('/:id', getUser)
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _db.Users.Include(u => u.Ward)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null) return NotFound(new { status = "fail", message = "User not found." });

        return Ok(new { status = "success", data = MapUser(user) });
    }

    // PATCH api/v1/users/{id} — mirrors: router.patch('/:id', updateuser)
    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateMeDto dto)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        if (dto.Name is not null) user.Name = dto.Name;
        if (dto.Email is not null) user.Email = dto.Email;
        if (dto.MobileNumber is not null) user.MobileNumber = dto.MobileNumber;

        await _db.SaveChangesAsync();

        return Ok(new { status = "success", data = MapUser(user) });
    }

    // DELETE api/v1/users/{id} — mirrors: router.delete('/:id', deleteUser)
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null) return NotFound();

        user.IsActive = false;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ── helpers ───────────────────────────────────────────────

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static UserSummaryDto MapUser(User u) => new()
    {
        Id = u.Id,
        Name = u.Name,
        Email = u.Email,
        MobileNumber = u.MobileNumber,
        Role = u.Role,
        WardId = u.WardId
    };
}