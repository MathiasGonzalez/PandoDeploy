using System.ComponentModel.DataAnnotations;

namespace PandoDeploy.Shared.Models;

public class DeploymentRequest
{
    [Required]
    public string ImageName { get; set; } = string.Empty;

    [Required]
    public string ImageTag { get; set; } = string.Empty;

    public int? Port { get; set; }

    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

    public Dictionary<string, string> Labels { get; set; } = new();

    public string? ContainerName { get; set; }

    public bool RestartOnFailure { get; set; } = true;

    public List<string> Volumes { get; set; } = new();

    public List<PortMapping> PortMappings { get; set; } = new();

    public string? Network { get; set; }

    public ResourceLimits? Resources { get; set; }
}

public class PortMapping
{
    public int HostPort { get; set; }
    public int ContainerPort { get; set; }
    public string Protocol { get; set; } = "tcp";
}

public class ResourceLimits
{
    public long? MemoryLimit { get; set; }
    public long? MemoryReservation { get; set; }
    public double? CpuLimit { get; set; }
}

