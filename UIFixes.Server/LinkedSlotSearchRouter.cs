using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Utils;

namespace UIFixes.Server;

[Injectable]
public class LinkedSlotSearchRouter(JsonUtil jsonUtil, RagfairCallbacks ragfairCallbacks)
    : StaticRouter(
        jsonUtil,
        [
            new RouteAction<LinkedSlotSearchRequestData>(
                "/uifixes/ragfair/find",
                async (url, info, sessionID, output) => await ragfairCallbacks.Search(url, info, sessionID)
            ),
        ]
    )
{ }
