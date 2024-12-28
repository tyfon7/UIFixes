using LiteNetLib.Utils;

namespace UIFixes;

public struct ConfigPacket(int netId) : INetSerializable
{
    public int netId = netId;
    public string version = PluginInfo.PLUGIN_VERSION;

    public void Deserialize(NetDataReader reader)
    {
        netId = reader.GetInt();
        version = reader.GetString();

        if (version.Equals(PluginInfo.PLUGIN_VERSION))
        {
            reader.Get<SettingsPacket>();
        }
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(netId);
        writer.Put(version);
        writer.Put(new SettingsPacket());
    }
}