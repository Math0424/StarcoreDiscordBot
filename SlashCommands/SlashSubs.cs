using Discord;
using Discord.WebSocket;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StarcoreDiscordBot.SlashCommands
{
    class SlashSubs
    {

        public static void Init()
        {
            var subCmd = new SlashCommandBuilder()
                .WithName("sub")
                .WithDescription("Sub management")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("invite")
                    .WithDescription("Invite a sub to your team")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("user", ApplicationCommandOptionType.User, "User to invite"))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("kick")
                    .WithDescription("Remove a sub from your team")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("user", ApplicationCommandOptionType.User, "User to kick"))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("leave")
                    .WithDescription("Leave your current sub team")
                    .WithType(ApplicationCommandOptionType.SubCommand))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("info")
                    .WithDescription("Info about your current sub status")
                    .WithType(ApplicationCommandOptionType.SubCommand));

            CommandManager.RegisterSlashCommand(subCmd, Callback);
        }

        private static async Task Callback(SocketSlashCommand arg)
        {
            if (!GetTeamAndPlayer(arg.User, out WCPlayer p, out WCTeam t))
            {
                if (p == null)
                {
                    await arg.RespondAsync("You have not registed!", ephemeral: true);
                    return;
                }
            }

            var first = arg.Data.Options.First();
            WCTeam subTeam = Data.GetSubTeam(p);

            switch (first.Name)
            {
                case "info":
                    if (subTeam != null)
                    {
                        await arg.RespondAsync(embed: new EmbedBuilder()
                            .WithTitle("Current sub status")
                            .WithFooter($"Team: {subTeam.Name} ({subTeam.Tag})\nCount: {subTeam.Subs.Count}")
                            .WithColor(subTeam.GetTeamColor().ToDiscordColor())
                            .Build(), ephemeral: true);
                    }
                    else
                        await arg.RespondAsync("You are not a sub for any team!", ephemeral: true);
                    break;
                case "leave":
                    if (subTeam != null)
                    {
                        subTeam.Subs.Remove(p.DiscordID);
                        await arg.RespondAsync("You have left the sub pool for " + subTeam.Name, ephemeral: true);
                        subTeam.MessagePlayers($"{p.UserName()} has left the teams sub pool team");
                    }
                    else
                        await arg.RespondAsync("You are not a sub for any team!", ephemeral: true);
                    break;
                case "kick":
                    if (t == null)
                    {
                        await arg.RespondAsync("You are not on a team!", ephemeral: true);
                        break;
                    }
                    if (t.Leader == arg.User.Id)
                    {
                        SocketUser kick = first.Options.First().Value as SocketUser;
                        if (t.Subs.Contains(kick.Id))
                        {
                            t.Subs.Remove(kick.Id);
                            t.MessagePlayers($"{kick.Username} has been removed from your teams sub pool");
                            await arg.RespondAsync($"Removed {kick.Username} from the sub pool", ephemeral: true);
                        }
                        else
                            await arg.RespondAsync(kick.Username + " is not in your sub pool!", ephemeral: true);
                    }
                    else
                        await arg.RespondAsync("You are not team leader!", ephemeral: true);
                    break;
                case "invite":
                    if (t == null)
                    {
                        await arg.RespondAsync("You are not on a team!", ephemeral: true);
                        break;
                    }
                    if (t.Leader == arg.User.Id)
                    {
                        SocketUser invite = first.Options.First().Value as SocketUser; 
                        if (!invite.IsBot && !t.GetPlayersAndSubs().Contains(invite.Id))
                        {
                            var c = await invite.CreateDMChannelAsync();

                            await arg.RespondAsync($"Inviting {invite.Username} to the team sub pool...", ephemeral: true);
                            var msg = await c.SendMessageAsync(embed: new EmbedBuilder()
                                .WithAuthor(arg.User.Username)
                                .WithColor(t.GetTeamColor().ToDiscordColor())
                                .WithTitle($"{t.Name} ({t.Tag})")
                                .WithFooter($"{arg.User.Username} wants you in their sub pool!")
                                .Build(), 
                                component: new ComponentBuilder()
                                .WithButton("Accept", "ac", ButtonStyle.Success)
                                .WithButton("Deny", "dn", ButtonStyle.Danger).Build());

                            CommandManager.RegisterButtonSelection(msg.Id, (x) => (BtnJoinTeamCallback(x, t)));
                        }
                        else
                            await arg.RespondAsync("Cannot invite somone already on your team!", ephemeral: true);
                    }
                    else
                        await arg.RespondAsync("You are not team leader!", ephemeral: true);
                    break;
            }
        }

        private static async Task<bool> BtnJoinTeamCallback(SocketMessageComponent inter, WCTeam team)
        {
            await inter.DeferLoadingAsync(true);
            await inter.Message.ModifyAsync((e) =>
            {
                e.Components = new ComponentBuilder()
                                .WithButton("Accept", "ac", ButtonStyle.Success, disabled: true)
                                .WithButton("Deny", "dn", ButtonStyle.Danger, disabled: true).Build();
            });
            switch (inter.Data.CustomId)
            {
                case "dn":
                    team.MessageLeader(inter.User.Username + " has denied to join your team sub pool.");
                    await inter.Channel.SendMessageAsync($"Denied sub invite for {team.Name}");
                    break;
                case "ac":
                    WCPlayer p = Data.GetPlayer(inter.User.Id);
                    if (p == null)
                    {
                        await inter.Channel.SendMessageAsync("You have not registed!");
                        return true;
                    }
                    WCTeam t = Data.GetPlayerTeam(p);
                    if (t != null)
                    {
                        await inter.Channel.SendMessageAsync("You are already on a team!");
                        return true;
                    }
                    WCTeam sub = Data.GetSubTeam(p);
                    if (t != null)
                    {
                        await inter.Channel.SendMessageAsync("You are already subbing for another team!");
                        return true;
                    }

                    team.MessagePlayers(inter.User.Username + " has joined your team!");
                    team.Subs.Add(inter.User.Id);
                    await inter.Channel.SendMessageAsync($"Accepted the sub invite for {team.Name}!");
                    break;
            }
            return true;
        }

        private static bool GetTeamAndPlayer(SocketUser arg, out WCPlayer player, out WCTeam team)
        {
            player = Data.GetPlayer(arg.Id);
            team = Data.GetPlayerTeam(player);
            return player != null && team != null;
        }

    }
}
