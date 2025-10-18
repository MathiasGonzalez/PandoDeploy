namespace PandoDeploy.Shared.Models;

public class DeploymentResponse
{
    public bool Success { get; set; }
    public string? DeploymentId { get; set; }
    public string? ContainerId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public DeploymentStatus Status { get; set; }
    public string? ErrorDetails { get; set; }
}

public enum DeploymentStatus
{
    Pending,
    Receiving,
    Processing,
    Loading,
    Starting,
    Running,
    Failed,
    Stopped
}

