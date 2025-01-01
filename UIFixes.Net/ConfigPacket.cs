using LiteNetLib.Utils;

namespace UIFixes.Net;

public struct ConfigPacket(string version, string[] settings) : INetSerializable
{
    public string Version = version;
    public string[] Settings = settings;

    public void Deserialize(NetDataReader reader)
    {
        Version = reader.GetString();
        Settings = reader.GetStringArray();
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Version);
        writer.PutArray(Settings);
    }
}