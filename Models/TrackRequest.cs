using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace G2CCRMPortal.Models;

[Table("TrackRequest")]
public class TrackRequest
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public Guid IssueRequestId { get; set; }

    public Guid ChangedByUserId { get; set; }

    [Required, MaxLength(30)]
    public string OldStatus { get; set; } = null!;

    [Required, MaxLength(30)]
    public string NewStatus { get; set; } = null!;

    public string? Remarks { get; set; }

    public DateTime ChangedAt { get; set; }

    // Navigation
    [ForeignKey(nameof(IssueRequestId))]
    public IssueRequest IssueRequest { get; set; } = null!;

    [ForeignKey(nameof(ChangedByUserId))]
    public User ChangedByUser { get; set; } = null!;
}