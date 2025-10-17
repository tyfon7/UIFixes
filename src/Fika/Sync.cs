using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.Communications;
using Fika.Core.Main.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using UIFixes.Net;

namespace UIFixes.Fika;

public static class Sync
{
    private static readonly Dictionary<ConfigEntryBase, SyncedValues> SettingOverrides = [];

    private static bool ConfigReceived;

    public static void Init()
    {
        Plugin.Instance.Logger.LogInfo("Fika detected, initializing settings sync");

        // Calling new Action() myself is required. Otherwise the compiler will generate a static class to cache the action, and that class
        // is walked by tarkov at load, forcing a fika dll load, which pukes when fika is missing!
        FikaEventDispatcher.SubscribeEvent(new Action<FikaNetworkManagerCreatedEvent>(OnFikaNetworkManagerCreated));
        FikaEventDispatcher.SubscribeEvent(new Action<PeerConnectedEvent>(OnPeerConnected));
        FikaEventDispatcher.SubscribeEvent(new Action<FikaRaidStartedEvent>(OnRaidStarted));
        FikaEventDispatcher.SubscribeEvent(new Action<FikaGameEndedEvent>(OnGameEnded));
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
        Singleton<FikaServer>.Instance.SendDataToPeer(ref packet, DeliveryMethod.ReliableUnordered, ev.Peer);
    }

    private static void OnFikaNetworkManagerCreated(FikaNetworkManagerCreatedEvent ev)
    {
        switch (ev.Manager)
        {
            case FikaServer server:
                // Manual call to new EventHandler() required, see comment above about new Action.
                Plugin.Instance.Config.SettingChanged += new EventHandler<SettingChangedEventArgs>(OnServerSettingChanged);
                break;
            case FikaClient client:
                Plugin.Instance.Config.SettingChanged += new EventHandler<SettingChangedEventArgs>(OnClientSettingChanged);
                client.RegisterPacket(new Action<ConfigPacket>(HandlePacketClient));
                break;
        }
    }

    private static void OnServerSettingChanged(object sender, SettingChangedEventArgs args)
    {
        if (!args.ChangedSetting.IsSynced())
        {
            return;
        }

        var settings = Settings.SyncedConfigs().Select(configEntry => configEntry.GetSerializedValue());
        ConfigPacket packet = new(PluginInfo.PLUGIN_VERSION, settings.ToArray());

        Plugin.Instance.Logger.LogInfo("Synced setting changed; sending Fika sync packet to all peers");
        Singleton<FikaServer>.Instance.SendData(ref packet, DeliveryMethod.ReliableUnordered);
    }

    // Prevents clients from changing synced settings
    private static bool IgnoreClientSettingsChanged;
    private static void OnClientSettingChanged(object sender, SettingChangedEventArgs args)
    {
        ConfigEntryBase configEntry = args.ChangedSetting;
        if (IgnoreClientSettingsChanged || !configEntry.IsSynced())
        {
            return;
        }

        IgnoreClientSettingsChanged = true;

        if (SettingOverrides.TryGetValue(configEntry, out SyncedValues values))
        {
            configEntry.SetSerializedValue(values.Override);
        }

        IgnoreClientSettingsChanged = false;
    }

    private static void OnRaidStarted(FikaRaidStartedEvent ev)
    {
        if (!ev.IsServer && !ConfigReceived)
        {
            Plugin.Instance.Logger.LogError("Fika sync config not received! UI Fixes missing from host?");
            NotificationManagerClass.DisplayWarningNotification("UI Fixes sync failed! UI Fixes is required on host", ENotificationDurationType.Long);
        }
    }

    private static void OnGameEnded(FikaGameEndedEvent ev)
    {
        if (ev.ExitStatus == ExitStatus.Transit)
        {
            return;
        }

        if (ev.IsServer)
        {
            Plugin.Instance.Config.SettingChanged -= new EventHandler<SettingChangedEventArgs>(OnServerSettingChanged);
        }
        else
        {
            Plugin.Instance.Config.SettingChanged -= new EventHandler<SettingChangedEventArgs>(OnClientSettingChanged);

            foreach (var config in SettingOverrides)
            {
                config.Key.SetSerializedValue(config.Value.Original);
            }

            SettingOverrides.Clear();

            ConfigReceived = false;
            Plugin.Instance.Config.Save();
            Plugin.Instance.Config.SaveOnConfigSet = true;
        }
    }

    private static void HandlePacketClient(ConfigPacket packet)
    {
        Plugin.Instance.Logger.LogInfo("Fika sync config received");
        ConfigReceived = true;

        if (packet.Version != PluginInfo.PLUGIN_VERSION)
        {
            Plugin.Instance.Logger.LogError($"UIFixes version mismatch: your client has {PluginInfo.PLUGIN_VERSION}, host has {packet.Version}");
            NotificationManagerClass.DisplayWarningNotification($"UIFixes version mismatch! You: {PluginInfo.PLUGIN_VERSION}, Host: {packet.Version}", ENotificationDurationType.Long);
            return;
        }

        // Disable saving until end of raid
        Plugin.Instance.Config.SaveOnConfigSet = false;

        IgnoreClientSettingsChanged = true;
        foreach (var (configEntry, value) in Settings.SyncedConfigs().Zip(packet.Settings, (key, value) => (key, value)))
        {
            if (SettingOverrides.TryGetValue(configEntry, out SyncedValues values))
            {
                SettingOverrides[configEntry] = new SyncedValues { Original = values.Original, Override = value };
            }
            else
            {
                SettingOverrides[configEntry] = new SyncedValues { Original = configEntry.GetSerializedValue(), Override = value };
            }

            configEntry.SetSerializedValue(value);
        }

        IgnoreClientSettingsChanged = false;

        NotificationManagerClass.DisplayMessageNotification("UIFixes configuration synced from host");
    }

    private struct SyncedValues
    {
        public string Original;
        public string Override;
    }
}