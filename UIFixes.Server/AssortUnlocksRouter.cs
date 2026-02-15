using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Utils;

namespace UIFixes.Server;

[Injectable]
public class AssortUnlocks(JsonUtil jsonUtil, AssortUnlocksCallbacks assortUnlocksCallbacks)
    : StaticRouter(
        jsonUtil,
        [
            new RouteAction(
                "/uifixes/assortUnlocks",
                async (url, info, sessionId, output) => {
                    var results = await assortUnlocksCallbacks.LoadAssorts();
                    return jsonUtil.Serialize(results);
                }
            )
        ]
    )
{ }