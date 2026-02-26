using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace G2CCRMPortal.Models;

[Table("Ward")]
public class Ward
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    [Column(TypeName = "decimal(9,6)")]
    public decimal Latitude { get; set; }

    [Column(TypeName = "decimal(9,6)")]
    public decimal Longitude { get; set; }

    [Required, MaxLength(100)]
    public string DepartmentEmail { get; set; } = null!;

    // Navigation
    public ICollection<User> Officers { get; set; } = [];
    public ICollection<IssueRequest> IssueRequests { get; set; } = [];
}