using System.Collections.Generic;
using System.Text.Json.Serialization;

using SPTarkov.Server.Core.Models.Common;

namespace UIFixes.Server;

public record ProfileData
{
    [JsonPropertyName("originalToolLocations")]
    public Dictionary<MongoId, Location> OriginalToolLocations { get; set; } = [];
}