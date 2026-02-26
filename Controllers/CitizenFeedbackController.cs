using System.Security.Claims;
using G2CCRMPortal.Data;
using G2CCRMPortal.DTOs.Feedback;
using G2CCRMPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace G2CCRMPortal.Controllers;

[ApiController]
[Route("api/v1/feedback")]
[Authorize]
public class CitizenFeedbackController : ControllerBase
{
    private readonly G2CCrmDbContext _db;

    public CitizenFeedbackController(G2CCrmDbContext db)
    {
        _db = db;
    }

    // POST api/v1/feedback — citizen submits rating after resolution
    [HttpPost]
    [Authorize(Roles = "Citizen")]
    public async Task<IActionResult> Create([FromBody] CreateFeedbackDto dto)
    {
        var issue = await _db.IssueRequests.FindAsync(dto.IssueRequestId);
        if (issue is null)
            return NotFound(new { status = "fail", message = "Issue not found." });

        if (issue.CitizenId != GetCurrentUserId())
            return StatusCode(403, new { status = "fail", message = "Not your complaint." });

        if (issue.Status is not ("Resolved" or "Closed"))
            return BadRequest(new { status = "fail", message = "Feedback only for resolved issues." });

        if (await _db.CitizenFeedbacks.AnyAsync(f => f.IssueRequestId == dto.IssueRequestId))
            return BadRequest(new { status = "fail", message = "Feedback already submitted." });

        var feedback = new CitizenFeedback
        {
            IssueRequestId = dto.IssueRequestId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            SubmittedAt = DateTime.UtcNow
        };

        _db.CitizenFeedbacks.Add(feedback);
        await _db.SaveChangesAsync();

        return CreatedAtAction(null, new { status = "success", data = feedback });
    }

    // GET api/v1/feedback?issueId={id} — get feedback for an issue
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid? issueId)
    {
        var query = _db.CitizenFeedbacks.AsQueryable();

        if (issueId.HasValue)
            query = query.Where(f => f.IssueRequestId == issueId.Value);

        var feedbacks = await query
            .OrderByDescending(f => f.SubmittedAt)
            .Select(f => new
            {
                f.Id,
                f.IssueRequestId,
                f.Rating,
                f.Comment,
                f.SubmittedAt
            })
            .ToListAsync();

        return Ok(new { status = "success", results = feedbacks.Count, data = feedbacks });
    }

    private Guid GetCurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}