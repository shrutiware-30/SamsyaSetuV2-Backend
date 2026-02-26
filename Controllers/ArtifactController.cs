using G2CCRMPortal.Data;
using G2CCRMPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace G2CCRMPortal.Controllers;

[ApiController]
[Route("api/v1/artifacts")]
public class ArtifactController : ControllerBase
{
    private readonly G2CCrmDbContext _db;

    public ArtifactController(G2CCrmDbContext db)
    {
        _db = db;
    }

    // GET api/v1/artifacts — public list for complaint form dropdown
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var artifacts = await _db.Artifacts
            .Where(a => a.IsActive)
            .OrderBy(a => a.Category)
            .ThenBy(a => a.Name)
            .Select(a => new { a.Id, a.Name, a.Category, a.DefaultSLADays })
            .ToListAsync();

        return Ok(new { status = "success", results = artifacts.Count, data = artifacts });
    }

    // POST api/v1/artifacts — Admin creates artifact type
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] Artifact dto)
    {
        var artifact = new Artifact
        {
            Name = dto.Name,
            Category = dto.Category,
            DefaultSLADays = dto.DefaultSLADays,
            IsActive = true
        };

        _db.Artifacts.Add(artifact);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { status = "success", data = artifact });
    }

    // PATCH api/v1/artifacts/{id}
    [HttpPatch("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] Artifact dto)
    {
        var artifact = await _db.Artifacts.FindAsync(id);
        if (artifact is null) return NotFound();

        artifact.Name = dto.Name;
        artifact.Category = dto.Category;
        artifact.DefaultSLADays = dto.DefaultSLADays;
        await _db.SaveChangesAsync();

        return Ok(new { status = "success", data = artifact });
    }

    // DELETE api/v1/artifacts/{id} — soft delete
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var artifact = await _db.Artifacts.FindAsync(id);
        if (artifact is null) return NotFound();

        artifact.IsActive = false;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}