using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace StarcoreDiscordBot
{

    [DataContract]
    class Config
    {
        public static Config Instance { get; private set; }

        [DataMember]
        public int PlayerBPLimit = 1;
        [DataMember]
        public int TeamSubLimit = 4;

        [DataMember]
        public ulong GuildID = 0;
        [DataMember]
        public ulong LogChannel = 0;


        [DataMember]
        public ulong AdminRole = 0;
        [DataMember]
        public ulong ManagerRole = 0;

        public static void Load()
        {
            string path = Path.Combine(Utils.GetDataFolder(), "config.bot");

            if (!File.Exists(path))
            {
                Instance = new Config();
                ReaderWriter.Save(Instance, path);
            } 
            else
            {
                Instance = ReaderWriter.Load<Config>(path);
            }
        }

        public void Save()
        {
            string path = Path.Combine(Utils.GetDataFolder(), "config.bot");
            File.Delete(path);
            ReaderWriter.Save(this, path);
        }

    }
}
