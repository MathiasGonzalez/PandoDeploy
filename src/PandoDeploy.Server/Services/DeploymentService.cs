using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PandoDeploy.Core.Interfaces;
using PandoDeploy.Server.Data;
using PandoDeploy.Server.Data.Entities;
using PandoDeploy.Shared.Models;

namespace PandoDeploy.Server.Services;

public class DeploymentService
{
    private readonly ILogger<DeploymentService> _logger;
    private readonly PandoDeployContext _context;
    private readonly IDockerService _dockerService;
    private readonly IImageStorageService _imageStorageService;

    public DeploymentService(
        ILogger<DeploymentService> logger,
        PandoDeployContext context,
        IDockerService dockerService,
        IImageStorageService imageStorageService)
    {
        _logger = logger;
        _context = context;
        _dockerService = dockerService;
        _imageStorageService = imageStorageService;
    }

    public async Task<DeploymentResponse> ProcessDeploymentAsync(
        Stream imageStream,
        DeploymentRequest request,
        string? apiKeyId = null,
        string? certificateThumbprint = null)
    {
        var deployment = new Deployment
        {
            ImageName = request.ImageName,
            ImageTag = request.ImageTag,
            Port = request.Port,
            Status = DeploymentStatus.Receiving.ToString(),
            CreatedAt = DateTime.UtcNow,
            ApiKeyId = apiKeyId,
            ClientCertificateThumbprint = certificateThumbprint,
            ContainerName = request.ContainerName,
            EnvironmentVariablesJson = JsonSerializer.Serialize(request.EnvironmentVariables),
            LabelsJson = JsonSerializer.Serialize(request.Labels)
        };

        _context.Deployments.Add(deployment);
        await _context.SaveChangesAsync();

        try
        {
            // Save image to storage
            _logger.LogInformation("Saving image for deployment {DeploymentId}", deployment.Id);
            deployment.Status = DeploymentStatus.Processing.ToString();
            await _context.SaveChangesAsync();

            var imagePath = await _imageStorageService.SaveImageAsync(
                imageStream, 
                request.ImageName, 
                request.ImageTag);

            deployment.ImageStoragePath = imagePath;
            await _context.SaveChangesAsync();

            // Load image into Docker
            _logger.LogInformation("Loading image into Docker for deployment {DeploymentId}", deployment.Id);
            deployment.Status = DeploymentStatus.Loading.ToString();
            await _context.SaveChangesAsync();

            var imageFileStream = await _imageStorageService.GetImageAsync(imagePath);
            await _dockerService.LoadImageFromStreamAsync(
                imageFileStream, 
                request.ImageName, 
                request.ImageTag);

            // Create and start container
            _logger.LogInformation("Creating container for deployment {DeploymentId}", deployment.Id);
            deployment.Status = DeploymentStatus.Starting.ToString();
            await _context.SaveChangesAsync();

            var containerId = await _dockerService.CreateAndStartContainerAsync(request);

            deployment.ContainerId = containerId;
            deployment.Status = DeploymentStatus.Running.ToString();
            deployment.StartedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Clean up stored image after successful deployment (optional)
            // await _imageStorageService.DeleteImageAsync(imagePath);

            return new DeploymentResponse
            {
                Success = true,
                DeploymentId = deployment.Id.ToString(),
                ContainerId = containerId,
                Status = DeploymentStatus.Running,
                Message = "Deployment completed successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Deployment {DeploymentId} failed", deployment.Id);
            
            deployment.Status = DeploymentStatus.Failed.ToString();
            await _context.SaveChangesAsync();

            return new DeploymentResponse
            {
                Success = false,
                DeploymentId = deployment.Id.ToString(),
                Status = DeploymentStatus.Failed,
                Message = "Deployment failed",
                ErrorDetails = ex.Message
            };
        }
    }

    public async Task<DeploymentInfo?> GetDeploymentAsync(int deploymentId)
    {
        var deployment = await _context.Deployments.FindAsync(deploymentId);
        if (deployment == null)
        {
            return null;
        }

        return MapToDeploymentInfo(deployment);
    }

    public async Task<DeploymentListResponse> GetDeploymentsAsync(int skip = 0, int take = 50)
    {
        var query = _context.Deployments
            .OrderByDescending(d => d.CreatedAt);

        var totalCount = await query.CountAsync();
        var deployments = await query
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return new DeploymentListResponse
        {
            Deployments = deployments.Select(MapToDeploymentInfo).ToList(),
            TotalCount = totalCount
        };
    }

    private DeploymentInfo MapToDeploymentInfo(Deployment deployment)
    {
        return new DeploymentInfo
        {
            Id = deployment.Id,
            ImageName = deployment.ImageName,
            ImageTag = deployment.ImageTag,
            ImageDigest = deployment.ImageDigest,
            ContainerId = deployment.ContainerId,
            ContainerName = deployment.ContainerName,
            Port = deployment.Port,
            Status = Enum.Parse<DeploymentStatus>(deployment.Status),
            CreatedAt = deployment.CreatedAt,
            StartedAt = deployment.StartedAt,
            StoppedAt = deployment.StoppedAt,
            ClientCertificateThumbprint = deployment.ClientCertificateThumbprint,
            ApiKeyId = deployment.ApiKeyId,
            EnvironmentVariables = string.IsNullOrEmpty(deployment.EnvironmentVariablesJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(deployment.EnvironmentVariablesJson) ?? new(),
            Labels = string.IsNullOrEmpty(deployment.LabelsJson)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(deployment.LabelsJson) ?? new()
        };
    }
}

