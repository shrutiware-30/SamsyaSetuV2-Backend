using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace G2CCRMPortal.Models;

[Table("CitizenFeedback")]
public class CitizenFeedback
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public Guid IssueRequestId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime SubmittedAt { get; set; }

    // Navigation
    [ForeignKey(nameof(IssueRequestId))]
    public IssueRequest IssueRequest { get; set; } = null!;
}