namespace PandoDeploy.Server.Data.Entities;

public class Deployment
{
    public int Id { get; set; }
    public string ImageName { get; set; } = string.Empty;
    public string ImageTag { get; set; } = string.Empty;
    public string ImageDigest { get; set; } = string.Empty;
    public string? ContainerId { get; set; }
    public string? ContainerName { get; set; }
    public int? Port { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? StoppedAt { get; set; }
    public string? ClientCertificateThumbprint { get; set; }
    public string? ApiKeyId { get; set; }
    public string? EnvironmentVariablesJson { get; set; }
    public string? LabelsJson { get; set; }
    public string? ImageStoragePath { get; set; }
}

