using G2CCRMPortal.Data;
using G2CCRMPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace G2CCRMPortal.Controllers;

[ApiController]
[Route("api/v1/wards")]
public class WardController : ControllerBase
{
    private readonly G2CCrmDbContext _db;

    public WardController(G2CCrmDbContext db)
    {
        _db = db;
    }

    // GET api/v1/wards
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var wards = await _db.Wards.OrderBy(w => w.Id).ToListAsync();
        return Ok(new { status = "success", results = wards.Count, data = wards });
    }

    // GET api/v1/wards/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var ward = await _db.Wards.FindAsync(id);
        if (ward is null) return NotFound();
        return Ok(new { status = "success", data = ward });
    }

    // GET api/v1/wards/{wardId}/officers
    [HttpGet("{wardId:int}/officers")]
    public async Task<IActionResult> GetWardOfficers(int wardId)
    {
        var ward = await _db.Wards.FindAsync(wardId);
        if (ward is null) return NotFound();

        var officers = await _db.Users
            .Where(u => u.Role == "Officer" && u.WardId == wardId)
            .Select(u => new
            {
                u.Id,
                u.Name,
                email = u.Email,
                phone = u.MobileNumber
            })
            .ToListAsync();

        return Ok(new { status = "success", data = officers });
    }

    // GET api/v1/wards/{wardId}/issue-stats
    [HttpGet("{wardId:int}/issue-stats")]
    public async Task<IActionResult> GetWardIssueStats(int wardId)
    {
        var ward = await _db.Wards.FindAsync(wardId);
        if (ward is null) return NotFound();

        var totalAssigned = await _db.IssueRequests.CountAsync(i => i.WardId == wardId);
        var totalCompleted = await _db.IssueRequests.CountAsync(i => i.WardId == wardId && i.Status == "Resolved");

        return Ok(new { status = "success", data = new { totalAssigned, totalCompleted } });
    }

    // POST api/v1/wards
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] Ward dto)
    {
        _db.Wards.Add(dto);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = dto.Id },
            new { status = "success", data = dto });
    }

    // PATCH api/v1/wards/{id}
    [HttpPatch("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] Ward dto)
    {
        var ward = await _db.Wards.FindAsync(id);
        if (ward is null) return NotFound();

        ward.Name = dto.Name;
        ward.Latitude = dto.Latitude;
        ward.Longitude = dto.Longitude;
        ward.DepartmentEmail = dto.DepartmentEmail;
        await _db.SaveChangesAsync();

        return Ok(new { status = "success", data = ward });
    }
}