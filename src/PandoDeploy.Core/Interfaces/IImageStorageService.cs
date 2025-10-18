namespace PandoDeploy.Core.Interfaces;

public interface IImageStorageService
{
    Task<string> SaveImageAsync(Stream imageStream, string imageName, string imageTag);
    Task<Stream> GetImageAsync(string imagePath);
    Task<bool> DeleteImageAsync(string imagePath);
    Task<long> GetImageSizeAsync(string imagePath);
    Task<string> GetImagePathAsync(string imageName, string imageTag);
}

