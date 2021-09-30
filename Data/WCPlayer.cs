using Discord.WebSocket;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
namespace StarcoreDiscordBot
{

    [DataContract]
    public class WCPlayer
    {
        private static string DataFolder = Path.Combine(Utils.GetDataFolder(), "PlayerData/");

        public static void Load()
        {
            if (!Directory.Exists(DataFolder))
            {
                Directory.CreateDirectory(DataFolder);
            }
            foreach (var x in Directory.GetFiles(DataFolder))
            {
                Data.RegisteredPlayers.Add(ReaderWriter.Load<WCPlayer>(x));
            }
        }

        public void Save()
        {
            if (!Data.RegisteredPlayers.Contains(this))
            {
                Data.RegisteredPlayers.Add(this);
            }
            string savePath = Path.Combine(DataFolder, DiscordID.ToString() + ".data");
            File.Delete(savePath);
            ReaderWriter.Save(this, savePath);
        }

        public string UserName()
        {
            return Utils.GetUsername(DiscordID);
        }

        public SocketUser Discord()
        {
            return Utils.GetUser(DiscordID);
        }

        public WCTeam GetTeam()
        {
            return Data.GetPlayerTeam(this);
        }

        public bool AddToRegistedBlueprints(int id)
        {
            if (RegisteredBlueprints == null)
                RegisteredBlueprints = new List<int>();
            if (RegisteredBlueprints.Contains(id))
                return false;

            RegisteredBlueprints.Add(id);
            Utils.LogToDiscord(Discord(), $"Has added blueprint {id}");
            Save();
            return true;
        }

        public bool RemoveFromRegistedBlueprints(int id)
        {
            if (RegisteredBlueprints == null)
                RegisteredBlueprints = new List<int>();
            if (!RegisteredBlueprints.Contains(id))
                return false;

            RegisteredBlueprints.Remove(id);
            Utils.LogToDiscord(Discord(), $"Has remove blueprint {id}");
            Save();
            return true;
        }

        public List<int> GetRegistedBlueprints()
        {
            if (RegisteredBlueprints == null)
                RegisteredBlueprints = new List<int>();
            return RegisteredBlueprints;
        }

        [DataMember] private List<int> RegisteredBlueprints;
        [DataMember] public int Wins;
        [DataMember] public int Losses;
        [DataMember] public ulong SteamID;
        [DataMember] public ulong DiscordID;

    }
}
