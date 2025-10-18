using PandoDeploy.Shared.Models;

namespace PandoDeploy.Core.Interfaces;

public interface IImagePackerService
{
    Task<Stream> PackImageAsync(string imageName, string imageTag, CancellationToken cancellationToken = default);
    Task<ImageTransferMetadata> UnpackImageAsync(Stream packedStream, string targetPath, CancellationToken cancellationToken = default);
    Task<string> CalculateImageDigestAsync(string imageName, string imageTag);
}

