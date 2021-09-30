using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace StarcoreDiscordBot
{

    [DataContract]
    class Data
    {
        public static string IconFolder = Path.Combine(Utils.GetDataFolder(), "Icons\\");
        public static string DefaultIcon = Path.Combine(Utils.GetDataFolder(), "Icons\\DefaultIcon.png");

        public static List<WCTeam> RegisteredTeams = new List<WCTeam>();
        public static List<WCPlayer> RegisteredPlayers = new List<WCPlayer>();
        public static List<WCTournament> RegisteredTournaments = new List<WCTournament>();
        public static List<WCBlueprint> RegisteredBlueprints = new List<WCBlueprint>();

        public static void Load()
        {
            if (!Directory.Exists(IconFolder))
            {
                Directory.CreateDirectory(IconFolder);
            }
            if (!File.Exists(IconFolder + "\\DefaultIcon.png"))
            {
                File.Copy(Utils.GetResourcesFolder() + "\\DefaultIcon.png", IconFolder + "\\DefaultIcon.png");
            }
        }

        public static WCTournament GetTournament(int TournID)
        {
            foreach(var x in RegisteredTournaments)
            {
                if (x.TornID == TournID)
                {
                    return x;
                }
            }
            return null;
        }

        public static WCBlueprint GetBlueprint(int BlueprintID)
        {
            foreach (var x in RegisteredBlueprints)
            {
                if (x.GID == BlueprintID)
                {
                    return x;
                }
            }
            return null;
        }

        public static WCPlayer GetPlayer(ulong MyDiscordID)
        {
            foreach (var x in RegisteredPlayers)
            {
                if (x.DiscordID == MyDiscordID)
                {
                    return x;
                }
            }
            return null;
        }

        public static bool GetOrCreatePlayer(ulong MyDiscordID, out WCPlayer player)
        {
            player = GetPlayer(MyDiscordID);
            if (player != null)
            {
                return true;
            }

            var p = new WCPlayer()
            {
                DiscordID = MyDiscordID
            };
            p.Save();
            player = p;
            return false;
        }

        public static WCTeam GetPlayerTeam(WCPlayer player)
        {
            if (player == null)
                return null;
            foreach (WCTeam t in RegisteredTeams)
            {
                if (t.GetPlayers().Contains(player.DiscordID))
                {
                    return t;
                }
            }
            return null;
        }

        public static WCTeam GetTeam(string id)
        {
            if (id.Contains('_') || id.Contains('-'))
            {
                string[] sId = id.Split('_', '-');
                if (sId.Length == 2)
                {
                    foreach (WCTeam t in RegisteredTeams)
                    {
                        if (t.Tag == sId[0])
                        {
                            return t;
                        }
                    }
                }
            }
            else if(id.Length == 3)
            {
                foreach (WCTeam t in RegisteredTeams)
                {
                    if (t.Tag == id)
                    {
                        return t;
                    }
                }
            }
            else
            {
                ulong fID;
                if (ulong.TryParse(id, out fID))
                {
                    foreach (WCTeam t in RegisteredTeams)
                        if (t.TeamID == fID)
                            return t;
                }
            }
            return null;
        }

        public static WCTeam GetTeam(ulong id)
        {
            foreach (WCTeam t in RegisteredTeams)
                if (t.TeamID == id)
                    return t;
            return null;
        }

        public static WCTeam GetSubTeam(WCPlayer player)
        {
            if (player == null)
                return null;
            foreach (WCTeam t in RegisteredTeams)
            {
                if (t.Subs != null && t.Subs.Contains(player.DiscordID))
                {
                    return t;
                }
            }
            return null;
        }

        public static bool CanSetTeamName(string name)
        {
            foreach (WCTeam t in RegisteredTeams)
            {
                if (t.Name.ToLower().Replace(" ", "").Equals(name.ToLower().Replace(" ", "")))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool CanSetTeamTag(string tag)
        {
            foreach (WCTeam t in RegisteredTeams)
            {
                if (t.Tag.ToLower().Equals(tag.ToLower()))
                {
                    return false;
                }
            }
            return true;
        }

    }
}
