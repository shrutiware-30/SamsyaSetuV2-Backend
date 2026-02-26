using System.Security.Claims;
using G2CCRMPortal.Data;
using G2CCRMPortal.DTOs.IssueRequest;
using G2CCRMPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace G2CCRMPortal.Controllers;

[ApiController]
[Route("api/v1/issues")]
[Authorize]
public class IssueRequestController : ControllerBase
{
    private readonly G2CCrmDbContext _db;

    public IssueRequestController(G2CCrmDbContext db)
    {
        _db = db;
    }

    // POST api/v1/issues — citizen creates a complaint
    [HttpPost]
    [Authorize(Roles = "Citizen")]
    public async Task<IActionResult> Create([FromBody] CreateIssueRequestDto dto)
    {
        var artifact = await _db.Artifacts.FindAsync(dto.ArtifactId);
        if (artifact is null || !artifact.IsActive)
            return BadRequest(new { status = "fail", message = "Invalid artifact." });

        // Resolve nearest ward using Geolib on the client — WardId sent or looked up
        var ward = await _db.Wards
            .OrderBy(w => Math.Abs((double)(w.Latitude - dto.Latitude)) +
                          Math.Abs((double)(w.Longitude - dto.Longitude)))
            .FirstOrDefaultAsync();

        if (ward is null)
            return BadRequest(new { status = "fail", message = "No ward found." });

        var now = DateTime.UtcNow;
        var issue = new IssueRequest
        {
            ArtifactId = dto.ArtifactId,
            CitizenId = GetCurrentUserId(),
            Description = dto.Description,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            LocationText = dto.LocationText,
            ImageUrl = dto.ImageUrl,
            Priority = dto.Priority,
            Status = "Submitted",
            WardId = ward.Id,
            SlaDeadline = now.AddDays(artifact.DefaultSLADays),
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.IssueRequests.Add(issue);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = issue.Id },
            new { status = "success", data = issue.Id });
    }

    // GET api/v1/issues — filtered list
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] int? wardId,
        [FromQuery] string? priority,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        var role = User.FindFirstValue(ClaimTypes.Role)!;
        var userId = GetCurrentUserId();

        var query = _db.IssueRequests.AsQueryable();

        // Citizens see only their own; Officers see their ward; Admin sees all
        query = role switch
        {
            "Citizen" => query.Where(i => i.CitizenId == userId),
            "Officer" => query.Where(i => i.AssignedToId == userId),
            _ => query
        };

        if (!string.IsNullOrEmpty(status)) query = query.Where(i => i.Status == status);
        if (wardId.HasValue) query = query.Where(i => i.WardId == wardId);
        if (!string.IsNullOrEmpty(priority)) query = query.Where(i => i.Priority == priority);

        var total = await query.CountAsync();
        var issues = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Include(i => i.Artifact)
            .Include(i => i.Ward)
            .Include(i => i.Citizen)
            .Include(i => i.AssignedTo)
            .Select(i => new
            {
                i.Id,
                i.Description,
                Artifact = i.Artifact.Name,
                i.Status,
                i.Priority,
                i.Latitude,
                i.Longitude,
                i.LocationText,
                i.ImageUrl,
                i.SlaDeadline,
                i.IsBreached,
                Ward = i.Ward.Name,
                i.CitizenId,
                CitizenName = i.Citizen.Name,
                OfficerName = i.AssignedTo != null ? i.AssignedTo.Name : null,
                i.CreatedAt,
                i.UpdatedAt
            })
            .ToListAsync();

        return Ok(new { status = "success", results = issues.Count, total, data = issues });
    }

    // GET api/v1/issues/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var issue = await _db.IssueRequests
            .Include(i => i.Artifact)
            .Include(i => i.Ward)
            .Include(i => i.Citizen)
            .Include(i => i.AssignedTo)
            .Include(i => i.TrackRequests.OrderByDescending(t => t.ChangedAt))
            .Include(i => i.Feedback)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (issue is null)
            return NotFound(new { status = "fail", message = "Issue not found." });

        return Ok(new { status = "success", data = issue });
    }

    // PATCH api/v1/issues/{id}/assign — Admin assigns officer
    [HttpPatch("{id:guid}/assign")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignOfficer(Guid id, [FromBody] AssignOfficerDto dto)
    {
        var issue = await _db.IssueRequests.FindAsync(id);
        if (issue is null) return NotFound();

        var officer = await _db.Users.FindAsync(dto.OfficerId);
        if (officer is null || officer.Role != "Officer" || !officer.IsActive)
            return BadRequest(new { status = "fail", message = "Invalid officer." });

        var oldStatus = issue.Status;
        issue.AssignedToId = dto.OfficerId;
        issue.Status = "Assigned";
        issue.UpdatedAt = DateTime.UtcNow;

        _db.TrackRequests.Add(new TrackRequest
        {
            IssueRequestId = issue.Id,
            ChangedByUserId = GetCurrentUserId(),
            OldStatus = oldStatus,
            NewStatus = "Assigned",
            Remarks = $"Assigned to officer {officer.Name}",
            ChangedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return Ok(new { status = "success", message = "Officer assigned." });
    }

    // PATCH api/v1/issues/{id}/status — Officer/Admin updates status
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Officer,Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateIssueStatusDto dto)
    {
        string[] allowed = ["Submitted", "Assigned", "InProgress", "Resolved", "Closed"];
        if (!allowed.Contains(dto.NewStatus))
            return BadRequest(new { status = "fail", message = "Invalid status value." });

        var issue = await _db.IssueRequests.FindAsync(id);
        if (issue is null) return NotFound();

        var oldStatus = issue.Status;
        issue.Status = dto.NewStatus;
        issue.UpdatedAt = DateTime.UtcNow;

        _db.TrackRequests.Add(new TrackRequest
        {
            IssueRequestId = issue.Id,
            ChangedByUserId = GetCurrentUserId(),
            OldStatus = oldStatus,
            NewStatus = dto.NewStatus,
            Remarks = dto.Remarks,
            ChangedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return Ok(new { status = "success", message = $"Status changed to {dto.NewStatus}." });
    }

    // GET api/v1/issues/{id}/track — full audit trail
    [HttpGet("{id:guid}/track")]
    public async Task<IActionResult> GetTrackHistory(Guid id)
    {
        var tracks = await _db.TrackRequests
            .Where(t => t.IssueRequestId == id)
            .OrderByDescending(t => t.ChangedAt)
            .Select(t => new
            {
                t.Id,
                t.OldStatus,
                t.NewStatus,
                t.Remarks,
                ChangedBy = t.ChangedByUser.Name,
                t.ChangedAt
            })
            .ToListAsync();

        return Ok(new { status = "success", results = tracks.Count, data = tracks });
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}