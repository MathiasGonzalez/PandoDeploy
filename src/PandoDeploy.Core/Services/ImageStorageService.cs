using Microsoft.Extensions.Logging;
using PandoDeploy.Core.Interfaces;

namespace PandoDeploy.Core.Services;

public class ImageStorageService : IImageStorageService
{
    private readonly ILogger<ImageStorageService> _logger;
    private readonly string _storagePath;

    public ImageStorageService(ILogger<ImageStorageService> logger, string storagePath)
    {
        _logger = logger;
        _storagePath = storagePath;

        // Ensure storage directory exists
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
            _logger.LogInformation("Created storage directory at {StoragePath}", _storagePath);
        }
    }

    public async Task<string> SaveImageAsync(Stream imageStream, string imageName, string imageTag)
    {
        try
        {
            var fileName = $"{SanitizeFileName(imageName)}_{SanitizeFileName(imageTag)}_{DateTime.UtcNow:yyyyMMddHHmmss}.tar.gz";
            var filePath = Path.Combine(_storagePath, fileName);

            _logger.LogInformation("Saving image to {FilePath}", filePath);

            await using var fileStream = File.Create(filePath);
            await imageStream.CopyToAsync(fileStream);

            var fileInfo = new FileInfo(filePath);
            _logger.LogInformation("Image saved successfully. Size: {Size} bytes", fileInfo.Length);

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save image {ImageName}:{ImageTag}", imageName, imageTag);
            throw;
        }
    }

    public async Task<Stream> GetImageAsync(string imagePath)
    {
        try
        {
            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException($"Image file not found: {imagePath}");
            }

            _logger.LogInformation("Reading image from {ImagePath}", imagePath);

            var memoryStream = new MemoryStream();
            await using (var fileStream = File.OpenRead(imagePath))
            {
                await fileStream.CopyToAsync(memoryStream);
            }

            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get image from {ImagePath}", imagePath);
            throw;
        }
    }

    public Task<bool> DeleteImageAsync(string imagePath)
    {
        try
        {
            if (!File.Exists(imagePath))
            {
                _logger.LogWarning("Image file not found for deletion: {ImagePath}", imagePath);
                return Task.FromResult(false);
            }

            File.Delete(imagePath);
            _logger.LogInformation("Image deleted: {ImagePath}", imagePath);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete image {ImagePath}", imagePath);
            return Task.FromResult(false);
        }
    }

    public Task<long> GetImageSizeAsync(string imagePath)
    {
        try
        {
            if (!File.Exists(imagePath))
            {
                throw new FileNotFoundException($"Image file not found: {imagePath}");
            }

            var fileInfo = new FileInfo(imagePath);
            return Task.FromResult(fileInfo.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get image size for {ImagePath}", imagePath);
            throw;
        }
    }

    public Task<string> GetImagePathAsync(string imageName, string imageTag)
    {
        var pattern = $"{SanitizeFileName(imageName)}_{SanitizeFileName(imageTag)}_*.tar.gz";
        var files = Directory.GetFiles(_storagePath, pattern)
            .OrderByDescending(f => File.GetCreationTimeUtc(f))
            .ToList();

        if (files.Count == 0)
        {
            throw new FileNotFoundException($"No image found for {imageName}:{imageTag}");
        }

        return Task.FromResult(files[0]);
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        return sanitized;
    }
}

