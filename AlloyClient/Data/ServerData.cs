using System.Linq;
using System.Xml.Linq;
using AlloyClient.State;
using Common;

namespace AlloyClient.Data;

public sealed class ServerListData(XElement xml) : IGlobalData {
    public readonly ServerItem[] ServerList = xml.Elements("Server").Select(s => new ServerItem(s)).ToArray();
}

public sealed class ServerItem(XElement xml) {
    public readonly bool AdminOnly = xml.GetValue("AdminOnly", false);

    public readonly string Dns = xml.GetValue("DNS", "");

    public readonly int MaxPlayers = xml.GetValue("MaxPlayers", 0);

    public readonly string Name = xml.GetValue("Name", "");

    public readonly int Players = xml.GetValue("Players", 0);

    public readonly int Port = xml.GetValue("Port", Settings.GameServerPort);
}