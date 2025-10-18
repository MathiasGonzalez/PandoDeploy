using PandoDeploy.Shared.Models;

namespace PandoDeploy.Core.Interfaces;

public interface IDockerService
{
    Task<bool> IsDockerAvailableAsync();
    Task<string> LoadImageFromStreamAsync(Stream imageStream, string imageName, string imageTag);
    Task<string> CreateAndStartContainerAsync(DeploymentRequest request);
    Task<bool> StopContainerAsync(string containerId);
    Task<bool> RemoveContainerAsync(string containerId);
    Task<DockerStatus> GetDockerStatusAsync();
    Task<bool> ImageExistsAsync(string imageName, string imageTag);
    Task<List<string>> ListRunningContainersAsync();
}

