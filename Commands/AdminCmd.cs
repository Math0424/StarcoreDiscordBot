using Discord;
using Discord.Commands;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
#if false
namespace StarcoreDiscordBot
{

    [Name("Admin Module")]
    [Remarks("admin")]
    [Summary("Admin only commands")]
    public class AdminCmd : ModuleBase<SocketCommandContext>
    {

        [Group("admin")]
        [Alias("a")]
        public class Admin : ModuleBase<SocketCommandContext>
        {
            [Command]
            public async Task MainCmd()
            {
                if (Context.Channel.Id != Bot.Instance.Admin.Id) { return; }


                EmbedBuilder builder = new EmbedBuilder();
                builder.Color = Color.Red;

                int BPCount = 0;

                foreach (var p in Data.RegisteredPlayers)
                {
                    BPCount += p.RegisteredShips.Count;
                }

                int ActiveTeams = 0;

                foreach (var t in Data.RegisteredTeams)
                {
                    if (t.IsActive)
                    {
                        ActiveTeams++;
                    }
                }

                EmbedFieldBuilder stats = new EmbedFieldBuilder();
                stats.Name = "Stats";
                stats.Value = $"TeamCount {Data.RegisteredTeams.Count}\nActive teams {ActiveTeams}\nPlayerCount {Data.RegisteredPlayers.Count}\nBpCount {BPCount}";
                builder.Fields.Add(stats);

                await Context.Channel.SendMessageAsync(null, false, builder.Build());
            }


            [NamedArgumentType]
            public class ExportArgs
            {
                public int? Amount { get; set; }
                public int? Players { get; set; }
                public int? PlayersOrMore { get; set; }
                public bool? Random { get; set; }
                public bool? Active { get; set; }
                public bool? Data { get; set; }
            }

            [Command("export")]
            [Summary("export all teams in a txt form")]
            public async Task ExportAllTeams([Name("Amount Random Players PlayersOrMore Active")] ExportArgs args = null)
            {
                if (Context.Channel.Id != Bot.Instance.Admin.Id) { return; }

                await ReplyAsync("Preparing data");

                List<WCTeam> exportTeams = new List<WCTeam>(Data.RegisteredTeams);

                if (args != null)
                {
                    if (args.Random != null && args.Random.Value)
                    {
                        exportTeams = exportTeams.OrderBy(e => Utils.Rand.Next()).ToList();
                    }

                    if (args.Active != null)
                    {
                        using var i = new List<WCTeam>(exportTeams).GetEnumerator();
                        while (i.MoveNext())
                        {
                            if (i.Current.IsActive != args.Active.Value)
                            {
                                exportTeams.Remove(i.Current);
                            }
                        }
                    }

                    if (args.Players != null)
                    {
                        using var i = new List<WCTeam>(exportTeams).GetEnumerator();
                        while (i.MoveNext())
                        {
                            if (args.PlayersOrMore != null)
                            {
                                if (i.Current.GetPlayers().Count < args.PlayersOrMore.Value)
                                {
                                    exportTeams.Remove(i.Current);
                                }
                            }
                            else
                            {
                                if (i.Current.GetPlayers().Count != args.Players.Value)
                                {
                                    exportTeams.Remove(i.Current);
                                }
                            }
                        }
                    }

                    if (args.Amount != null)
                    {
                        using var i = new List<WCTeam>(exportTeams).GetEnumerator();
                        int count = Math.Min(args.Amount.Value, exportTeams.Count);
                        while (i.MoveNext())
                        {
                            count--;
                            if (count < 0)
                            {
                                exportTeams.Remove(i.Current);
                            }
                        }
                    }
                }

                using (MemoryStream stream = new MemoryStream())
                {
                    using StreamWriter writer = new StreamWriter(stream);
                    Dictionary<string, int> Blocks = new Dictionary<string, int>();
                    int GlobalBlockCount = 0, GlobalBP = 0;

                    foreach (var x in exportTeams)
                    {
                        writer.WriteLine("---------------------------------");
                        writer.WriteLine($"Name: {x.Name} ({x.Tag})");
                        writer.WriteLine($"sID: {x.GetsID()}");
                        string builtString = "";
                        foreach (var m in x.GetPlayers())
                            builtString += $"<@{m}> ";
                        writer.WriteLine($"Members: {builtString}"); 
                        builtString = "";
                        if (x.Subs != null)
                            foreach (var m in x.Subs)
                                builtString += $"<@{m}> ";
                        writer.WriteLine($"Subs: {builtString}");
                        builtString = "";
                        foreach (var m in x.Ships)
                            builtString += $"{m.Id} ";
                        writer.WriteLine($"Ships: {builtString}\n");

                        if (args != null && args.Data.HasValue && args.Data.Value)
                        {
                            foreach(var s in x.Ships)
                            {
                                BlueprintReader bp = new BlueprintReader(s);
                                GlobalBlockCount += bp.BlockCount;
                                GlobalBP += bp.BattlePoints;
                                writer.WriteLine($"{s.Title} : {(bp.IsLargeGrid ? "LargeGrid" : "!!SmallGrid!!")} {bp.BlockCount} blocks\nGeneral Data:");
                                foreach (var b in bp.Blocks.OrderBy(key => -key.Value))
                                {
                                    writer.WriteLine(string.Format("-   {0} x{1}", b.Key, b.Value));
                                }
                                foreach(var b in bp.Blocks)
                                {
                                    if (!Blocks.ContainsKey(b.Key))
                                        Blocks.Add(b.Key, 0);
                                    Blocks[b.Key] += b.Value;
                                }
                                writer.WriteLine("End Data\n");
                            }

                        }
                    }
                    writer.WriteLine("---------------------------------");
                    if (Blocks.Count != 0)
                    {
                        writer.WriteLine("FULL DATA REPORT:");
                        writer.WriteLine("GlobalBlockCount: " + GlobalBlockCount);
                        writer.WriteLine("GlobalBP: " + GlobalBP);
                        foreach (var b in Blocks.OrderBy(key => -key.Value))
                        {
                            writer.WriteLine(string.Format("-   {0} x{1}", b.Key, b.Value));
                        }
                    }

                    writer.Flush();
                    stream.Position = 0;
                    await Context.Channel.SendFileAsync(stream, "teams.txt", "Exported " + exportTeams.Count + " teams");
                }
            }

