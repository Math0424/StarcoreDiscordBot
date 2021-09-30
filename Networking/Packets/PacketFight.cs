using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;
using static StarcoreDiscordBot.Networking.Pipelines;

namespace StarcoreDiscordBot.Networking.Packets
{
    [ProtoContract]
    class PacketFight : IPacket
    {
        public PacketFight() { }
        public PacketFight(TournamentEntry entry1, TournamentEntry entry2, MyTuple<int, int, int> Team1Spawn, MyTuple<int, int, int> Team2Spawn)
        {
            Team1 = Team.Create(entry1);
            Team2 = Team.Create(entry2);
            this.Team1Spawn = Team1Spawn;
            this.Team2Spawn = Team2Spawn;
        }

        [ProtoMember(1)] public Team Team1;
        [ProtoMember(2)] public Team Team2;
        [ProtoMember(3)] public MyTuple<int, int, int> Team1Spawn;
        [ProtoMember(4)] public MyTuple<int, int, int> Team2Spawn;

        public int GetID() => 5;
    }
}
