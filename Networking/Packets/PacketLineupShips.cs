using ProtoBuf;
using static StarcoreDiscordBot.Networking.Pipelines;

namespace StarcoreDiscordBot.Networking.Packets
{
    [ProtoContract]
    class PacketLineupShips : IPacket
    {
        public PacketLineupShips() { }
        public PacketLineupShips(TournamentEntry[] entries)
        {
            Teams = new Team[entries.Length];
            for (int j = 0; j < entries.Length; j++)
            {
                Teams[j] = Team.Create(entries[j]);
            }
        }

        [ProtoMember(1)] public Team[] Teams;

        public int GetID() => 3;
    }
}
