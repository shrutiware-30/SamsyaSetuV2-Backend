using Microsoft.AspNetCore.Http;

namespace G2CCRMPortal.Services;

public interface IUploadService
{
    Task<string> UploadImageAsync(IFormFile file);
    void DeleteImage(string fileLink);
}