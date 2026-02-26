using System.ComponentModel.DataAnnotations;

namespace G2CCRMPortal.DTOs.Feedback;

public class CreateFeedbackDto
{
    [Required]
    public Guid IssueRequestId { get; set; }

    [Required, Range(1, 5)]
    public int Rating { get; set; }

    public string? Comment { get; set; }
}