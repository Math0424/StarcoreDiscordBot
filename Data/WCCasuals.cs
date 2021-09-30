using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace StarcoreDiscordBot
{
    [DataContract]
    class WCCasuals
    {
        public static WCCasuals Instance;
        private static string DataFolder = Path.Combine(Utils.GetDataFolder(), "Casuals/");

        public static void Load()
        {
            if (!Directory.Exists(DataFolder))
            {
                Directory.CreateDirectory(DataFolder);
            }
            string path = Path.Combine(DataFolder, "CasualPlay.data");
            if (!File.Exists(path))
            {
                Instance = new WCCasuals();
                Instance.Save();
            }
            else
                Instance = ReaderWriter.Load<WCCasuals>(Path.Combine(DataFolder, "CasualPlay.data"));
        }

        public void Save()
        {
            string savePath = Path.Combine(DataFolder, "CasualPlay.data");
            File.Delete(savePath);
            ReaderWriter.Save(this, savePath);
        }

        public void GetOpenServer()
        {

        }

        public bool IsServerOpen()
        {
            return true;
        }

        public int GetWaitTime()
        {
            return 1;
        }

        public void PrepareTeamFight(WCTeam team1, WCTeam team2)
        {
            
        }

        public void Prepare1v1(WCPlayer player1, WCPlayer player2, WCBlueprint blueprint1, WCBlueprint blueprint2)
        {

        }

        [DataMember] public ulong GeneralChannel;
        [DataMember] public ulong AdminChannel;

        [DataMember] public byte ServerIDStart = 25;
        [DataMember] public byte ServerCount = 5;

        [DataMember] public List<TournamentEntry> Entries;
    }
}
