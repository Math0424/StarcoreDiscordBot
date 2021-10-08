using Discord;
using Discord.WebSocket;
using ImageMagick;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using static StarcoreDiscordBot.BlueprintReader;

namespace StarcoreDiscordBot
{
    [DataContract]
    class WCTournament
    {
        private static string DataFolder = Path.Combine(Utils.GetDataFolder(), "Tornaments/");
        private BlueprintReader Reader;

        public bool IsFull
        {
            get { return MaxTeams != -1 && (Entries?.Count ?? 0) >= MaxTeams; }
        }

        public static void Load()
        {
            if (!Directory.Exists(DataFolder))
            {
                Directory.CreateDirectory(DataFolder);
            }
            foreach (var x in Directory.GetFiles(DataFolder))
            {
                WCTournament torn = ReaderWriter.Load<WCTournament>(x);
                Data.RegisteredTournaments.Add(torn);
            }

            foreach (var x in Data.RegisteredTournaments)
            {
                x.UpdateTournament();
            }
        }

        public BlueprintInfo BlueprintData(ref WCBlueprint blueprint)
        {
            return Reader.ReadBP(blueprint);
        }

        public bool GetBlueprintInfo(int blueprintId, out WCBlueprint blueprint, out BlueprintInfo info)
        {
            blueprint = Data.GetBlueprint(blueprintId);
            if (blueprint != null)
            {
                info = Reader.ReadBP(blueprint);
                return true;
            }
            info = default;
            return false;
        }
         
        public bool CanAddBlueprint(TournamentEntry entry, int blueprintId, out BlueprintInfo info, out string error)
        {
            if (GetBlueprintInfo(blueprintId, out _, out BlueprintInfo data))
            {
                info = data;
                if (!info.FileNotFound)
                {
                    if (info.HasLargeGrid && !LargeGridAllowed)
                    {
                        error = "Large grid not allowed!";
                    } 
                    else if (info.HasSmallGrid && !SmallGridAllowed)
                    {
                        error = "Small grid not allowed!";
                    }
                    else if (MaxShipBlocks != -1 && MaxShipBlocks < info.BlockCount)
                    {
                        error = $"Over ship block limit of {MaxShipBlocks} (Ship has {info.BlockCount} blocks)!";
                    }
                    else if (MinShipBlocks != -1 && MinShipBlocks > info.BlockCount)
                    {
                        error = $"Under ship block limit of {MinShipBlocks} (Ship has {info.BlockCount} blocks)!";
                    }
                    else if (MaxShipBattlePoints != -1 && MaxShipBattlePoints < info.BattlePoints)
                    {
                        error = $"Over ship battle point limit of {MaxShipBattlePoints} (Ship has {info.BattlePoints} BP)!";
                    }
                    else if (MinShipBattlePoints != -1 && MinShipBattlePoints > info.BattlePoints)
                    {
                        error = $"Under ship battle point limit of {MinShipBattlePoints} (Ship has {info.BattlePoints} BP)!";
                    }
                    else if (MaxTeamBattlePoints != -1 && MaxTeamBattlePoints < entry.TeamBattlePoints + info.BattlePoints)
                    {
                        error = $"Adding ship would max out team BattlePoints ({MaxTeamBattlePoints}) this Ship has {info.BattlePoints} BP and team has {entry.TeamBattlePoints} BP in use!";
                    }
                    else if (MaxTeamBlocks != -1 && MaxTeamBlocks < entry.TeamBlocks + info.BlockCount)
                    {
                        error = $"Adding ship would max out team Block limit ({MaxTeamBlocks}) this Ship has {info.BlockCount} Blocks and team has {entry.TeamBlocks} Blocks in use!!";
                    }
                    else
                    {
                        error = string.Empty;
                        return true;
                    }
                }
                else
                    error = "Blueprint sbc not found!";
            } 
            else
                error = "Blueprint not found!";

            info = default;
            return false;
        }

