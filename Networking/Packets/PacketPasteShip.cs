using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;
using static StarcoreDiscordBot.Networking.Pipelines;

namespace StarcoreDiscordBot.Networking.Packets
{
    [ProtoContract]
    class PacketPasteShip : IPacket
    {
        public PacketPasteShip() { }
        public PacketPasteShip(string Ship, int X, int Y, int Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
            this.Ship = Ship;
        }
        [ProtoMember(1)] public string Ship;
        [ProtoMember(2)] public int X;
        [ProtoMember(3)] public int Y;
        [ProtoMember(4)] public int Z;
        public int GetID() => 2;
    }
}