            [NamedArgumentType]
            public class TeamAdminArgs
            {
                public bool? Active { get; set; }
                public ulong? SetLeader { get; set; }
                public string? SetTag { get; set; }
                public string? SetName { get; set; }
                public int? SetTeamNum { get; set; }
                public int? SetTeamElo { get; set; }
            }

            [Command("modifyTeam")]
            [Summary("Modify select team")]
            public async Task ModifyTeam(string teamID, [Name("Active SetLeader SetName SetTag SetTeamNum SetTeamElo")] TeamAdminArgs args = null)
            {
                if (Context.Channel.Id != Bot.Instance.Admin.Id) { return; }

                WCTeam team = Data.GetTeam(teamID);
                if (team == null)
                {
                    await ReplyAsync($"Team {teamID} not found!");
                    return;
                }

                if (args != null)
                {
                    if (args.Active.HasValue)
                    {
                        team.IsActive = args.Active.Value;
                        await ReplyAsync("Set team activity to " + team.IsActive);
                    }

                    if (args.SetLeader.HasValue)
                    {
                        team.Leader = args.SetLeader.Value;
                        await ReplyAsync("Set team leader to " + team.Leader);
                    }

                    if (!string.IsNullOrEmpty(args.SetTag))
                    {
                        team.Tag = args.SetTag;
                        await ReplyAsync("Set team tag to " + team.Tag);
                    }

                    if (!string.IsNullOrEmpty(args.SetName))
                    {
                        team.Name = args.SetName;
                        await ReplyAsync("Set team name to " + team.Name);
                    }

                    if (args.SetTeamNum.HasValue)
                    {
                        team.TeamNumber = args.SetTeamNum.Value;
                        await ReplyAsync("Set team number to " + team.TeamNumber);
                    }

                    if (args.SetTeamElo.HasValue)
                    {
                        team.Elo = args.SetTeamElo.Value;
                        await ReplyAsync("Set team elo to " + team.Elo);
                    }

                }

                team.Save();

            }

