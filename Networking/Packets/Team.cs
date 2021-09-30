using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarcoreDiscordBot.Networking.Packets
{
    [ProtoContract]
    struct Team
    {
        [ProtoMember(1)] public string TeamName;
        [ProtoMember(2)] public string TeamTag;
        [ProtoMember(3)] public Tuple<int, int, int> TeamColor;
        [ProtoMember(4)] public Pair<ulong, string>[] Players;
        [ProtoMember(5)] public Pair<ulong, string>[] Ships;

        public static Team Create(TournamentEntry entry)
        {
            Team returned = new Team();

            var team = Data.GetTeam(entry.TeamID);
            if (team != null)
            {
                var color = team.GetTeamColor();

                returned.TeamColor = new Tuple<int, int, int>(color.R, color.G, color.B);
                returned.TeamName = team.Name;
                returned.TeamTag = team.Tag;
                returned.Players = GetPlayers(team);
                returned.Ships = GetShips(entry);
            }
            else
            {
                Utils.Log($"Cannot find team {entry.TeamID}");
            }

            return returned;
        }

        private static Pair<ulong, string>[] GetShips(TournamentEntry team)
        {
            var ships = team.GetShips();
            var returnShips = new Pair<ulong, string>[ships.Count];
            for (int i = 0; i < ships.Count; i++)
            {
                var bp = Data.GetBlueprint(ships[i]);
                returnShips[i] = new Pair<ulong, string>(bp.Requester, bp.GetSBCPath());
            }
            return returnShips;
        }

        private static Pair<ulong, string>[] GetPlayers(WCTeam team)
        {
            var players = team.GetPlayersAndSubs();
            var returnedPlayers = new Pair<ulong, string>[players.Count];
            for (int i = 0; i < players.Count; i++)
            {
                returnedPlayers[i] = new Pair<ulong, string>(players[i], Data.GetPlayer(players[i]).UserName());
            }
            return returnedPlayers;
        }

    }
}
