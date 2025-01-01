using BepInEx.Configuration;
using Comfort.Common;
using Fika.Core.Coop.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using LiteNetLib;
using System;
using System.Linq;
using UIFixes.Net;

namespace UIFixes.Fika;

public static class Sync
{
    public static void Init()
    {
        Plugin.Instance.Logger.LogInfo("Fika detected, initializing settings sync");

        FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnFikaNetworkManagerCreated);
        FikaEventDispatcher.SubscribeEvent<PeerConnectedEvent>(OnPeerConnected);
        FikaEventDispatcher.SubscribeEvent<FikaGameEndedEvent>(OnGameEnded);
    }

    private static void OnPeerConnected(PeerConnectedEvent ev)
    {
        if (!FikaBackendUtils.IsServer)
        {
            return;
        }

        var settings = Settings.SyncedConfigs().Select(configEntry => configEntry.GetSerializedValue());
        ConfigPacket packet = new(PluginInfo.PLUGIN_VERSION, settings.ToArray());

        Plugin.Instance.Logger.LogInfo($"Peer connected; sending Fika sync packet to peer {ev.Peer.Id}");
        Singleton<FikaServer>.Instance.SendDataToPeer(ev.Peer, ref packet, DeliveryMethod.ReliableUnordered);
    }

    private static void OnSettingChanged(object sender, EventArgs args)
    {
        if (args is not SettingChangedEventArgs settingArgs || !settingArgs.ChangedSetting.IsSynced())
        {
            return;
        }

        var settings = Settings.SyncedConfigs().Select(configEntry => configEntry.GetSerializedValue());
        ConfigPacket packet = new(PluginInfo.PLUGIN_VERSION, settings.ToArray());

        Plugin.Instance.Logger.LogInfo("Synced setting changed; sending Fika sync packet to all peers");
        Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableUnordered);
    }

    private static void OnFikaNetworkManagerCreated(FikaNetworkManagerCreatedEvent ev)
    {
        switch (ev.Manager)
        {
            case FikaServer server:
                Plugin.Instance.Config.SettingChanged += OnSettingChanged;
                break;
            case FikaClient client:
                client.RegisterPacket<ConfigPacket>(HandlePacketClient);
                break;
        }
    }

    private static void OnGameEnded(FikaGameEndedEvent ev)
    {
        Plugin.Instance.Config.SettingChanged -= OnSettingChanged;
        foreach (UIFConfigEntryBase configEntry in Settings.SyncedConfigs())
        {
            configEntry.Readonly = false;
            configEntry.ClearOverride();
        }
    }

    private static void HandlePacketClient(ConfigPacket packet)
    {
        Plugin.Instance.Logger.LogInfo("Fika sync config received");

        if (packet.Version != PluginInfo.PLUGIN_VERSION)
        {
            Plugin.Instance.Logger.LogError($"UIFixes version mismatch: your client has {PluginInfo.PLUGIN_VERSION}, host has {packet.Version}");
            NotificationManagerClass.DisplayWarningNotification($"UIFixes version mismatch: your client has {PluginInfo.PLUGIN_VERSION}, host has {packet.Version}");
            return;
        }

        foreach (var (configEntry, value) in Settings.SyncedConfigs().Zip(packet.Settings, (key, value) => (key, value)))
        {
            configEntry.SetSerializedOverride(value);
            configEntry.Readonly = true;
        }

        NotificationManagerClass.DisplayMessageNotification("UIFixes configuration synced from host");
    }
}