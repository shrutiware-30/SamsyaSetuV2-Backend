using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace G2CCRMPortal.DTOs.IssueRequest;

public class CreateIssueRequestDto
{
    [Required]
    public int ArtifactId { get; set; }

    [Required]
    public string Description { get; set; } = null!;

    [Required]
    public decimal Latitude { get; set; }

    [Required]
    public decimal Longitude { get; set; }

    [MaxLength(255)]
    public string? LocationText { get; set; }

    [MaxLength(10)]
    public string Priority { get; set; } = "Medium";

    // File upload instead of ImageUrl string
    public IFormFile? ImageFile { get; set; }
}