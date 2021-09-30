using Discord;
using Discord.WebSocket;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace StarcoreDiscordBot
{
    [DataContract]
    public class WCTeam
    {

        private static string DataFolder = Path.Combine(Utils.GetDataFolder(), "TeamData/");

        public static void Load()
        {
            if (!Directory.Exists(DataFolder))
            {
                Directory.CreateDirectory(DataFolder);
            }

            foreach (var x in Directory.GetFiles(DataFolder))
            {
                WCTeam team = ReaderWriter.Load<WCTeam>(x);
                Data.RegisteredTeams.Add(team);
            }

            foreach (var x in Data.RegisteredTeams)
            {
                x.LoadTeam();
            }
        }

        public void Save()
        {
            if (!Data.RegisteredTeams.Contains(this))
            {
                Data.RegisteredTeams.Add(this);
            }
            string savePath = Path.Combine(DataFolder, TeamID.ToString() + ".data");
            File.Delete(savePath);
            ReaderWriter.Save(this, savePath);
        }

        public void Delete()
        {
            Role.DeleteRole();
            Data.RegisteredTeams.Remove(this);
            string savePath = Path.Combine(DataFolder, TeamID.ToString() + ".data");
            File.Delete(savePath);
            if (CustomIcon)
            {
                File.Delete(Data.IconFolder + "\\" + TeamID + ".png");
            }
        }

        public WCTeam() {}

        public WCTeam(ulong leader)
        {
            Players = new List<ulong>();
            Players.Add(leader);
        }

        public void LoadTeam()
        {
            if (MMR == 0)
            {
                MMR = 1500;
            }
            if (Subs == null)
                Subs = new List<ulong>();
            if (Players == null)
                Players = new List<ulong>();

            Role = new DiscordRole(Tag, RoleID);
            Role.RoleIdUpdated += RoleIDUpdated;
            Role.UpdateMembers(GetPlayers());
        }

        public void RoleIDUpdated(ulong id)
        {
            RoleID = id;
            Save();
        }

        public void MessageLeader(string message)
        {
            WCPlayer player = Data.GetPlayer(Leader);
            if (player != null)
                player.Discord()?.CreateDMChannelAsync().Result.SendMessageAsync(Utils.Format(message, player, this));
        }

        public void MessagePlayers(string message, Embed embed = null)
        {
            foreach (var p in Players)
            {
                WCPlayer player = Data.GetPlayer(p);
                if (player != null)
                {
                    player.Discord()?.CreateDMChannelAsync()?.Result?.SendMessageAsync(Utils.Format(message, player, this), false, embed);
                }
            }
        }

        public MagickImage GetIcon()
        {
            return new MagickImage(CustomIcon ? Path.Combine(Data.IconFolder, $"{TeamID}.png") : Data.DefaultIcon);
        }

        public string GetsID()
        {
            return $"{Tag}";
        }

        public bool AddPlayer(ulong id, bool silent = false)
        {
            var player = Data.GetPlayer(id);
            if (player != null)
            {
                Players.Add(id);
                Save();
                Role.AddMember(id);
                if (!silent)
                    Utils.LogToDiscord($"{Name} ({Tag})", $"Added {player.UserName()}");
                return true;
            } 
            else
            {
                return false;
            }
        }

        public bool RemovePlayer(ulong id)
        {
            if (Players.Contains(id))
            {
                Players.Remove(id);
                Save();
                Role.RemoveMember(id);
                Utils.LogToDiscord($"{Name} ({Tag})", $"Removed {Utils.GetUsername(id)}");
                if (Players.Count == 0 || id == Leader)
                {
                    Delete();
                    MessagePlayers($"{Name} has been deleted");
                    Utils.LogToDiscord($"{Name} ({Tag})", $"Has been deleted");
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public List<ulong> GetPlayers()
        {
            return new List<ulong>(Players);
        }

        public List<ulong> GetPlayersAndSubs()
        {
            var r = new List<ulong>(Players);
            foreach(var x in Subs)
            {
                r.Add(x);
            }
            return r;
        }

        public List<WCPlayer> GetWCPlayers(bool subs = false)
        {
            List<WCPlayer> list = new List<WCPlayer>();
            foreach (var p in Players)
                list.Add(Data.GetPlayer(p));
            if (subs && Subs != null)
            {
                foreach (var p in Subs)
                    list.Add(Data.GetPlayer(p));
            }
            return list;
        }

        public List<ulong> GetPlayerSteamIDs(bool subs = false)
        {
            List<ulong> list = new List<ulong>();
            foreach(var p in Players)
                list.Add(Data.GetPlayer(p).SteamID);
            if (subs && Subs != null)
            {
                foreach (var p in Subs)
                    list.Add(Data.GetPlayer(p).SteamID);
            }
            return list;
        }

        public TeamColor GetTeamColor()
        {
            return Color ?? TeamColor.Default;
        }

        public void SetTeamColor(TeamColor color)
        {
            Color = color;
            Save();
        }

        private DiscordRole Role;

        [DataMember] public ulong TeamID;
        [DataMember] public string Name;
        [DataMember] public string Tag;
        [DataMember] public bool CustomIcon = false;
        [DataMember] private TeamColor Color;
        [DataMember] public ulong RoleID;

        [DataMember] public int MMR;
        [DataMember] public int GlobalWins;
        [DataMember] public int GlobalLosses;

        [DataMember] public ulong Leader;
        [DataMember] private List<ulong> Players;
        [DataMember] public List<ulong> Subs;
        [DataMember] public DateTime Creation;

    }
}