            [Command("lineup")]
            [Summary("setup the server with selected team ships")]
            public async Task Lineup(params string[] teamsRaw)
            {
                if (Context.Channel.Id != Bot.Instance.Admin.Id) { return; }

                var data = new Pipelines.Data();

                List<WCTeam> teams = new List<WCTeam>();

                if (teamsRaw.Length == 1 && teamsRaw[0].ToLower() == "all")
                {
                    ReplyAsync("Preparing all teams");
                    foreach (var t in Data.RegisteredTeams)
                        teams.Add(t);
                }
                else if (teamsRaw.Length == 1 && teamsRaw[0].ToLower() == "active")
                {
                    ReplyAsync("Preparing all active teams");
                    foreach (var t in Data.RegisteredTeams)
                        if (t.IsActive)
                            teams.Add(t);
                }
                else
                {
                    foreach (string s in teamsRaw)
                    {
                        var t = Data.GetTeam(s);
                        if (t != null)
                        {
                            teams.Add(t);
                        }
                        else
                        {
                            ReplyAsync("Unknown team " + s);
                        }
                    }
                }

                int count = 0;
                foreach (var t in teams)
                {

                    if (t.Ships.Count == 0)
                    {
                        Utils.Log("No ships found for '" + t.Name + "'");
                        continue;
                    }

                    var teamList = t.GetWCPlayers(true);
                    foreach (var p in teamList)
                        if (!data.players.ContainsKey(p.SteamID))
                            data.players.Add(p.SteamID, new Pair<long, string>(0, Utils.GetUsername(p.DiscordID)));


                    data.factions.Add(new MyTuple<string, string, ulong[]>(t.Tag, t.Name, t.GetPlayerSteamIDs(true).ToArray()));

                    count += t.Ships.Count;

                    string[] paths = new string[t.Ships.Count];
                    ulong[] owners = new ulong[t.Ships.Count];

                    for (int i = 0; i < t.Ships.Count; i++)
                    {
                        paths[i] = t.Ships[i].GetPath();
                        owners[i] = Data.GetPlayer(t.Ships[i].Requester).SteamID;
                    }

                    data.showcaseBlueprints.Add(new Pair<string[], ulong[]>(paths, owners));
                }

                data.action = 1;
                Utils.Log("Writing to server");
                SendData(data);
                Utils.Log("Sent to server");
                await ReplyAsync("Sent " + count + " ships to the server");
            }

            [Command("addWhitelist")]
            [Summary("Add a member to the server whitelist")]
            public async Task AddWhitelist(ulong id)
            {
                if (Context.Channel.Id != Bot.Instance.Admin.Id) { return; }

                var data = new Pipelines.Data();
                data.players.Add(id, new Pair<long, string>(0, Utils.GetUsername(id)));

                data.action = 4;
                Utils.Log("Writing to server");
                SendData(data);
                Utils.Log("Sent to server");
                await ReplyAsync($"Adding {id} to the server whitelist...");
            }

            [Command("pasteShip")]
            [Summary("paste one ship")]
            public async Task PasteSingleShip(string sID, ulong shipId, int x, int y, int z)
            {
                if (Context.Channel.Id != Bot.Instance.Admin.Id) { return; }

                WCTeam team = Data.GetTeam(sID);
                if (team == null)
                {
                    await ReplyAsync($"Team {sID} not found!");
                    return;
                }

                WCBlueprint bp = null;
                foreach(var s in team.Ships)
                {
                    if(s.Requester == shipId || s.Id == shipId)
                    {
                        bp = s;
                        break;
                    }
                }
                if (bp == null)
                {
                    await ReplyAsync($"Ship not found!");
                    return;
                }

                var data = new Pipelines.Data();

                data.players.Add(Data.GetPlayer(bp.Requester)?.SteamID ?? 0, new Pair<long, string>(0, Utils.GetUsername(bp.Requester)));
                var list = new List<Pair<string, ulong>>();
                list.Add(new Pair<string, ulong>(bp.GetPath(), bp.Requester));
                data.factionBlueprints.Add(new Pair<MyTuple<int, int, int>, List<Pair<string, ulong>>>(new MyTuple<int, int, int>(x, y, z), list));

                data.action = 3;
                Utils.Log("Writing to server");
                SendData(data);
                Utils.Log("Sent to server");
                await ReplyAsync($"Preparing ship on the server...");
            }

