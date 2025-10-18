namespace PandoDeploy.Shared.Models;

public class ImageTransferMetadata
{
    public string ImageName { get; set; } = string.Empty;
    public string ImageTag { get; set; } = string.Empty;
    public long ImageSize { get; set; }
    public string ImageDigest { get; set; } = string.Empty;
    public bool IsCompressed { get; set; } = true;
    public bool IsEncrypted { get; set; } = true;
    public string TransferId { get; set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

