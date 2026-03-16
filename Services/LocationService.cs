using System.Text.Json;

namespace G2CCRMPortal.Services;

public class LocationService : ILocationService
{
    private readonly HttpClient _httpClient;
    private const string NominatimApi = "https://nominatim.openstreetmap.org/reverse";

    public LocationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> ReverseGeocodeAsync(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
            throw new ArgumentException("Invalid latitude or longitude values.");

        try
        {
            var url = $"{NominatimApi}?format=json&lat={latitude}&lon={longitude}";
            
            // Set User-Agent (required by Nominatim)
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "G2CCRMPortal/1.0");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // Nominatim returns an "address" object, we'll return the display_name
            if (root.TryGetProperty("address", out var addressObj) && root.TryGetProperty("display_name", out var displayName))
            {
                return displayName.GetString() ?? "Address not found";
            }

            return "Address not found";
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Failed to call Nominatim API", ex);
        }
    }
}