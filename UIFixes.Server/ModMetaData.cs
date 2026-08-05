using System.Collections.Generic;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace UIFixes.Server;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.tyfon.uifixes";
    public string Name { get; init; } = "UI Fixes";

    public string Author { get; init; } = "Tyfon";

    public List<string> Contributors { get; init; }

    public SemanticVersioning.Version Version { get; init; } = new(typeof(ModMetadata).Assembly.GetName().Version.ToString(3));

    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");

    public List<string> Incompatibilities { get; init; }

    public Dictionary<string, SemanticVersioning.Range> ModDependencies { get; init; }

    public string Url { get; init; } = "https://github.com/tyfon7/uifixes";

    public string License { get; init; } = "MIT";

    public bool HasPrepatcher { get; init; } = false;
}