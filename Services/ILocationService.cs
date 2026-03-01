using System.Threading.Tasks;

namespace G2CCRMPortal.Services;

public interface ILocationService
{
    Task<string> ReverseGeocodeAsync(double latitude, double longitude);
}