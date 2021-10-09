using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;
using static StarcoreDiscordBot.Networking.Pipelines;

namespace StarcoreDiscordBot.Networking.Packets
{
    [ProtoContract]
    class PacketWhitelist : IPacket
    {
        [ProtoMember(1)] public bool Enabled;
        public PacketWhitelist(bool enabled) 
        {
            this.Enabled = enabled;
        }
        public int GetID() => 9;
    }
}
