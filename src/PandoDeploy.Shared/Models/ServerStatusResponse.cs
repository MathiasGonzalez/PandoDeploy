namespace PandoDeploy.Shared.Models;

public class ServerStatusResponse
{
    public bool IsHealthy { get; set; }
    public string Version { get; set; } = string.Empty;
    public DateTime ServerTime { get; set; } = DateTime.UtcNow;
    public DockerStatus Docker { get; set; } = new();
    public int ActiveDeployments { get; set; }
    public int TotalDeployments { get; set; }
}

public class DockerStatus
{
    public bool IsAvailable { get; set; }
    public string? Version { get; set; }
    public int RunningContainers { get; set; }
    public int TotalImages { get; set; }
}