        public List<TournamentEntry> GetEntrys()
        {
            return new List<TournamentEntry>(Entries);
        }

        public bool GetEntry(WCTeam team, out TournamentEntry entry)
        {
            foreach (var c in Entries)
                if (c.TeamID == team.TeamID)
                {
                    entry = c;
                    return true;
                }

            entry = null;
            return false;
        }

        public bool AddEntry(WCTeam team)
        {
            foreach (var c in Entries)
                if (c.TeamID == team.TeamID)
                    return false;

            TournamentEntry entry = new TournamentEntry();
            entry.TeamID = team.TeamID;
            Entries.Add(entry);
            Role.UpdateMembers(team.GetPlayers());
            Utils.LogToDiscord(team.Name, $"Added to entry pool of '{Name}'");
            Save();
            return true;
        }

        public bool RemoveEntry(WCTeam team)
        {
            foreach(var c in Entries)
            {
                if (c.TeamID == team.TeamID)
                {
                    Entries.Remove(c);
                    Role.RemoveMembers(team.GetPlayers());
                    Utils.LogToDiscord(team.Name, $"Removed from entry pool of '{Name}'");
                    Save();
                    return true;
                }
            }
            return false;
        }

        public void UpdateTournament()
        {
            if (Entries == null)
                Entries = new List<TournamentEntry>();

            Role = new DiscordRole(Abv, RoleID);
            Role.RoleIdUpdated += RoleIDUpdated;

            foreach (var e in Entries)
            {
                Role.UpdateMembers(e.GetTeam()?.GetPlayers());
            }

            Entries.RemoveAll((e) => !e.IsValid());

            Reader = new BlueprintReader(DataSheet);
        }

        public void SetReaderPath(string data)
        {
            Reader = new BlueprintReader(data);
            DataSheet = data;
            Save();
        }

        public void RoleIDUpdated(ulong id)
        {
            RoleID = id;
            Save();
        }

        public void Save()
        {
            if (!Data.RegisteredTournaments.Contains(this))
            { 
                Data.RegisteredTournaments.Add(this);
            }
            string savePath = Path.Combine(DataFolder, TornID.ToString() + ".data");
            File.Delete(savePath);
            ReaderWriter.Save(this, savePath);
        }

        public void Delete()
        {
            Data.RegisteredTournaments.Remove(this);
            string savePath = Path.Combine(DataFolder, TornID.ToString() + ".data");
            File.Delete(savePath);
            if (CustomIcon)
            {
                File.Delete(Path.Combine(Data.IconFolder, $"{TornID}.png"));
            }
        }

        public MagickImage GetIcon()
        {
            return new MagickImage(CustomIcon ? Path.Combine(Data.IconFolder, $"{TornID}.png") : Data.DefaultIcon);
        }

        public SocketTextChannel GetGeneral()
        {
            return Bot.Instance.Client.GetChannel(GeneralChannel) as SocketTextChannel;
        }

        private DiscordRole Role;

        [DataMember] public int TornID;
        [DataMember] public ulong RoleID;
        [DataMember] public byte ServerID = 255;
        [DataMember] public ulong GeneralChannel;
        [DataMember] public ulong AdminChannel;

        [DataMember] public string Name;
        [DataMember] public string Abv;
        [DataMember] private List<TournamentEntry> Entries;
        [DataMember] public bool CustomIcon = false;

        [DataMember] public string DataSheet;
        [DataMember] public bool Locked;
        [DataMember] public bool SmallGridAllowed;
        [DataMember] public bool LargeGridAllowed;

        [DataMember] public int MinShipBattlePoints;
        [DataMember] public int MinShipBlocks;

        [DataMember] public int MaxShipBattlePoints;
        [DataMember] public int MaxShipBlocks;

        [DataMember] public int MaxTeamBattlePoints;
        [DataMember] public int MaxTeamBlocks;

        [DataMember] public int MaxTeams;
        [DataMember] public int MaxTeamShips;

    }
}
