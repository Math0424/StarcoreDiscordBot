using Discord;
using Discord.WebSocket;
using ImageMagick;
using StarcoreDiscordBot.Networking;
using StarcoreDiscordBot.Networking.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StarcoreDiscordBot.SlashCommands
{
    class SlashTournamentAdmin
    {

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "HAA0101:Array allocation for params parameter")]
        public static void Init()
        {
            Bot.Instance.Client.MessageReceived += ReadNextMessageIn;

            var cmd1 = new SlashCommandBuilder()
                .WithName("tournament-manager")
                .WithDescription("Tournament management commands")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("info")
                    .WithDescription("Information about tournament")
                    .WithType(ApplicationCommandOptionType.SubCommand))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("set-data-sheet")
                    .WithDescription("set the point sheet")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("link", ApplicationCommandOptionType.String, "link to datasheet"))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("remove")
                    .WithDescription("remove a team")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("team", ApplicationCommandOptionType.String, "team to remove"))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("export-data")
                    .WithDescription("Get a data sheet with selected options")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("random", ApplicationCommandOptionType.Boolean, "Random team selection", false)
                    .AddOption("count", ApplicationCommandOptionType.Integer, "Number of teams to select", false)
                    .AddOption("full-data", ApplicationCommandOptionType.Boolean, "Full data sheet", false))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("set-restrictions")
                    .WithDescription("Set the restrictions for entry")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("locked", ApplicationCommandOptionType.Boolean, "If people can join", false)
                    .AddOption("allow-small-grid", ApplicationCommandOptionType.Boolean, "Can people submit small grid", false)
                    .AddOption("allow-large-grid", ApplicationCommandOptionType.Boolean, "Can people submit large grid", false)

                    .AddOption("min-ship-battle-points", ApplicationCommandOptionType.Integer, "Min battle points per submittion", false)
                    .AddOption("min-ship-blocks", ApplicationCommandOptionType.Integer, "Min block count of a ship", false)
                    .AddOption("max-ship-battle-points", ApplicationCommandOptionType.Integer, "Max battle points per submittion", false)
                    .AddOption("max-ship-blocks", ApplicationCommandOptionType.Integer, "Max block count of a ship", false)
                    
                    .AddOption("max-team-battle-points", ApplicationCommandOptionType.Integer, "Max battle points per submittion", false)
                    .AddOption("max-team-blocks", ApplicationCommandOptionType.Integer, "Max block count of a ship", false)

                    .AddOption("max-teams", ApplicationCommandOptionType.Integer, "Max teams to enter", false)
                    .AddOption("max-team-ships", ApplicationCommandOptionType.Integer, "Max amount of ships per team", false))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("set-custom-icon")
                    .WithDescription("Set a custom icon for tournament")
                    .WithType(ApplicationCommandOptionType.SubCommand))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("set-server-id")
                    .WithDescription("Set the server ID")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("server-id", ApplicationCommandOptionType.Integer, "Server-id"));

            var cmd2 = new SlashCommandBuilder()
                .WithName("tournament-server")
                .WithDescription("Tournament management commands")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("status")
                    .WithDescription("Information about tournament")
                    .WithType(ApplicationCommandOptionType.SubCommand))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("fight")
                    .WithDescription("Fight 2 teams on a server")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("team1", ApplicationCommandOptionType.String, "team1 to fight")
                    .AddOption("team2", ApplicationCommandOptionType.String, "team2 to fight")
                    .AddOption("announce", ApplicationCommandOptionType.Boolean, "If to announce the fight")
                    .AddOption("ip-addr", ApplicationCommandOptionType.String, "IP adress of server"))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("ffa")
                    .WithDescription("free for all of select teams")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("teams", ApplicationCommandOptionType.String, "Teams to fight"))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("paste-ship")
                    .WithDescription("Ship paste on server")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("blueprint-id", ApplicationCommandOptionType.Integer, "Id of the blueprint")
                    .AddOption("pos-x", ApplicationCommandOptionType.Integer, "pos-X")
                    .AddOption("pos-y", ApplicationCommandOptionType.Integer, "pos-Y")
                    .AddOption("pos-z", ApplicationCommandOptionType.Integer, "pos-Z"))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("lineup")
                    .WithDescription("Create ship lineup on server")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("teams", ApplicationCommandOptionType.String, "Teams to lineup"))
                 .AddOption(new SlashCommandOptionBuilder()
                    .WithName("add-whitelist")
                    .WithDescription("Add member to server whitelist")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("steam-id", ApplicationCommandOptionType.String, "steam-id"));


            var adminPerm = new ApplicationCommandPermission(Config.Instance.AdminRole, ApplicationCommandPermissionTarget.Role, true);
            var managerPerm = new ApplicationCommandPermission(Config.Instance.ManagerRole, ApplicationCommandPermissionTarget.Role, true);

            CommandManager.RegisterSlashCommand(cmd2, Callback, adminPerm, managerPerm);
            CommandManager.RegisterSlashCommand(cmd1, Callback, adminPerm, managerPerm);
        }

        private static async Task Callback(SocketSlashCommand arg)
        {
            var firstName = arg.Data.Options.First();

            WCTournament context = null;
            WCBlueprint bp = null;
            foreach(var t in Data.RegisteredTournaments)
            {
                if (t.AdminChannel == arg.Channel.Id)
                {
                    context = t;
                }
            }
            if (context == null)
            {
                await arg.RespondAsync("This command must be typed in a Tournamnet admin channel", ephemeral: true);
                return;
            }

            switch (firstName.Name)
            {
                //TOURNAMENT
                case "info":
                    StringBuilder desc = new StringBuilder();
                    desc.AppendLine($"ID: {context.TornID}");
                    desc.AppendLine($"Role ID: {context.RoleID}");
                    desc.AppendLine($"Server ID: {context.ServerID}");
                    desc.AppendLine($"Custom Icon: {context.CustomIcon}");
                    desc.AppendLine($"Is full: {context.IsFull}");
                    desc.AppendLine($"Is Locked: {context.Locked}");
                    desc.AppendLine($"Ship BP; min/max {context.MinShipBattlePoints}/{context.MaxShipBattlePoints}");
                    desc.AppendLine($"Ship Blocks; min/max {context.MinShipBlocks}/{context.MaxShipBlocks}");
                    desc.AppendLine($"Max team BP: {context.MaxTeamBattlePoints}");
                    desc.AppendLine($"Max team Blocks: {context.MaxTeamBlocks}");
                    desc.AppendLine($"Max Teams: {context.MaxTeams}");
                    desc.AppendLine($"Max team ships: {context.MaxTeamShips}");
                    desc.AppendLine($"Small grid allowed: {context.SmallGridAllowed}");
                    desc.AppendLine($"Large grid allowed: {context.LargeGridAllowed}");
                    desc.AppendLine($"General channel: <#{context.GeneralChannel}>");

                    EmbedBuilder builder = new EmbedBuilder()
                        .WithTitle($"{context.Name} ({context.Abv})")
                        .WithDescription(desc.ToString());
                    await arg.RespondAsync(embed: builder.Build());
                    break;
                case "set-data-sheet":
                    string dataSheet = (string)firstName.Options.Get(0).Value;
                    context.SetReaderPath(dataSheet);
                    await arg.RespondAsync($"Set data sheet to '{dataSheet.Substring(0, (dataSheet.Length > 20 ? 20 : dataSheet.Length))}'");
                    break;
                case "remove":
                    string teamToRemove = (string)firstName.Options.Get(0).Value;
                    var removeTeam = Data.GetTeam(teamToRemove);
                    if (removeTeam != null)
                    {
                        if (context.RemoveEntry(removeTeam))
                        {
                            removeTeam.MessagePlayers($"You have been removed from {context.Name} by tournament admins");
                            await arg.RespondAsync($"Removed Team {teamToRemove} from this tournament");
                        }
                        else
                            await arg.RespondAsync($"Team {teamToRemove} is not in this tournament");
                    }
                    else
                        await arg.RespondAsync($"Cannot find team {teamToRemove}");
                    break;
                case "export-data":
                    bool random = false, fullData = false;
                    int count = -1;
                    if (firstName?.Options != null)
                    {
                        foreach (var option in firstName.Options)
                        {
                            switch (option.Name)
                            {
                                case "random":
                                    random = (bool)option.Value;
                                    break;
                                case "count":
                                    count = (int)(long)option.Value;
                                    break;
                                case "full-data":
                                    fullData = (bool)option.Value;
                                    break;
                            }
                        }
                    }
                    List<TournamentEntry> entries = context.GetEntrys();
                    if(random)
                        entries.OrderBy(x => Utils.Rng.Next()).ToArray();
                    if (count != -1)
                    {
                        int remove = entries.Count - count;
                        entries.RemoveRange(0, Math.Min(0, remove));
                    }

                    await arg.RespondAsync($"Preparing {entries.Count} out of {context.GetEntrys().Count} teams...");

                    using (MemoryStream stream = new MemoryStream())
                    {
                        using StreamWriter writer = new StreamWriter(stream);
                        Dictionary<string, int> Blocks = new Dictionary<string, int>();
                        int GlobalBlockCount = 0, GlobalBP = 0;

                        foreach (var x in entries)
                        {
                            WCTeam t = x.GetTeam();
                            writer.WriteLine("---------------------------------");
                            writer.WriteLine($"Team: {t.Name} ({t.Tag}) : {t.TeamID} : <@{t.RoleID}>");
                            writer.WriteLine();
                            writer.WriteLine($"Members:");
                            foreach (var m in t.GetPlayers())
                                writer.WriteLine($"-   <@{m}>  ({Utils.GetUsername(m)})");
                            writer.WriteLine();
                            writer.WriteLine($"Subs:");
                            foreach (var m in t.Subs)
                                writer.WriteLine($"-   <@{m}>  ({Utils.GetUsername(m)})");
                            writer.WriteLine();
                            writer.WriteLine($"Ships:");
                            foreach (var m in x.GetShips())
                                writer.WriteLine($"-   {m}");
                            writer.WriteLine();

                            if (fullData)
                            {
                                foreach (var s in x.GetShips())
                                {
                                    bp = Data.GetBlueprint(s);
                                    var info = context.BlueprintData(ref bp);
                                    GlobalBlockCount += info.BlockCount;
                                    GlobalBP += info.BattlePoints;
                                    writer.WriteLine($"'{bp.Title}' : {info.BattlePoints} BattlePoints - {info.BlockCount} Blocks - {info.SubgridCount} Subgrids");
                                    writer.WriteLine($"ID: {bp.GID} | {(info.HasLargeGrid ? "HasLargeGrid" : "NoLargeGrid")} : {(info.HasSmallGrid ? "HasSmallGrid" : "NoSmallGrid")}");
                                    writer.WriteLine("Data:");
                                    foreach(var b in info.Blocks.OrderBy(key => -key.Value))
                                    {
                                        writer.WriteLine($"-   {b.Key} x{b.Value}");
                                    }
                                    foreach (var b in info.Blocks)
                                    {
                                        if (!Blocks.ContainsKey(b.Key))
                                            Blocks.Add(b.Key, 0);
                                        Blocks[b.Key] += b.Value;
                                    }
                                    writer.WriteLine();
                                }
                            }
                        }
                        if (fullData)
                        {
                            writer.WriteLine("---------------------------------");
                            if (Blocks.Count != 0)
                            {
                                writer.WriteLine("GLOBAL DATA REPORT:");
                                writer.WriteLine("GlobalBlockCount: " + GlobalBlockCount);
                                writer.WriteLine("GlobalBP: " + GlobalBP);
                                foreach (var b in Blocks.OrderBy(key => -key.Value))
                                {
                                    writer.WriteLine($"-   {b.Key} x{b.Value}");
                                }
                            }
                        }
                        
                        writer.Flush();
                        stream.Position = 0;
                        await arg.Channel.SendFileAsync(stream, "teamData.txt");
                    }
            break;
                case "set-restrictions":
                    foreach (var option in firstName.Options)
                    {
                        switch(option.Name)
                        {
                            case "locked":
                                context.Locked = (bool)option.Value;
                                break;
                            case "allow-small-grid":
                                context.SmallGridAllowed = (bool)option.Value;
                                break;
                            case "allow-large-grid":
                                context.LargeGridAllowed = (bool)option.Value;
                                break;
                            case "min-ship-battle-points":
                                context.MinShipBattlePoints = (int)(long)option.Value;
                                break;
                            case "min-ship-blocks":
                                context.MinShipBlocks = (int)(long)option.Value;
                                break;
                            case "max-ship-battle-points":
                                context.MaxShipBattlePoints = (int)(long)option.Value;
                                break;
                            case "max-ship-blocks":
                                context.MaxShipBlocks = (int)(long)option.Value;
                                break;
                            case "max-team-battle-points":
                                context.MaxTeamBattlePoints = (int)(long)option.Value;
                                break;
                            case "max-team-blocks":
                                context.MaxTeamBlocks = (int)(long)option.Value;
                                break;
                            case "max-teams":
                                context.MaxTeams = (int)(long)option.Value;
                                break;
                            case "max-team-ships":
                                context.MaxTeamShips = (int)(long)option.Value;
                                break;
                        }
                    }
                    await arg.RespondAsync("Changed tournament values");
                    context.Save();
                    break;
                case "set-custom-icon":
                    await arg.RespondAsync("Upload an image in this channel as your next message to set the tournament icon!");
                    setIconDict.Remove(arg.User.Id);
                    setIconDict.Add(arg.User.Id, arg.Channel.Id);
                    break;
                case "set-server-id":
                    context.ServerID = (byte)(long)firstName.Options.Get(0);
                    context.Save();
                    await arg.RespondAsync($"Changed server ID to {context.ServerID}");
                    break;

                //SERVER
                case "status":
                    if (Pipelines.SendData(new PacketPing(), context.ServerID))
                    {
                        await arg.RespondAsync("Server is online!");
                    }
                    else
                        await arg.RespondAsync("Server is offline!");
                    break;
                case "add-whitelist":
                    if (ulong.TryParse((string)firstName.Options.Get(0).Value, out ulong user))
                    {
                        if (Pipelines.SendData(new PacketAddWhitelist(user), context.ServerID))
                        {
                            await arg.RespondAsync($"Adding '{user}' to server whitelist");
                        }
                        else
                            await arg.RespondAsync("Server is offline!");
                    }
                    else
                        await arg.RespondAsync("Not a valid UserID!");
                    break;
                case "paste-ship":
                    int shipID = (int)(long)firstName.Options.Get(0).Value;
                    bp = Data.GetBlueprint(shipID);
                    if (bp != null)
                    {
                        int myX = (int)(long)firstName.Options.Get(1).Value;
                        int myY = (int)(long)firstName.Options.Get(2).Value;
                        int myZ = (int)(long)firstName.Options.Get(3).Value;
                        if (Pipelines.SendData(new PacketPasteShip(bp.GetSBCPath(), myX, myY, myZ), context.ServerID))
                        {
                            await arg.RespondAsync("Pasting ship in server...");
                        }
                        else
                            await arg.RespondAsync("Server is offline!");
                    }
                    else
                        await arg.RespondAsync("Blueprint not found!...");
                    break;
                case "lineup":
                    string value = (string)firstName.Options.Get(0).Value;
                    string cannotFind = string.Empty;
                    List<TournamentEntry> lineups = new List<TournamentEntry>();
                    int lineupShips = 0;

                    if (value.ToLower() != "everyone")
                    {
                        string[] teams = value.Split(" ");
                        foreach (var s in teams)
                        {
                            var team = Data.GetTeam(s);
                            if (team != null && context.GetEntry(team, out TournamentEntry entry))
                            {
                                if (entry.IsValid())
                                {
                                    lineupShips += entry.GetShips().Count;
                                    lineups.Add(entry);
                                }
                            }
                            else
                            {
                                cannotFind += " " + s;
                            }
                        }
                    }
                    else
                    {
                        foreach(var e in context.GetEntrys()) 
                        {
                            if (e.IsValid())
                            {
                                lineupShips += e.GetShips().Count;
                                lineups.Add(e);
                            }
                        }
                    }

                    if (lineups.Count == 0 || lineupShips == 0)
                    {
                        await arg.RespondAsync("No teams or ships to send to server!");
                        return;
                    }

                    if (Pipelines.SendData(new PacketLineupShips(lineups.ToArray()), context.ServerID))
                    {
                        if (string.IsNullOrEmpty(cannotFind))
                        {
                            await arg.RespondAsync($"Sent {lineups.Count} teams and {lineupShips} ships to the server!");
                        }
                        else
                        {
                            await arg.RespondAsync($"Sent {lineups.Count} teams and {lineupShips} ships to the server!\nBut could not find teams{cannotFind}");
                        }
                    }
                    else
                        await arg.RespondAsync("Server is offline!");
                    break;
                case "ffa":
                    await arg.RespondAsync("Not implemented yet");
                    break;
                case "fight":

                    var team1 = Data.GetTeam((string)firstName.Options.Get(0).Value);
                    var team2 = Data.GetTeam((string)firstName.Options.Get(1).Value);

                    if (team1 != null && team2 != null)
                    {
                        if (context.GetEntry(team1, out TournamentEntry entry1) && context.GetEntry(team2, out TournamentEntry entry2))
                        {
                            var blueSpawn = new MyTuple<int, int, int>(-670, -210, -9700);
                            var redSpawn = new MyTuple<int, int, int>(668, 248, 9600);

                            if (Pipelines.SendData(new PacketFight(entry1, entry2, blueSpawn, redSpawn)))
                            {
                                if ((bool)firstName.Options.Get(2).Value)
                                {
                                    SendFightAnnouncementMessages(context, (string)firstName.Options.Get(3).Value, team1, team2, entry1, entry2);
                                }
                                
                                await arg.RespondAsync($"Sent {team1.Name} to fight {team2.Name} with {entry1.GetShips().Count + entry2.GetShips().Count} ships to the server!");
                            }
                            else
                                await arg.RespondAsync("Server is offline!");
                        }
                        else
                            await arg.RespondAsync("One of the teams are not in this tournament!");
                    }
                    else
                        await arg.RespondAsync("Cannot find team(s)!");
                    break;
            }
        }

        private static async void SendFightAnnouncementMessages(WCTournament context, string IP, WCTeam team1, WCTeam team2, TournamentEntry entry1, TournamentEntry entry2)
        {
            var str = @$"
『{team1.Name}』vs『{team2.Name}』

【**You're about to fight!**】
➽ log onto the server below
➽ spawn into the game ({team1.Tag} on Blue & {team2.Tag} on Red)
➽ enter your ships
➽ await further instructions and do not begin fighting.

________________
steam://connect/{IP}
▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔▔";

            EmbedBuilder messageBuild = new EmbedBuilder();
            messageBuild.Color = Color.Red;

            EmbedFieldBuilder field = new EmbedFieldBuilder();
            field.Name = "StarCore Admin team";
            field.Value = str;
            messageBuild.Fields.Add(field);

            team1.MessagePlayers("", embed: messageBuild.Build());
            team2.MessagePlayers("", embed: messageBuild.Build());

            EmbedBuilder statBuilder = new EmbedBuilder();
            statBuilder.Color = Color.Red;

            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = "Statistics";
            statBuilder.Author = author;
            statBuilder.Color = Color.Red;

            EmbedFieldBuilder team1Field = new EmbedFieldBuilder();
            team1Field.Name = team1.Name;
            team1Field.Value = @$"{team1.GetPlayers().Count} players
{team1.Subs?.Count ?? 0} subs
{entry1.GetShips()} ships
{entry1.TeamBlocks} total blocks
{entry1.TeamBattlePoints} total battlePoints";
            statBuilder.Fields.Add(team1Field);

            EmbedFieldBuilder team2Field = new EmbedFieldBuilder();
            team2Field.Name = team2.Name;
            team2Field.Value = @$"{team2.GetPlayers().Count} players
{team2.Subs?.Count ?? 0} subs
{entry2.GetShips()} ships
{entry2.TeamBlocks} total blocks
{entry2.TeamBattlePoints} total battlePoints";
            statBuilder.Fields.Add(team2Field);

            using MagickImage sendImage = new MagickImage();
            sendImage.Resize(1024 * 2, 1024);
            using var team1icon = SlashTournament.CreateShowcase(context, team1, entry1);
            team1icon.Resize(1024, 1024);
            using var team2icon = SlashTournament.CreateShowcase(context, team2, entry2);
            team2icon.Resize(1024, 1024);
            sendImage.Composite(team1icon, Gravity.East, 0, 0, CompositeOperator.Over);
            sendImage.Composite(team2icon, Gravity.West, 0, 0, CompositeOperator.Over);

            using (Stream stream = new MemoryStream())
            {
                sendImage.Write(stream);
                stream.Position = 0;
                await context.GetGeneral().SendFileAsync(stream, "TeamMatch.png", "", embed: statBuilder.Build());
            }

        }


        private static Dictionary<ulong, ulong> setIconDict = new Dictionary<ulong, ulong>();
        public static async Task ReadNextMessageIn(SocketMessage e)
        {
            if (setIconDict.ContainsKey(e.Author.Id) && setIconDict[e.Author.Id] == e.Channel.Id)
            {
                setIconDict.Remove(e.Author.Id);
                WCTournament context = null;
                foreach (var t in Data.RegisteredTournaments)
                {
                    if (t.AdminChannel == e.Channel.Id)
                    {
                        context = t;
                    }
                }
                if (context == null)
                    return;

                if (e.Attachments.Count != 0)
                {
                    foreach (var x in e.Attachments)
                    {
                        string extension = Path.GetExtension(x.Filename.ToLower());
                        if (extension.Equals(".png") || extension.Equals(".jpeg") || extension.Equals(".jpg"))
                        {
                            using WebClient client = new WebClient();
                            using Stream stream = client.OpenRead(x.Url);
                            using MagickImage image = new MagickImage(stream);

                            string savePath = Path.Combine(Data.IconFolder, context.TornID + ".png");
                            File.Delete(savePath);

                            image.Write(savePath, MagickFormat.Png);

                            context.CustomIcon = true;
                            await e.Channel.SendMessageAsync("Set Tournament icon!");
                            context.Save();
                        }
                        else
                        {
                            await e.Channel.SendMessageAsync("Upload a image (*.png | *.jpg)!");
                            return;
                        }
                        break;
                    }
                }
                else
                {
                    await e.Channel.SendMessageAsync("This message does not contain a file!");
                }
            }
        }


    }
}
