using Docker.DotNet;
using Docker.DotNet.Models;

using Microsoft.Extensions.Logging;

using PandoDeploy.Core.Interfaces;
using PandoDeploy.Shared.Models;

namespace PandoDeploy.Core.Services;

public class DockerService : IDockerService
{
    private readonly ILogger<DockerService> _logger;
    private readonly DockerClient _dockerClient;

    public DockerService(ILogger<DockerService> logger, string dockerEndpoint = "unix:///var/run/docker.sock")
    {
        _logger = logger;

        // Determine Docker endpoint based on OS
        var endpoint = dockerEndpoint;
        if (OperatingSystem.IsWindows() && dockerEndpoint.StartsWith("unix://"))
        {
            endpoint = "npipe://./pipe/docker_engine";
        }

        _dockerClient = new DockerClientConfiguration(new Uri(endpoint)).CreateClient();
    }

    public async Task<bool> IsDockerAvailableAsync()
    {
        try
        {
            await _dockerClient.System.PingAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Docker is not available");
            return false;
        }
    }

    public async Task<string> LoadImageFromStreamAsync(Stream imageStream, string imageName, string imageTag)
    {
        try
        {
            _logger.LogInformation("Loading image {ImageName}:{ImageTag} into Docker", imageName, imageTag);

            var loadImageParams = new ImageLoadParameters();
            var progress = new Progress<JSONMessage>(message => 
            {
                _logger.LogDebug("Image load progress: {Message}", message);
            });
            
            await _dockerClient.Images.LoadImageAsync(loadImageParams, imageStream, progress, CancellationToken.None);

            _logger.LogInformation("Image loaded successfully: {ImageName}:{ImageTag}", imageName, imageTag);

            return $"{imageName}:{imageTag}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load image {ImageName}:{ImageTag}", imageName, imageTag);
            throw;
        }
    }

    public async Task<string> CreateAndStartContainerAsync(DeploymentRequest request)
    {
        try
        {
            var imageFullName = $"{request.ImageName}:{request.ImageTag}";
            _logger.LogInformation("Creating container from image {ImageFullName}", imageFullName);

            var containerName = request.ContainerName ?? $"{request.ImageName}-{Guid.NewGuid():N}";

            // Build port bindings
            var portBindings = new Dictionary<string, IList<PortBinding>>();
            var exposedPorts = new Dictionary<string, EmptyStruct>();

            if (request.Port.HasValue)
            {
                var containerPort = $"{request.Port.Value}/tcp";
                portBindings[containerPort] =
                [
                    new PortBinding { HostPort = request.Port.Value.ToString() }
                ];
                exposedPorts[containerPort] = default;
            }

            foreach (var portMapping in request.PortMappings)
            {
                var containerPort = $"{portMapping.ContainerPort}/{portMapping.Protocol}";
                portBindings[containerPort] =
                [
                    new PortBinding { HostPort = portMapping.HostPort.ToString() }
                ];
                exposedPorts[containerPort] = default;
            }

            // Build volume bindings
            var binds = request.Volumes.ToList();

            // Create container config
            var createParams = new CreateContainerParameters
            {
                Image = imageFullName,
                Name = containerName,
                Env = request.EnvironmentVariables.Select(kv => $"{kv.Key}={kv.Value}").ToList(),
                Labels = request.Labels,
                ExposedPorts = exposedPorts,
                HostConfig = new HostConfig
                {
                    PortBindings = portBindings,
                    Binds = binds,
                    RestartPolicy = request.RestartOnFailure
                        ? new RestartPolicy { Name = RestartPolicyKind.UnlessStopped }
                        : new RestartPolicy { Name = RestartPolicyKind.No },
                    NetworkMode = request.Network
                }
            };

            // Apply resource limits if specified
            if (request.Resources != null)
            {
                if (request.Resources.MemoryLimit.HasValue)
                {
                    createParams.HostConfig.Memory = request.Resources.MemoryLimit.Value;
                }

                if (request.Resources.MemoryReservation.HasValue)
                {
                    createParams.HostConfig.MemoryReservation = request.Resources.MemoryReservation.Value;
                }

                if (request.Resources.CpuLimit.HasValue)
                {
                    createParams.HostConfig.NanoCPUs = (long)(request.Resources.CpuLimit.Value * 1_000_000_000);
                }
            }

            var container = await _dockerClient.Containers.CreateContainerAsync(createParams);

            _logger.LogInformation("Container created with ID: {ContainerId}", container.ID);

            // Start the container
            var started = await _dockerClient.Containers.StartContainerAsync(
                container.ID,
                new ContainerStartParameters());

            if (started)
            {
                _logger.LogInformation("Container {ContainerId} started successfully", container.ID);
            }
            else
            {
                _logger.LogWarning("Container {ContainerId} creation returned false", container.ID);
            }

            return container.ID;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create and start container for {ImageName}:{ImageTag}",
                request.ImageName, request.ImageTag);
            throw;
        }
    }

    public async Task<bool> StopContainerAsync(string containerId)
    {
        try
        {
            _logger.LogInformation("Stopping container {ContainerId}", containerId);

            await _dockerClient.Containers.StopContainerAsync(
                containerId,
                new ContainerStopParameters { WaitBeforeKillSeconds = 10 });

            _logger.LogInformation("Container {ContainerId} stopped successfully", containerId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop container {ContainerId}", containerId);
            return false;
        }
    }

    public async Task<bool> RemoveContainerAsync(string containerId)
    {
        try
        {
            _logger.LogInformation("Removing container {ContainerId}", containerId);

            await _dockerClient.Containers.RemoveContainerAsync(
                containerId,
                new ContainerRemoveParameters { Force = true });

            _logger.LogInformation("Container {ContainerId} removed successfully", containerId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove container {ContainerId}", containerId);
            return false;
        }
    }

    public async Task<DockerStatus> GetDockerStatusAsync()
    {
        try
        {
            var version = await _dockerClient.System.GetVersionAsync();
            var info = await _dockerClient.System.GetSystemInfoAsync();

            var containers = await _dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters { All = false });

            var images = await _dockerClient.Images.ListImagesAsync(
                new ImagesListParameters { All = false });

            return new DockerStatus
            {
                IsAvailable = true,
                Version = version.Version,
                RunningContainers = containers.Count,
                TotalImages = images.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Docker status");
            return new DockerStatus { IsAvailable = false };
        }
    }

    public async Task<bool> ImageExistsAsync(string imageName, string imageTag)
    {
        try
        {
            var images = await _dockerClient.Images.ListImagesAsync(
                new ImagesListParameters { All = true });

            var fullImageName = $"{imageName}:{imageTag}";
            return images.Any(img => img.RepoTags != null && img.RepoTags.Contains(fullImageName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if image exists: {ImageName}:{ImageTag}", imageName, imageTag);
            return false;
        }
    }

    public async Task<List<string>> ListRunningContainersAsync()
    {
        try
        {
            var containers = await _dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters { All = false });

            return containers.Select(c => c.ID).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list running containers");
            return [];
        }
    }
}

