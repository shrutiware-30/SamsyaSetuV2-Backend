using System.Security.Claims;
using G2CCRMPortal.Data;
using G2CCRMPortal.DTOs.IssueRequest;
using G2CCRMPortal.Models;
using G2CCRMPortal.Services;
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
    private readonly IUploadService _uploadService;

    public IssueRequestController(G2CCrmDbContext db, IUploadService uploadService)
    {
        _db = db;
        _uploadService = uploadService;
    }

    // POST api/v1/issues — citizen creates a complaint
    [HttpPost]
    [Authorize(Roles = "Citizen")]
    public async Task<IActionResult> Create([FromForm] CreateIssueRequestDto dto)
    {
        var artifact = await _db.Artifacts.FindAsync(dto.ArtifactId);
        if (artifact is null || !artifact.IsActive)
            return BadRequest(new { status = "fail", message = "Invalid artifact." });

        var ward = await _db.Wards
            .OrderBy(w => Math.Abs((double)(w.Latitude - dto.Latitude)) +
                          Math.Abs((double)(w.Longitude - dto.Longitude)))
            .FirstOrDefaultAsync();

        if (ward is null)
            return BadRequest(new { status = "fail", message = "No ward found." });

        // Handle file upload if provided
        string? imageUrl = null;
        if (dto.ImageFile != null)
        {
            try
            {
                imageUrl = await _uploadService.UploadImageAsync(dto.ImageFile);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { status = "fail", message = ex.Message });
            }
        }

        var now = DateTime.UtcNow;
        var issue = new IssueRequest
        {
            ArtifactId = dto.ArtifactId,
            CitizenId = GetCurrentUserId(),
            Description = dto.Description,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            LocationText = dto.LocationText,
            ImageUrl = imageUrl,
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

        // Citizens see only their own issues
        // Officers see only issues assigned to them
        // Admin sees all
        if (role == "Citizen")
        {
            query = query.Where(i => i.CitizenId == userId);
        }
        else if (role == "Officer")
        {
            // Officers see only issues explicitly assigned to them
            query = query.Where(i => i.AssignedToId == userId);
        }
        // Admin gets all (no filter)

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
                AssignedToId = i.AssignedToId,
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

    /// <summary>
    /// GET /api/v1/issues/nearby?latitude=..&amp;longitude=..&amp;radiusMeters=200&amp;artifactId=..
    /// Returns active issues of the same artifact type within the given radius.
    /// </summary>
    [HttpGet("nearby")]
    [Authorize(Roles = "Citizen")]
    public async Task<IActionResult> GetNearbyIssues(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] int radiusMeters = 200,
        [FromQuery] int artifactId = 0)
    {
        if (artifactId <= 0)
            return BadRequest(new { status = "fail", message = "artifactId is required." });

        string[] activeStatuses = ["Submitted", "Assigned", "InProgress"];

        var nearbyIssues = await _db.IssueRequests
            .Where(i => i.ArtifactId == artifactId
                     && activeStatuses.Contains(i.Status))
            .Select(i => new
            {
                i.Id,
                i.Description,
                i.Status,
                i.Priority,
                i.Latitude,
                i.Longitude,
                i.LocationText,
                i.CreatedAt,
                Artifact = i.Artifact.Name,
                Category = i.Artifact.Category,
                DistanceMeters = 6371000.0 * 2.0 * Math.Atan2(
                    Math.Sqrt(
                        Math.Pow(Math.Sin(((double)i.Latitude - latitude) * Math.PI / 180.0 / 2.0), 2) +
                        Math.Cos(latitude * Math.PI / 180.0) *
                        Math.Cos((double)i.Latitude * Math.PI / 180.0) *
                        Math.Pow(Math.Sin(((double)i.Longitude - longitude) * Math.PI / 180.0 / 2.0), 2)
                    ),
                    Math.Sqrt(
                        1.0 - (
                            Math.Pow(Math.Sin(((double)i.Latitude - latitude) * Math.PI / 180.0 / 2.0), 2) +
                            Math.Cos(latitude * Math.PI / 180.0) *
                            Math.Cos((double)i.Latitude * Math.PI / 180.0) *
                            Math.Pow(Math.Sin(((double)i.Longitude - longitude) * Math.PI / 180.0 / 2.0), 2)
                        )
                    )
                )
            })
            .Where(i => i.DistanceMeters <= radiusMeters)
            .OrderBy(i => i.DistanceMeters)
            .ToListAsync();

        return Ok(new { status = "success", results = nearbyIssues.Count, data = nearbyIssues });
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

    // PATCH api/v1/issues/{id}/status — Officer updates status (Assigned → InProgress → Resolved)
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Officer")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateIssueStatusDto dto)
    {
        string[] allowed = ["Assigned", "InProgress", "Resolved"];
        if (!allowed.Contains(dto.NewStatus))
            return BadRequest(new { status = "fail", message = "Invalid status value." });

        var issue = await _db.IssueRequests.FindAsync(id);
        if (issue is null) return NotFound();

        // Ensure officer can only update their own assigned issues
        var userId = GetCurrentUserId();
        if (issue.AssignedToId != userId)
            return Forbid();

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

    // PATCH api/v1/issues/{id}/close — Admin closes issue (final step)
    [HttpPatch("{id:guid}/close")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CloseIssue(Guid id, [FromBody] UpdateIssueStatusDto dto)
    {
        var issue = await _db.IssueRequests.FindAsync(id);
        if (issue is null) return NotFound(new { status = "fail", message = "Issue not found." });

        // Only allow closing if already Resolved
        if (issue.Status != "Resolved")
            return BadRequest(new { status = "fail", message = "Issue must be Resolved before closing." });

        var oldStatus = issue.Status;
        issue.Status = "Closed";
        issue.UpdatedAt = DateTime.UtcNow;

        _db.TrackRequests.Add(new TrackRequest
        {
            IssueRequestId = issue.Id,
            ChangedByUserId = GetCurrentUserId(),
            OldStatus = oldStatus,
            NewStatus = "Closed",
            Remarks = dto.Remarks ?? "Issue closed by admin.",
            ChangedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return Ok(new { status = "success", message = "Issue closed." });
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
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier!));
}