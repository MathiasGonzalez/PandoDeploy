namespace PandoDeploy.Shared.Models;

public class DeploymentInfo
{
    public int Id { get; set; }
    public string ImageName { get; set; } = string.Empty;
    public string ImageTag { get; set; } = string.Empty;
    public string ImageDigest { get; set; } = string.Empty;
    public string? ContainerId { get; set; }
    public string? ContainerName { get; set; }
    public int? Port { get; set; }
    public DeploymentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? StoppedAt { get; set; }
    public string? ClientCertificateThumbprint { get; set; }
    public string? ApiKeyId { get; set; }
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
    public Dictionary<string, string> Labels { get; set; } = new();
}

public class DeploymentListResponse
{
    public List<DeploymentInfo> Deployments { get; set; } = new();
    public int TotalCount { get; set; }
}

