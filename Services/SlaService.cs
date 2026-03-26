using G2CCRMPortal.Data;
using Microsoft.EntityFrameworkCore;

namespace G2CCRMPortal.Services;

public class SlaService : ISlaService
{
    private readonly G2CCrmDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<SlaService> _logger;

    public SlaService(G2CCrmDbContext db, IEmailService email, ILogger<SlaService> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    /// Checks all unresolved issues and marks those past SLA deadline as breached.
    /// Sends escalation email to department head (Ward officer) once per breach.
    public async Task CheckAndEscalateSlaBreachesAsync()
    {
        try
        {
            var now = DateTime.UtcNow;

            // Get all active issues that haven't been resolved
            var activeStatuses = new[] { "Submitted", "Assigned", "InProgress" };
            var breachedIssues = await _db.IssueRequests
                .Where(i => activeStatuses.Contains(i.Status) 
                         && i.SlaDeadline.HasValue 
                         && i.SlaDeadline < now 
                         && !i.IsBreached)
                .Include(i => i.Ward)
                .Include(i => i.Citizen)
                .Include(i => i.AssignedTo)
                .Include(i => i.Artifact)
                .ToListAsync();

            if (breachedIssues.Count == 0)
            {
                _logger.LogInformation("SLA Check completed. No breaches found.");
                return;
            }

            _logger.LogInformation($"Found {breachedIssues.Count} breached issues. Sending escalations...");

            foreach (var issue in breachedIssues)
            {
                issue.IsBreached = true;
                if(issue.EscalationSentAt.HasValue)
                {
                    // Escalation already sent for this issue, skip to next
                    _logger.LogInformation($"Issue {issue.Id} already marked as breached and escalation sent at {issue.EscalationSentAt.Value}. Skipping.");
                    continue;
                }
                issue.EscalationSentAt = now;

                try
                {
                    // Get department head (Admin or senior officer) for the ward
                    var wardHead = await _db.Users
                        .FirstOrDefaultAsync(u => u.WardId == issue.WardId 
                                              && u.Role == "Officer" 
                                              && u.IsActive);

                    if (wardHead != null && !string.IsNullOrEmpty(wardHead.Email))
                    {
                        await _email.SendSlaEscalationAsync(
                            wardHead.Email,
                            issue.Id.ToString(),
                            issue.Ward.Name,
                            issue.Description,
                            issue.Citizen.Name,
                            (int)Math.Floor((now - issue.SlaDeadline.Value).TotalHours));

                        _logger.LogInformation($"Escalation sent for issue {issue.Id} to {wardHead.Email}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error sending escalation email for issue {issue.Id}: {ex.Message}");
                    // Don't rethrow — continue processing other issues
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation($"SLA check completed. {breachedIssues.Count} issues marked as breached.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Fatal error in SLA check: {ex.Message}");
            throw;
        }
    }

    /// Returns all breached issues with metadata for admin dashboard (shown in red).
    public async Task<List<SlaBreachDto>> GetBreachedIssuesAsync()
    {
        var now = DateTime.UtcNow;

        var breachedIssues = await _db.IssueRequests
            .Where(i => i.IsBreached && i.Status != "Closed")
            .Include(i => i.Artifact)
            .Include(i => i.Ward)
            .Include(i => i.Citizen)
            .Include(i => i.AssignedTo)
            .OrderBy(i => i.EscalationSentAt)
            .Select(i => new SlaBreachDto
            {
                IssueId = i.Id,
                Description = i.Description,
                ArtifactName = i.Artifact.Name,
                WardName = i.Ward.Name,
                CreatedAt = i.CreatedAt,
                SlaDeadline = i.SlaDeadline!.Value,
                Priority = i.Priority,
                Status = i.Status,
                CitizenName = i.Citizen.Name,
                AssignedToName = i.AssignedTo != null ? i.AssignedTo.Name : null,
                EscalationSentAt = i.EscalationSentAt,
                HoursOverdue = (int)Math.Floor((now - i.SlaDeadline!.Value).TotalHours/24.0)
            })
            .ToListAsync();

        return breachedIssues;
    }
}