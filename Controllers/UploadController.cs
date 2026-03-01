using G2CCRMPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace G2CCRMPortal.Controllers;

[ApiController]
[Route("api/v1/uploads")]
[Authorize]
public class UploadController : ControllerBase
{
    private readonly IUploadService _uploadService;

    public UploadController(IUploadService uploadService)
    {
        _uploadService = uploadService;
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        try
        {
            var fileLink = await _uploadService.UploadImageAsync(file);
            return Ok(new 
            { 
                message = "Upload successful",
                fileLink
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { status = "fail", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { status = "fail", message = "Upload failed.", error = ex.Message });
        }
    }
}