            [Command("queue")]
            [Summary("Prepare selected teams to kill eachother")]
            public async Task TeamQueue(params string[] teams)
            {
                if (Context.Channel.Id != Bot.Instance.Admin.Id) { return; }

                int teamCount = 0;
                var data = new Pipelines.Data();
                foreach (string s in teams)
                {
                    teamCount++;
                    WCTeam team = Data.GetTeam(s);
                    if (team == null)
                    {
                        ReplyAsync($"Team {s} not found!");
                        continue;
                    }
                    if (team.Ships.Count == 0)
                    {
                        ReplyAsync($"{s} has no registered ships!");
                        continue;
                    }

                    var teamList = team.GetWCPlayers(true);
                    foreach (var p in teamList)
                        if (!data.players.ContainsKey(p.SteamID))
                            data.players.Add(p.SteamID, new Pair<long, string>(0, Utils.GetUsername(p.DiscordID)));

                    data.factions.Add(new MyTuple<string, string, ulong[]>(team.Tag, team.Name, team.GetPlayerSteamIDs(true).ToArray()));

                    List<Pair<string, ulong>> teamShips = new List<Pair<string, ulong>>();
                    foreach (var ship in team.Ships)
                    {
                        teamShips.Add(new Pair<string, ulong>(ship.GetPath(), Data.GetPlayer(ship.Requester).SteamID));
                    }

                    MyTuple<int, int, int> teamPos;
                    if (teams.Length <= 2)
                    {
                        if (teamCount == 1)
                        {
                            Utils.Log("Making Blue spawn");
                            teamPos = new MyTuple<int, int, int>(-670, -210, -9700); //Blue
                        }
                        else
                        {
                            Utils.Log("Making Red spawn");
                            teamPos = new MyTuple<int, int, int>(668, 248, 9600);    //Red
                        }
                    }
                    else
                    {
                        double angle = (Math.PI * 2.0 / teams.Length) * teamCount;
                        int x = (int)(Math.Cos(angle) * 9700.0);
                        int z = (int)(Math.Sin(angle) * 9700.0);
                        teamPos = new MyTuple<int, int, int>(x, 0, z);
                    }

                    data.factionBlueprints.Add(new Pair<MyTuple<int, int, int>, List<Pair<string, ulong>>>(teamPos, teamShips));
                }

                data.action = 2;
                Utils.Log("Writing to server");
                SendData(data);
                Utils.Log("Sent to server");
                await ReplyAsync($"Preparing {teamCount} teams on the server...");
            }
            
            [Command("scan")]
            [Summary("scan all blueprints for issues")]
            public async Task CheckAllBP(bool activeOnly = true)
            {
                if (Context.Channel.Id != Bot.Instance.Admin.Id) { return; }
                string issues = "";
                foreach(var t in Data.RegisteredTeams)
                {
                    if (activeOnly && !t.IsActive)
                        continue;

                    bool foundIssues = false;
                    string newIssues = t.GetsID()+":\n";
                    int totalBP = 0;
                    if (t.Ships.Count == 0)
                    {
                        foundIssues = true;
                        newIssues += "- No ships\n";
                    }
                    foreach (var s in t.Ships)
                    {
                        BlueprintReader bp = new BlueprintReader(s);
                        if (!bp.IsLargeGrid)
                        {
                            foundIssues = true;
                            newIssues += "- Small grid ship\n";
                        }
                        totalBP += bp.BattlePoints;
                    }
                    if (totalBP > 35000)
                    {
                        foundIssues = true;
                        newIssues += $"- Over BP limit of 35000 ({totalBP})\n";
                    }
                    if (foundIssues)
                    {
                        issues += newIssues;
                    }
                }

                if (!string.IsNullOrEmpty(issues))
                {
                    await ReplyAsync("Issues found with the following teams:");
                    await ReplyAsync(issues);
                } 
                else
                {
                    await ReplyAsync("No issues found");
                }

            }

