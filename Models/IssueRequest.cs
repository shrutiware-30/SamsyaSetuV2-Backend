using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace G2CCRMPortal.Models;

[Table("IssueRequest")]
public class IssueRequest
{
    [Key]
    public Guid Id { get; set; }

    public int ArtifactId { get; set; }

    public Guid CitizenId { get; set; }

    public Guid? AssignedToId { get; set; }

    [Required]
    public string Description { get; set; } = null!;

    [Column(TypeName = "decimal(9,6)")]
    public decimal Latitude { get; set; }

    [Column(TypeName = "decimal(9,6)")]
    public decimal Longitude { get; set; }

    [MaxLength(255)]
    public string? LocationText { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Submitted";

    [Required, MaxLength(10)]
    public string Priority { get; set; } = "Medium";

    public DateTime? SlaDeadline { get; set; }

    public bool IsBreached { get; set; }

    public DateTime? EscalationSentAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int WardId { get; set; }

    // Navigation
    [ForeignKey(nameof(ArtifactId))]
    public Artifact Artifact { get; set; } = null!;

    [ForeignKey(nameof(CitizenId))]
    public User Citizen { get; set; } = null!;

    [ForeignKey(nameof(AssignedToId))]
    public User? AssignedTo { get; set; }

    [ForeignKey(nameof(WardId))]
    public Ward Ward { get; set; } = null!;

    public ICollection<TrackRequest> TrackRequests { get; set; } = [];
    public CitizenFeedback? Feedback { get; set; }
}