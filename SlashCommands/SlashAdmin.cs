using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarcoreDiscordBot.SlashCommands
{
    class SlashAdmin
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Nonsense", "HAA0101")]
        public static void Init()
        {
            var registerCmd = new SlashCommandBuilder()
                .WithName("admin")
                .WithDefaultPermission(false)
                .WithDescription("Admin commands")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("user-info")
                    .WithDescription("Get player information")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("user", ApplicationCommandOptionType.User, "User"))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("team-info")
                    .WithDescription("Get team information")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("team", ApplicationCommandOptionType.String, "Team"))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("edit-team")
                    .WithDescription("Edit a team")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("team", ApplicationCommandOptionType.String, "Team")
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("edit")
                        .WithDescription("Edit mode")
                        .WithRequired(true)
                        .WithType(ApplicationCommandOptionType.Integer)
                        .AddChoice("TeamLeader", 0)
                        .AddChoice("TeamName", 1)
                        .AddChoice("TeamTag", 2))
                    .AddOption("value", ApplicationCommandOptionType.String, "Value"))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("team-msg")
                    .WithDescription("Message select teams")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("teams", ApplicationCommandOptionType.String, "Teams to message")
                    .AddOption("message", ApplicationCommandOptionType.String, "Message to send"))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("create-tournament")
                    .WithDescription("Create a tournament")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("name", ApplicationCommandOptionType.String, "Name")
                    .AddOption("abreviation", ApplicationCommandOptionType.String, "Tag")
                    .AddOption("general-channel", ApplicationCommandOptionType.Channel, "General channel")
                    .AddOption("admin-channel", ApplicationCommandOptionType.Channel, "Channel for controlling it"))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("delete-tournament")
                    .WithDescription("Delete a tournament")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("identity", ApplicationCommandOptionType.Integer, "Identity"))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("edit-config")
                    .WithDescription("Edit global config")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("edit")
                        .WithDescription("Edit mode")
                        .WithRequired(true)
                        .WithType(ApplicationCommandOptionType.Integer)
                        .AddChoice("PlayerBPLimit", 0)
                        .AddChoice("TeamSubLimit", 1))
                    .AddOption("value", ApplicationCommandOptionType.Integer, "Value"));

            //admin only
            var perms = new ApplicationCommandPermission(Config.Instance.AdminRole, ApplicationCommandPermissionTarget.Role, true);

            CommandManager.RegisterSlashCommand(registerCmd, Callback, perms);
        }

        private static async Task Callback(SocketSlashCommand arg)
        {
            if (Bot.Instance.MainServer.GetRole(855481604948361246).Position > Bot.Instance.MainServer.GetUser(arg.User.Id).Hierarchy)
            {
                await arg.RespondAsync("You do not have permission to preform this command.", ephemeral: true);
                return;
            }

            var first = arg.Data.Options.First();

            WCTeam team;
            string strTeam;
            switch (first.Name)
            {
                case "user-info":
                    SocketUser info = first.Options.First().Value as SocketUser;
                    string userInfo = "Not registered";
                    string ships = "";
                    WCPlayer p = Data.GetPlayer(info.Id);
                    if (p != null)
                    {
                        userInfo = $"SteamID: {p.SteamID}\nWins/Losses {p.Wins}/{p.Losses}\n{p.GetRegistedBlueprints().Count} registered ships\n";
                        WCTeam playerTeam = p.GetTeam();
                        if (playerTeam != null)
                            userInfo += $"Team: {playerTeam.Name} ({playerTeam.Tag})\n";
                        else
                            userInfo += "Team: none\n";
                        WCTeam sub = Data.GetSubTeam(p);
                        if (sub != null)
                            userInfo += $"Subbed: {sub.Name}";
                        else
                            userInfo += "Subbed: none";

                        foreach (var m in p.GetRegistedBlueprints())
                            ships += $"- {m}\n";
                    }
                    await arg.RespondAsync(embed: new EmbedBuilder()
                        .WithAuthor(new EmbedAuthorBuilder().WithIconUrl(info.GetAvatarUrl()).WithName(info.Username))
                        .WithTitle("Information")
                        .AddField(new EmbedFieldBuilder().WithName("Info").WithValue(userInfo))
                        .AddField(new EmbedFieldBuilder().WithName("Ships").WithValue(string.IsNullOrEmpty(ships) ? "None" : ships))
                        .Build(), ephemeral: true);
                    break;
                case "team-info":
                    strTeam = first.Options.First().Value as string;
                    team = Data.GetTeam(strTeam);
                    if (team != null)
                    {
                        string members = "";
                        foreach (var m in team.GetPlayers())
                            members += $"<@{m}>\n";

                        string subs = "";
                        foreach (var m in team.Subs)
                            subs += $"<@{m}>\n";

                        await arg.RespondAsync(embed: new EmbedBuilder()
                        .WithTitle($"{team.Name} ({team.Tag})")
                        .WithColor(team.GetTeamColor().ToDiscordColor())
                        .AddField(new EmbedFieldBuilder().WithName("Info")
                            .WithValue($"Created: <t:{(int)(team.Creation - new DateTime(1970, 1, 1)).TotalSeconds}:R>" +
                            $"\nLeader: {Utils.GetUsername(team.Leader)}" +
                            $"\nWins/Losses: {team.GlobalWins}/{team.GlobalLosses}" +
                            $"\nRoleID: {team.RoleID}" +
                            $"\nTeamID: {team.TeamID}" +
                            $"\nCustom Icon: {team.CustomIcon}"))
                        .AddField(new EmbedFieldBuilder().WithName("Members").WithValue(members))
                        .AddField(new EmbedFieldBuilder().WithName("Subs").WithValue(string.IsNullOrEmpty(subs) ? "None" : subs))
                        .Build(), ephemeral: true);
                    }
                    else
                        await arg.RespondAsync($"Unknown team '{strTeam}'", ephemeral: true);
                    break;
                case "edit-team":
                    strTeam = first.Options.First().Value as string;
                    team = Data.GetTeam(strTeam);
                    if (team != null)
                    {
                        string value = first.Options.Get(2).Value as string;
                        switch ((int)(long)first.Options.Get(1).Value)
                        {
                            case 0:
                                team.Leader = ulong.Parse(value);
                                break;
                            case 1:
                                team.Name = value;
                                break;
                            case 2:
                                team.Tag = value;
                                break;
                        }
                        team.Save();
                        await arg.RespondAsync($"Changed values", ephemeral: true);
                    }
                    else
                        await arg.RespondAsync($"Unknown team '{strTeam}'", ephemeral: true);
                    break;
                case "team-msg":
                    List<WCTeam> teams = new List<WCTeam>();
                    string teamIDs = (string)first.Options.First().Value;
                    string errorTeams = "";

                    if (teamIDs.ToLower() == "everyone")
                        foreach (var t in Data.RegisteredTeams)
                            teams.Add(t);
                    else
                    {
                        string[] teamIDArr = teamIDs.Split(' ');
                        foreach (string s in teamIDArr)
                        {
                            var t = Data.GetTeam(s);
                            if (t != null)
                                teams.Add(t);
                            else
                                errorTeams += $"\n- {s}";
                        }
                    }

                    string msg = (string)first.Options.Get(1).Value;
                    foreach (var t in teams)
                    {
                        foreach (var player in t.GetWCPlayers())
                        {
                            if (player != null)
                            {
                                EmbedBuilder builder = new EmbedBuilder();
                                builder.Color = Color.Red;

                                EmbedFieldBuilder field = new EmbedFieldBuilder();
                                field.Name = "StarCore Admin team";
                                field.Value = Utils.Format(msg, player, t);
                                builder.Fields.Add(field);
                                player.Discord()?.CreateDMChannelAsync()?.Result?.SendMessageAsync(null, false, builder.Build());
                            }
                        }
                    }
                    await arg.RespondAsync($"Sent message to {teams.Count} teams! {(string.IsNullOrEmpty(errorTeams) ? "" : $"\nCould not find teams: {errorTeams}")}", ephemeral: true);
                    break;
                case "edit-config":
                    int newVal = (int)(long)first.Options.Get(1).Value;
                    int oldVal = 0;
                    switch ((int)(long)first.Options.Get(0).Value)
                    {
                        case 0:
                            oldVal = Config.Instance.PlayerBPLimit;
                            Config.Instance.PlayerBPLimit = newVal;
                            break;
                        case 1:
                            oldVal = Config.Instance.TeamSubLimit;
                            Config.Instance.TeamSubLimit = newVal;
                            break;
                    }
                    Config.Instance.Save();
                    await arg.RespondAsync($"Changed value from {oldVal} -> {newVal}", ephemeral: true);
                    break;
                case "create-tournament":
                    string name = (string)first.Options.Get(0).Value;
                    string abv = (string)first.Options.Get(1).Value;

                    var general = (SocketTextChannel)first.Options.Get(2).Value;
                    var admin = (SocketTextChannel)first.Options.Get(3).Value;

                    var tourn = new WCTournament()
                    {
                        Name = name,
                        Abv = abv,
                        AdminChannel = admin.Id,
                        GeneralChannel = general.Id,
                        TornID = (int)(int.MaxValue * Utils.Rng.NextDouble()),
                        Locked = true,
                        LargeGridAllowed = true,
                        MaxShipBattlePoints = -1,
                        MaxShipBlocks = -1,
                        MaxTeams = -1,
                        MaxTeamShips = -1,
                    };
                    tourn.UpdateTournament();
                    tourn.Save();

                    SlashTournament.Init();

                    await arg.RespondAsync($"Created Tournament '{name}' ({abv})!");
                    await general.SendMessageAsync($"Tournament '{name}' has registed this as the general channel.");
                    await admin.SendMessageAsync($"Tournament '{name}' has registed this as the admin channel.");
                    break;
                case "delete-tournament":
                    var number = (int)(long)first.Options.Get(0).Value;

                    tourn = Data.GetTournament(number);
                    if (tourn != null)
                    {
                        tourn.Delete();
                        await arg.RespondAsync($"Deleted Tournament '{tourn.Name}'!");
                        SlashTournament.Init();
                    } 
                    else
                    {
                        await arg.RespondAsync($"Unknown tournament id {tourn}!");
                    }
                    break;
            }
        }


    }
}