            [Command("fight")]
            [Summary("queue a fight")]
            public async Task TeamFight([Name("Team1ID")] string team1str = null, [Name("Team2ID")] string team2str = null, string ip = null, [Name("Announce (true/false)")]bool announce = false)
            {
                if (Context.Channel.Id != Bot.Instance.Admin.Id) { return; }

                WCTeam team1 = Data.GetTeam(team1str);
                if (team1 == null)
                {
                    await ReplyAsync("Team1 not found!");
                    return;
                }
                WCTeam team2 = Data.GetTeam(team2str);
                if (team2 == null)
                {
                    await ReplyAsync("Team2 not found!");
                    return;
                }
                if (ip == null)
                {
                    await ReplyAsync("enter a IP!");
                    return;
                }
                if (team1 == team2)
                {
                    await ReplyAsync("Cannot fight 2 of the same team!");
                    return;
                }

                var str = @$"
『{team1.Name}』vs『{team2.Name}』

【**You're up next! Please:**】
➽ log onto the server below
➽ spawn in ({team1.Tag} on Blue & {team2.Tag} on Red)
➽ enter your ships
➽ await further instructions and do not begin fighting.

▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂▂
steam://connect/{ip}
▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔";

                await TeamQueue(team1str, team2str);

                await TeamMsg(str, team1.TeamID.ToString(), team2.TeamID.ToString());

                if (announce)
                {
                    using MagickImage send = GenerateVsLogo(team1, team2);

                    EmbedBuilder builder = new EmbedBuilder();
                    builder.Color = Color.Red;

                    EmbedAuthorBuilder author = new EmbedAuthorBuilder();
                    author.Name = "Statistics";
                    builder.Author = author;
                    builder.Color = Color.Red;

                    int bp, bc;
                    Utils.TallyTeamData(team1, out bc, out bp);

                    EmbedFieldBuilder team1Field = new EmbedFieldBuilder();
                    team1Field.Name = team1.Name;
                    team1Field.Value = @$"{team1.GetPlayers().Count} players
{team1.Subs?.Count ?? 0} subs
Created {team1.Creation.ToString("MM/dd")}
{team1.Ships.Count} flyable ships
{bc} total blocks
{bp} total battlePoints";
                    builder.Fields.Add(team1Field);

                    Utils.TallyTeamData(team2, out bc, out bp);

                    EmbedFieldBuilder team2Field = new EmbedFieldBuilder();
                    team2Field.Name = team2.Name;
                    team2Field.Value = @$"{team2.GetPlayers().Count} players
{team2.Subs?.Count ?? 0} subs
Created {team2.Creation.ToString("MM/dd")}
{team2.Ships.Count} flyable ships
{bc} total blocks
{bp} total battlePoints";
                    builder.Fields.Add(team2Field);

                    using (Stream stream = new MemoryStream())
                    {
                        send.Write(stream);
                        stream.Position = 0;
                        await Bot.Instance.General.SendFileAsync(stream, "TeamMatch.png", null, false, builder.Build());
                    }
                }

            }

            private MagickImage GenerateVsLogo(WCTeam one, WCTeam two)
            {
                MagickImage background = new MagickImage(Path.Combine(Utils.GetResourcesFolder(), "VS.png"));

                using MagickImage teamIcon1 = one.GetIcon();
                using MagickImage teamIcon2 = two.GetIcon();

                background.Composite(teamIcon1, Gravity.Center, -294, 0, CompositeOperator.Atop);
                background.Composite(teamIcon2, Gravity.Center, 294, 0, CompositeOperator.Atop);

                int count = 0;
                foreach (var p in one.GetPlayers())
                {
                    var u = Utils.GetUser(p);
                    if (u != null)
                    {
                        using WebClient client = new WebClient();
                        using Stream stream = client.OpenRead(u.GetAvatarUrl(ImageFormat.Png) ?? u.GetDefaultAvatarUrl());
                        using MagickImage icon = new MagickImage(stream);
                        icon.Resize(48, 48);
                        icon.BackgroundColor = MagickColors.None;

                        icon.Distort(DistortMethod.DePolar, 0);
                        icon.VirtualPixelMethod = VirtualPixelMethod.HorizontalTile;
                        icon.BackgroundColor = MagickColors.None;
                        icon.Distort(DistortMethod.Polar, 0);

                        background.Composite(icon, Gravity.Center, -294 - 64 - 24 + (count * 50), -128 - 24, CompositeOperator.Atop);

                        count++;
                    }
                }

                count = 0;
                foreach (var p in two.GetPlayers())
                {
                    var u = Utils.GetUser(p);
                    if (u != null)
                    {
                        using WebClient client = new WebClient();
                        using Stream stream = client.OpenRead(u.GetAvatarUrl(ImageFormat.Png) ?? u.GetDefaultAvatarUrl());
                        using MagickImage icon = new MagickImage(stream);
                        icon.Resize(48, 48);
                        icon.BackgroundColor = MagickColors.None;

                        icon.Distort(DistortMethod.DePolar, 0);
                        icon.VirtualPixelMethod = VirtualPixelMethod.HorizontalTile;
                        icon.BackgroundColor = MagickColors.None;
                        icon.Distort(DistortMethod.Polar, 0);

                        background.Composite(icon, Gravity.Center, 294 - 64 - 24 + (count * 50), -128 - 24, CompositeOperator.Atop);

                        count++;
                    }
                }

                new Drawables()
                  .FontPointSize(100)
                  .Font("Impact")
                  .StrokeColor(new MagickColor("Black"))
                  .FillColor(new MagickColor("White"))
                  .TextAlignment(TextAlignment.Center)
                  .Text(218, 475, one.Tag.ToUpper())
                  .Gravity(Gravity.Center)
                  .Draw(background);

                new Drawables()
                  .FontPointSize(100)
                  .Font("Impact")
                  .StrokeColor(new MagickColor("Black"))
                  .FillColor(new MagickColor("White"))
                  .TextAlignment(TextAlignment.Center)
                  .Text(806, 475, two.Tag.ToUpper())
                  .Gravity(Gravity.Center)
                  .Draw(background);

                return background;
            }

