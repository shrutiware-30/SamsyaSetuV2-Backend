using System.Threading.Tasks;
using G2CCRMPortal.DTOs.Location;
using G2CCRMPortal.Services;
using Microsoft.AspNetCore.Mvc;

namespace G2CCRMPortal.Controllers;

[ApiController]
[Route("api/v1/location")]
public class LocationController : ControllerBase
{
    private readonly ILocationService _locationService;

    public LocationController(ILocationService locationService)
    {
        _locationService = locationService;
    }

    [HttpGet("reverse-geocode")]
    public async Task<IActionResult> ReverseGeocode([FromQuery] double lat, [FromQuery] double lng)
    {
        if (lat == 0 && lng == 0)
            return BadRequest(new { status = "fail", message = "Valid latitude and longitude are required." });

        try
        {
            var address = await _locationService.ReverseGeocodeAsync(lat, lng);
            return Ok(new { status = "success", data = new ReverseGeocodeResponseDto { Address = address } });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { status = "fail", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(502, new { status = "fail", message = "Unable to reach geocoding service" });
        }
    }
}