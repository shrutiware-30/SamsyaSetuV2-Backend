using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace G2CCRMPortal.Models;

[Table("Artifact")]
public class Artifact
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    [Required, MaxLength(50)]
    public string Category { get; set; } = null!;

    public int DefaultSLADays { get; set; }

    public bool IsActive { get; set; }

    // Navigation
    public ICollection<IssueRequest> IssueRequests { get; set; } = [];
}