            [Command("teaminfo")]
            [Summary("get team info")]
            public async Task TeamInfo(string id = null)
            {
                if (Context.Channel.Id != Bot.Instance.Admin.Id) { return; }

                WCTeam team = Data.GetTeam(id);
                if (team == null)
                {
                    await ReplyAsync($"Team {id} not found!");
                    return;
                }

                string path = Data.DefaultIcon;
                if (team.CustomIcon)
                {
                    path = Path.Combine(Data.IconFolder, team.TeamID.ToString() + ".png");
                }

                var i = new Image(path);
                var stream = i.Stream;
                var image = await Context.Channel.SendFileAsync(stream, "icon.png");
                stream.Close();
                image.DeleteAsync();
                i.Dispose();

                EmbedBuilder builder = new EmbedBuilder();
                EmbedAuthorBuilder author = new EmbedAuthorBuilder();
                author.Name = team.Name;

                builder.Author = author;
                builder.Color = Color.Red;

                string url = null;
                foreach (var e in image.Attachments)
                {
                    url = e.Url;
                    break;
                }
                if (url != null)
                {
                    author.IconUrl = url;
                }

                EmbedFieldBuilder field = new EmbedFieldBuilder();
                field.Name = "Team info:";
                field.Value = @$"Tag: {team.Tag}
ID: {team.TeamID}
sID: {team.GetsID()}
Leader: {Utils.GetUsername(team.Leader)}
Active: {team.IsActive}
Created at: {team.Creation.ToString("MM\\/dd\\/yyyy HH:mm")}
{team.GetPlayers().Count} member(s)
{team.Subs?.Count ?? 0} sub(s)
{team.Ships.Count} registered ship(s)";

                builder.Fields.Add(field);

                foreach (var b in team.Ships)
                {
                    EmbedFieldBuilder ship = new EmbedFieldBuilder();
                    ship.Name = b.Title;
                    ship.Value = b.ToString();
                    builder.Fields.Add(ship);
                }

                await ReplyAsync(null, false, builder.Build());
            }

            [Command("teammsg")]
            [Summary("msg select teams")]
            public async Task TeamMsg(string msg = null, params string[] teamIDs)
            {
                if (Context.Channel.Id != Bot.Instance.Admin.Id)
                {
                    return;
                }
                if (msg == null)
                {
                    await ReplyAsync("Invalid message!");
                    return;
                }

                List<WCTeam> teams = new List<WCTeam>();

                if (teamIDs.Length == 1 && teamIDs[0].ToLower() == "everyone")
                {
                    foreach (var t in Data.RegisteredTeams)
                        teams.Add(t);
                }
                else if (teamIDs.Length == 1 && teamIDs[0].ToLower() == "active")
                {
                    foreach (var t in Data.RegisteredTeams)
                        if (t.IsActive)
                            teams.Add(t);
                }
                else
                {
                    foreach (string s in teamIDs)
                    {
                        var t = Data.GetTeam(s);
                        if (t != null)
                        {
                            teams.Add(t);
                        }
                        else
                        {
                            ReplyAsync("Unknown team " + s);
                        }
                    }
                }

                foreach (var team in teams)
                {
                    Utils.Log("Sending message to " + team.Tag);
                    foreach(var player in team.GetWCPlayers())
                    {
                        if (player != null)
                        {
                            EmbedBuilder builder = new EmbedBuilder();
                            builder.Color = Color.Red;

                            EmbedFieldBuilder field = new EmbedFieldBuilder();
                            field.Name = "StarCore Admin team";
                            field.Value = Utils.Format(msg, player, team);
                            builder.Fields.Add(field);

                            //Utils.Log(player.Discord());
                            player.Discord()?.GetOrCreateDMChannelAsync()?.Result?.SendMessageAsync(null, false, builder.Build());
                        }
                    }
                }
                await Context.Message.AddReactionAsync(new Emoji("\u2705"));
            }

