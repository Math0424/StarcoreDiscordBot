using System.Collections.Generic;
using System.Runtime.Serialization;

namespace StarcoreDiscordBot
{
    [DataContract]
    class TournamentEntry
    {

        public bool IsValid()
        {
            return Data.GetTeam(TeamID) != null;
        }

        public WCTeam GetTeam()
        {
            return Data.GetTeam(TeamID);
        }

        public void AddShip(int id, int battlePoints, int blockCount)
        {
            if (Ships == null)
                Ships = new List<int>();

            TeamBlocks += blockCount;
            TeamBattlePoints += battlePoints;

            Ships.Add(id);
        }

        public bool RemoveShip(int id, int battlePoints, int blockCount)
        {
            if (Ships == null)
                return false;

            TeamBlocks -= blockCount;
            TeamBattlePoints -= battlePoints;

            return Ships.Remove(id);
        }

        public List<int> GetShips()
        {
            if (Ships == null)
                Ships = new List<int>();
            return Ships;
        }

        [DataMember] public ulong TeamID;
        [DataMember] private List<int> Ships;

        [DataMember] public int TeamBattlePoints;
        [DataMember] public int TeamBlocks;

        [DataMember] public int Wins;
        [DataMember] public int Losses;
        [DataMember] public int MMR;
        [DataMember] public LinkedList<Fight> Fights;

        [DataContract]
        public class Fight
        {
            [DataMember] public ulong Enemy;
            [DataMember] public EnemyType Type;
            [DataMember] public Outcome Outcome;
        }

        public enum EnemyType
        {
            Team,
            Player,
        }

        public enum Outcome 
        { 
            Win,
            Loss,
            Draw,
            Forfeit,
        }
    }
}
