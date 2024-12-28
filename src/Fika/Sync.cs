using System;
using Comfort.Common;
using Fika.Core.Coop.Players;
using Fika.Core.Coop.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using LiteNetLib;

namespace UIFixes;

public static class Sync
{
    public static void Init()
    {
        FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnFikaNetworkManagerCreated);
        FikaEventDispatcher.SubscribeEvent<GameWorldStartedEvent>(OnGameWorldStarted);
    }

    private static void OnFikaNetworkManagerCreated(FikaNetworkManagerCreatedEvent ev)
    {
        switch (ev.Manager)
        {
            case FikaServer server:
                server.RegisterPacket<ConfigPacket, NetPeer>(HandlePacketServer);
                break;
            case FikaClient client:
                client.RegisterPacket<ConfigPacket>(HandlePacketClient);
                break;
        }
    }

    private static void HandlePacketClient(ConfigPacket packet)
    {
        if (packet.version != PluginInfo.PLUGIN_VERSION)
        {
            NotificationManagerClass.DisplayWarningNotification($"UIFixes version mismatch: your client has {PluginInfo.PLUGIN_VERSION}, host has {packet.version}");
            return;
        }

        NotificationManagerClass.DisplayMessageNotification("UIFixes configuration synced from host");
    }

    private static void HandlePacketServer(ConfigPacket packet, NetPeer peer)
    {
        throw new NotImplementedException();
    }

    private static void OnGameWorldStarted(GameWorldStartedEvent ev)
    {
        if (FikaBackendUtils.IsSinglePlayer || !FikaBackendUtils.IsServer || ev.GameWorld.MainPlayer is not CoopPlayer player)
        {
            return;
        }

        ConfigPacket packet = new(player.NetId);
        Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableUnordered);
    }
}