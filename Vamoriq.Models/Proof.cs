namespace Vamoriq.Models;

public class Proof
{
    public string Id { get; set; } = string.Empty;
    public string MissionId { get; set; } = string.Empty;
    public string TextContent { get; set; } = string.Empty;
    public string? PhotoPath { get; set; }
    public bool HasPhoto => !string.IsNullOrEmpty(PhotoPath);
    public DateTime CompletedAtUtc { get; set; }
}
