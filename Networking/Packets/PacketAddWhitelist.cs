using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;
using static StarcoreDiscordBot.Networking.Pipelines;

namespace StarcoreDiscordBot.Networking.Packets
{
    [ProtoContract]
    class PacketAddWhitelist : IPacket
    {
        public PacketAddWhitelist() { }
        public PacketAddWhitelist(ulong User)
        {
            this.User = User;
        }
        [ProtoMember(1)] public ulong User;
        public int GetID() => 1;
    }
}
