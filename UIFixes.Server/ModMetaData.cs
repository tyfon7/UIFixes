using System.Collections.Generic;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace UIFixes.Server;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.tyfon.uifixes";
    public override string Name { get; init; } = "UI Fixes";

    public override string Author { get; init; } = "Tyfon";

    public override List<string> Contributors { get; init; }

    public override SemanticVersioning.Version Version { get; init; } = new("5.0.1");

    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");

    public override List<string> Incompatibilities { get; init; }

    public override Dictionary<string, SemanticVersioning.Range> ModDependencies { get; init; }

    public override string Url { get; init; } = "https://github.com/tyfon7/uifixes";

    public override bool? IsBundleMod { get; init; } = false;

    public override string License { get; init; } = "MIT";
}