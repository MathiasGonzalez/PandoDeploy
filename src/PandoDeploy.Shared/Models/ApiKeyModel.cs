namespace PandoDeploy.Shared.Models;

public class ApiKeyModel
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

public class CreateApiKeyRequest
{
    public string Name { get; set; } = string.Empty;
    public DateTime? ExpiresAt { get; set; }
}

public class CreateApiKeyResponse
{
    public bool Success { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

