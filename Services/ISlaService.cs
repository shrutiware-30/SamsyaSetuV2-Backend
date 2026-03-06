namespace G2CCRMPortal.Services;

public interface ISlaService
{
    /// <summary>
    /// Checks all active issues for SLA breaches and sends escalation notifications.
    /// Runs every 24 hours.
    /// </summary>
    Task CheckAndEscalateSlaBreachesAsync();

    /// <summary>
    /// Gets breached/escalated issues for admin dashboard.
    /// </summary>
    Task<List<SlaBreachDto>> GetBreachedIssuesAsync();
}

public class SlaBreachDto
{
    public Guid IssueId { get; set; }
    public string Description { get; set; } = null!;
    public string ArtifactName { get; set; } = null!;
    public string WardName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime SlaDeadline { get; set; }
    public string Priority { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string CitizenName { get; set; } = null!;
    public string? AssignedToName { get; set; }
    public DateTime? EscalationSentAt { get; set; }
    public int HoursOverdue { get; set; }
}