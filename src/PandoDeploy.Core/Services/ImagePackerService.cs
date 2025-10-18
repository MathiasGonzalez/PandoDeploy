using System.IO.Compression;
using System.Security.Cryptography;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using PandoDeploy.Core.Interfaces;
using PandoDeploy.Shared.Models;

namespace PandoDeploy.Core.Services;

public class ImagePackerService : IImagePackerService
{
    private readonly ILogger<ImagePackerService> _logger;
    private readonly DockerClient _dockerClient;

    public ImagePackerService(ILogger<ImagePackerService> logger, string dockerEndpoint = "unix:///var/run/docker.sock")
    {
        _logger = logger;
        
        var endpoint = dockerEndpoint;
        if (OperatingSystem.IsWindows() && dockerEndpoint.StartsWith("unix://"))
        {
            endpoint = "npipe://./pipe/docker_engine";
        }
        
        _dockerClient = new DockerClientConfiguration(new Uri(endpoint)).CreateClient();
    }

    public async Task<Stream> PackImageAsync(string imageName, string imageTag, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullImageName = $"{imageName}:{imageTag}";
            _logger.LogInformation("Packing image {ImageFullName}", fullImageName);

            // Export image to tar
            var imageStream = await _dockerClient.Images.SaveImageAsync(fullImageName, cancellationToken);

            // Create a memory stream to hold the compressed data
            var compressedStream = new MemoryStream();

            // Compress with GZip
            await using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
            {
                await imageStream.CopyToAsync(gzipStream, cancellationToken);
            }

            compressedStream.Position = 0;

            _logger.LogInformation("Image {ImageFullName} packed successfully. Size: {Size} bytes", 
                fullImageName, compressedStream.Length);

            return compressedStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pack image {ImageName}:{ImageTag}", imageName, imageTag);
            throw;
        }
    }

    public async Task<ImageTransferMetadata> UnpackImageAsync(Stream packedStream, string targetPath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Unpacking image to {TargetPath}", targetPath);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Decompress from GZip
            await using var fileStream = File.Create(targetPath);
            await using var gzipStream = new GZipStream(packedStream, CompressionMode.Decompress);
            await gzipStream.CopyToAsync(fileStream, cancellationToken);

            var fileInfo = new FileInfo(targetPath);

            // Calculate digest
            var digest = await CalculateFileDigestAsync(targetPath);

            _logger.LogInformation("Image unpacked successfully. Size: {Size} bytes", fileInfo.Length);

            return new ImageTransferMetadata
            {
                ImageSize = fileInfo.Length,
                ImageDigest = digest,
                IsCompressed = false,
                IsEncrypted = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unpack image");
            throw;
        }
    }

    public async Task<string> CalculateImageDigestAsync(string imageName, string imageTag)
    {
        try
        {
            var fullImageName = $"{imageName}:{imageTag}";
            _logger.LogInformation("Calculating digest for {ImageFullName}", fullImageName);

            // Get image inspection details
            var imageInspect = await _dockerClient.Images.InspectImageAsync(fullImageName);

            // Return the image ID or RepoDigests if available
            if (imageInspect.RepoDigests != null && imageInspect.RepoDigests.Count > 0)
            {
                return imageInspect.RepoDigests[0];
            }

            return imageInspect.ID;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate digest for {ImageName}:{ImageTag}", imageName, imageTag);
            throw;
        }
    }

    private async Task<string> CalculateFileDigestAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        await using var stream = File.OpenRead(filePath);
        var hash = await sha256.ComputeHashAsync(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}

