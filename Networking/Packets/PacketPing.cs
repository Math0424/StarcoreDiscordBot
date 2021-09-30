using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;
using static StarcoreDiscordBot.Networking.Pipelines;

namespace StarcoreDiscordBot.Networking.Packets
{
    [ProtoContract]
    class PacketPing : IPacket
    {
        public PacketPing() {}
        public int GetID() => 0;
    }
}
