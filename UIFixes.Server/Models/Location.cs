using System.Text.Json.Serialization;

namespace UIFixes.Server;

public record Location
{
    [JsonPropertyName("parentId")]
    public string ParentId { get; set; }

    [JsonPropertyName("slotId")]
    public string SlotId { get; set; }
}