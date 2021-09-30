using Discord;
using ImageMagick;
using System;
using System.IO;
using System.Runtime.Serialization;

namespace StarcoreDiscordBot
{
    [DataContract]
    public class WCBlueprint
    {

        private static string DataFolder = Path.Combine(Utils.GetDataFolder(), "Blueprints\\");
        private static string SaveFolder = Path.Combine(DataFolder, "Data\\");


        public static void Load()
        {
            if (!Directory.Exists(DataFolder))
                Directory.CreateDirectory(DataFolder);

            if (!Directory.Exists(SaveFolder))
                Directory.CreateDirectory(SaveFolder);

            foreach (var x in Directory.GetFiles(DataFolder))
            {
                WCBlueprint blueprint = ReaderWriter.Load<WCBlueprint>(x);
                Data.RegisteredBlueprints.Add(blueprint);
            }
        }

        public void Save()
        {
            if (!Data.RegisteredBlueprints.Contains(this))
            {
                Data.RegisteredBlueprints.Add(this);
            }
            string savePath = Path.Combine(DataFolder, GID.ToString() + ".data");
            File.Delete(savePath);
            ReaderWriter.Save(this, savePath);
        }

        public void Delete()
        {
            Data.RegisteredBlueprints.Remove(this);
            string savePath = Path.Combine(DataFolder, GID.ToString() + ".data");
            File.Delete(savePath);
            Directory.Delete(GetSaveDirectory(), true);
        }

        public bool IsOwner(WCPlayer player)
        {
            return player.GetRegistedBlueprints().Contains(GID);
        }

        [DataMember] public int GID;

        [DataMember] public ulong Requester;
        [DataMember] public ulong Creator;
        [DataMember] public string Title;
        [DataMember] public DateTime Creation;
        [DataMember] public ulong SteamID;

        public string DownloadUUID = string.Empty;

        public bool Exists()
        {
            return Directory.Exists(GetSaveDirectory());
        }

        public string GetSaveDirectory()
        {
            return Path.Combine(SaveFolder, GID.ToString());
        }

        public string GetTempDir()
        {
            return Path.Combine(DataFolder, GID.ToString());
        }

        public string GetSBCPath()
        {
            return Path.Combine(SaveFolder, GID.ToString(), "bp.sbc");
        }

        public EmbedFieldBuilder GetEmbed()
        {
            EmbedFieldBuilder field = new EmbedFieldBuilder();
            field.Name = Title;
            field.Value = $"\nID: {GID}\nCreated: <t:{(int)Creation.Subtract(DateTime.UnixEpoch).TotalSeconds}:R>";
            return field;
        }

        public MagickImage GetThumbnail()
        {
            if (Exists())
            {
                string thumb = Path.Combine(SaveFolder, GID.ToString(), "thumb.png");
                if (File.Exists(thumb))
                {
                    return new MagickImage(thumb);
                }
            }
            return null;
        }

    }

    [DataContract]
    public struct BlueprintData
    {
        public int BlockCount;
        public int BattlePoints;
    }

}
