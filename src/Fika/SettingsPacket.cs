using System.Linq;
using BepInEx.Configuration;
using LiteNetLib.Utils;

namespace UIFixes;

public struct SettingsPacket : INetSerializable
{
    public void Deserialize(NetDataReader reader)
    {
        foreach (ConfigDefinition key in Plugin.Instance.Config.Keys)
        {
            var configEntry = Plugin.Instance.Config[key];
            if (configEntry.Description.Tags.FirstOrDefault() is not ConfigurationManagerAttributes attributes || !attributes.Synced.HasValue || !attributes.Synced.Value)
            {
                continue;
            }

            configEntry.SetSerializedValue(reader.GetString());
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        foreach (ConfigDefinition key in Plugin.Instance.Config.Keys)
        {
            var configEntry = Plugin.Instance.Config[key];
            if (configEntry.Description.Tags.FirstOrDefault() is not ConfigurationManagerAttributes attributes || !attributes.Synced.HasValue || !attributes.Synced.Value)
            {
                continue;
            }

            writer.Put(configEntry.GetSerializedValue());
        }
    }
}