            [Command("teams")]
            [Summary("Admin teams")]
            public async Task ListTeams()
            {
                if (Context.Channel.Id != Bot.Instance.Admin.Id) { return; }

                EmbedBuilder builder = new EmbedBuilder();
                EmbedAuthorBuilder author = new EmbedAuthorBuilder();
                author.Name = "All teams";
                builder.Author = author;
                builder.Color = Color.Red;

                foreach(WCTeam t in Data.RegisteredTeams)
                {
                    EmbedFieldBuilder field = new EmbedFieldBuilder();
                    field.Name = $"{t.Name} ({t.Tag})";
                    field.Value = $"sID: {t.GetsID()}\n{t.GetPlayers().Count} players";
                    builder.Fields.Add(field);
                }
                
                await ReplyAsync(null, false, builder.Build());
            }

            [Command("SetAllTeamsInactive")]
            [Summary("sets all teams to inactive")]
            public async Task ResetAllActiveTeams()
            {
                if (Context.Channel.Id != Bot.Instance.Admin.Id) { return; }
                
                foreach (WCTeam t in Data.RegisteredTeams)
                {
                    t.IsActive = false;
                    t.Save();
                }

                await ReplyAsync("All teams set to inActive");
            }

            [NamedArgumentType]
            public class AdminSetArgs
            {
                public int? PlayerBPLimit { get; set; }
                public int? TeamPlayerLimit { get; set; }
                public int? TeamBPLimit { get; set; }
                public int? TeamSubLimit { get; set; }
                public bool? AllowTeamCreation { get; set; }
                public bool? AllowBlueprintModification { get; set; }
                public bool? AllowTeamModification { get; set; }
                public bool? AllowTeamReady { get; set; }
                public int? TeamReadyLimit { get; set; }

            }

            [Command("setconfig")]
            [Summary("Admin configs")]
            public async Task AdminSetConfig(AdminSetArgs args = null)
            {
                if (Context.Channel.Id != Bot.Instance.Admin.Id) { return; }

                if (args != null)
                {
                    if (args.TeamPlayerLimit != null)
                    {
                        Config.Instance.TeamPlayerLimit = args.TeamPlayerLimit.Value;
                    }
                    if (args.PlayerBPLimit != null)
                    {
                        Config.Instance.PlayerBPLimit = args.PlayerBPLimit.Value;
                    }
                    if (args.TeamBPLimit != null)
                    {
                        Config.Instance.TeamBPLimit = args.TeamBPLimit.Value;
                    }
                    if (args.AllowTeamCreation != null)
                    {
                        Config.Instance.AllowTeamCreation = args.AllowTeamCreation.Value;
                    }
                    if (args.AllowBlueprintModification != null)
                    {
                        Config.Instance.AllowBlueprintModification = args.AllowBlueprintModification.Value;
                    }
                    if (args.AllowTeamModification != null)
                    {
                        Config.Instance.AllowTeamModification = args.AllowTeamModification.Value;
                    }
                    if (args.TeamSubLimit != null)
                    {
                        Config.Instance.TeamSubLimit = args.TeamSubLimit.Value;
                    }
                    if (args.AllowTeamReady != null)
                    {
                        Config.Instance.AllowTeamReady = args.AllowTeamReady.Value;
                    }
                    if (args.TeamReadyLimit != null)
                    {
                        Config.Instance.TeamReadyLimit = args.TeamReadyLimit.Value;
                    }
                }
                
                Config.Instance.Save();
                await ReplyAsync($@"**Config values:**
PlayerBPLimit: {Config.Instance.PlayerBPLimit}
TeamPlayerLimit: {Config.Instance.TeamPlayerLimit}
TeamSubLimit: {Config.Instance.TeamSubLimit}
TeamBPLimit: {Config.Instance.TeamBPLimit}
AllowTeamCreation: {Config.Instance.AllowTeamCreation}
AllowTeamModification: {Config.Instance.AllowTeamModification}
AllowBlueprintModification: {Config.Instance.AllowBlueprintModification}
AllowTeamReady: { Config.Instance.AllowTeamReady}
TeamReadyLimit: { Config.Instance.TeamReadyLimit}
                ");
            }

        }
    }
}
#endif