using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace G2CCRMPortal.Services;

public class UploadService : IUploadService
{
    private readonly string _uploadsDirectory;
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB
    private static readonly string[] AllowedMimeTypes = 
    { 
        "image/jpeg",
        "image/jpg",
        "image/png", 
        "image/webp", 
        "image/gif" 
    };

    public UploadService(IWebHostEnvironment env)
    {
        // Handle null WebRootPath by using content root as fallback
        var basePath = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        _uploadsDirectory = Path.Combine(basePath, "uploads");
        
        if (!Directory.Exists(_uploadsDirectory))
        {
            Directory.CreateDirectory(_uploadsDirectory);
        }
    }

    public async Task<string> UploadImageAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("No file provided.");

        if (!AllowedMimeTypes.Contains(file.ContentType))
            throw new ArgumentException("Invalid file type. Only JPEG, PNG, WEBP, GIF are allowed.");

        if (file.Length > MaxFileSize)
            throw new ArgumentException("File size exceeds 5 MB limit.");

        var extension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(_uploadsDirectory, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        return $"/uploads/{uniqueFileName}";
    }

    public void DeleteImage(string fileLink)
    {
        if (string.IsNullOrEmpty(fileLink) || !fileLink.StartsWith("/uploads/"))
            return;

        var fileName = Path.GetFileName(fileLink);
        var filePath = Path.Combine(_uploadsDirectory, fileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}