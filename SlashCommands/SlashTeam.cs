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
    class SlashTeam
    {

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Nonsense", "HAA0101")]
        public static void Init()
        {
            Bot.Instance.Client.MessageReceived += ReadNextMessageIn;

            var teamCmd = new SlashCommandBuilder()
                .WithName("team")
                .WithDescription("Team management")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("invite")
                    .WithDescription("Invite a player to your team")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("user", ApplicationCommandOptionType.User, "User to invite"))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("kick")
                    .WithDescription("Remove a player from your team")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("user", ApplicationCommandOptionType.User, "User to kick"))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("leave")
                    .WithDescription("Leave your current team")
                    .WithType(ApplicationCommandOptionType.SubCommand))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("create")
                    .WithDescription("Create a team")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("team-name", ApplicationCommandOptionType.String, "Team name")
                    .AddOption("team-abv", ApplicationCommandOptionType.String, "Team abbreviation (3 characters)"))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("set-color")
                    .WithDescription("Set the team color")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("red", ApplicationCommandOptionType.Integer, "Red (0-255)")
                    .AddOption("green", ApplicationCommandOptionType.Integer, "Green (0-255)")
                    .AddOption("blue", ApplicationCommandOptionType.Integer, "Blue (0-255)"))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("icon-2-lcd")
                    .WithDescription("Turn your team icon into a LCD image")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("mode")
                        .WithDescription("Quality of the output image")
                        .WithRequired(false)
                        .WithType(ApplicationCommandOptionType.Integer)
                        .AddChoice("3bit", 0)
                        .AddChoice("5bit", 1)))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("set-team-icon")
                    .WithDescription("Set your team icon (512x512)")
                    .WithType(ApplicationCommandOptionType.SubCommand))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("info")
                    .WithDescription("Get information on your team")
                    .WithType(ApplicationCommandOptionType.SubCommand));

            CommandManager.RegisterSlashCommand(teamCmd, Callback);
        }


        private static async Task Callback(SocketSlashCommand arg)
        {
            var firstName = arg.Data.Options.First();

            if (!GetTeamAndPlayer(arg.User, out WCPlayer p, out WCTeam t))
            {
                if (p == null)
                {
                    await arg.RespondAsync("You have not registed!", ephemeral: true);
                    return;
                }

                bool isCreatingTeam = firstName.Name.Equals("create");
                if (t == null && !isCreatingTeam)
                {
                    await arg.RespondAsync("You are not on a team!", ephemeral: true);
                    return;
                }
            }
            
            switch(firstName.Name)
            {
                case "set-team-icon":
                    await arg.RespondAsync("Upload an image (512x512) in this channel as your next message to set the team icon!", ephemeral: true);
                    setIconDict.Remove(arg.User.Id);
                    setIconDict.Add(arg.User.Id, arg.Channel.Id);
                    break;
                case "icon-2-lcd":
                    if (arg.Channel is IPrivateChannel)
                    {
                        int value = firstName.Options != null ? (int)firstName.Options.First().Value : 1;
                        await arg.RespondAsync($"Rendering {(value == 1 ? "5bit" : "3bit")} image, this make take a while...");

                        using (var icon = t.GetIcon())
                        {
                            byte[] image = ConvertImage(icon, value);
                            using MemoryStream stream = new MemoryStream(image);
                            using MemoryStream imageStream = new MemoryStream();
                            icon.Write(imageStream);
                            imageStream.Position = 0;

                            await arg.Channel.SendFileAsync(imageStream, "outputImage.png", "Output image render");
                            await arg.Channel.SendFileAsync(stream, "outputFile.txt", "Text file");
                        }
                    }
                    else
                        await arg.RespondAsync("Please DM the bot this command", ephemeral: true);
                    break;
                case "info":
                    string members = "";
                    foreach (var m in t.GetPlayers())
                        members += $"<@{m}>\n";

                    string subs = "";
                    foreach (var m in t.Subs)
                        subs += $"<@{m}>\n";

                    await arg.RespondAsync(embed: new EmbedBuilder()
                        .WithColor(t.GetTeamColor().ToDiscordColor())
                        .WithAuthor("Starcore team")
                        .WithTitle($"{t.Name} ({t.Tag})")
                        .AddField(new EmbedFieldBuilder().WithName("Info")
                            .WithValue($"Created: <t:{(int)t.Creation.Subtract(DateTime.UnixEpoch).TotalSeconds}:R>\nLeader: {Utils.GetUsername(t.Leader)}"))
                        .AddField(new EmbedFieldBuilder().WithName("Members").WithValue(members))
                        .AddField(new EmbedFieldBuilder().WithName("Subs").WithValue(string.IsNullOrEmpty(subs) ? "None" : subs))
                        .Build(), ephemeral: true);
                    break;
                case "create":
                    if (t == null)
                    {
                        string name = firstName.Options.First().Value as string;
                        string abv = (firstName.Options.Get(1).Value as string).ToUpper();

                        if (!Regex.IsMatch(name, "^[a-zA-Z0-9-_ ]{5,50}$"))
                        {
                            await arg.RespondAsync($"Invalid team name '{name}'", ephemeral: true);
                            return;
                        }
                        if (!Regex.IsMatch(abv, "^[A-Z]{3}$"))
                        {
                            await arg.RespondAsync($"Invalid abbreviation '{abv}'", ephemeral: true);
                            return;
                        }
                        if (!Data.CanSetTeamName(name))
                        {
                            await arg.RespondAsync("Name taken!", ephemeral: true);
                            return;
                        }
                        if (!Data.CanSetTeamTag(abv))
                        {
                            await arg.RespondAsync("Abbreviation taken!", ephemeral: true);
                            return;
                        }

                        ulong teamID = (ulong)(ulong.MaxValue * Utils.Rng.NextDouble());
                        var team = new WCTeam(arg.User.Id)
                        {
                            Leader = arg.User.Id,
                            Tag = abv.ToUpper(),
                            Name = name,
                            Creation = DateTime.Now,
                            TeamID = teamID,
                            CustomIcon = false,
                        };
                        team.LoadTeam();
                        team.Save();

                        await arg.RespondAsync($"Created team {name} ({abv})!\nCustomize the team further with `/team set-color` or `set-team-icon`", ephemeral: true);
                        Utils.LogToDiscord(arg.User, $"Created team '{name}' ({abv})");
                    }
                    else
                        await arg.RespondAsync("You are already on a team!", ephemeral: true);
                    break;
                case "leave":
                    t.RemovePlayer(p.DiscordID);
                    await arg.RespondAsync("You have left " + t.Name, ephemeral: true);
                    t.MessagePlayers($"{p.UserName()} has left your team");
                    break;
                case "set-color":
                    if (t.Leader == arg.User.Id)
                    {
                        t.SetTeamColor(new TeamColor(
                            (byte)Math.Clamp((int)(long)firstName.Options.Get(0).Value, 0, 255),
                            (byte)Math.Clamp((int)(long)firstName.Options.Get(1).Value, 0, 255), 
                            (byte)Math.Clamp((int)(long)firstName.Options.Get(2).Value, 0, 255)));
                        t.Save();
                        await arg.RespondAsync(embed: new EmbedBuilder()
                            .WithColor(t.GetTeamColor().ToDiscordColor())
                            .WithAuthor(t.Name)
                            .WithFooter("<- Set team color")
                            .Build(), ephemeral: true);
                    }
                    else
                        await arg.RespondAsync("You are not team leader!", ephemeral: true);
                    break;
                case "kick":
                    if (t.Leader == arg.User.Id)
                    {
                        SocketUser kick = firstName.Options.First().Value as SocketUser;
                        if (t.Leader != kick.Id)
                        {
                            if (t.RemovePlayer(kick.Id))
                            {
                                t.MessagePlayers($"{kick.Username} has been removed from your team");
                                await arg.RespondAsync("Kicked " + kick.Username, ephemeral: true);
                            }
                            else
                                await arg.RespondAsync(kick.Username + " is not on your team!", ephemeral: true);
                        }
                        else
                            await arg.RespondAsync("Cannot kick self!", ephemeral: true);
                    }
                    else
                        await arg.RespondAsync("You are not team leader!", ephemeral: true);
                    break;
                case "invite":
                    if (t.Leader == arg.User.Id)
                    {
                        SocketUser invite = firstName.Options.First().Value as SocketUser; 
                        if (!invite.IsBot && !t.GetPlayers().Contains(invite.Id))
                        {
                            var c = await invite.CreateDMChannelAsync();

                            await arg.RespondAsync($"Inviting {invite.Username} to the team...", ephemeral: true);
                            var msg = await c.SendMessageAsync(embed: new EmbedBuilder()
                                .WithAuthor(arg.User.Username)
                                .WithColor(t.GetTeamColor().ToDiscordColor())
                                .WithTitle($"{t.Name} ({t.Tag})")
                                .WithFooter($"{arg.User.Username} wants you on their team!")
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

        private static Dictionary<ulong, ulong> setIconDict = new Dictionary<ulong, ulong>();
        public static async Task ReadNextMessageIn(SocketMessage e)
        {
            if (setIconDict.ContainsKey(e.Author.Id) && setIconDict[e.Author.Id] == e.Channel.Id)
            {
                setIconDict.Remove(e.Author.Id);
                if (!GetTeamAndPlayer(e.Author, out WCPlayer p, out WCTeam team))
                {
                    if (p == null)
                        await e.Channel.SendMessageAsync("You have not registed!");
                    if (team == null)
                        await e.Channel.SendMessageAsync("You are not on a team!");
                    return;
                }
                if (e.Attachments.Count != 0)
                {
                    string url = "";
                    foreach (var x in e.Attachments)
                    {
                        Utils.Log($"{e.Author} is requesting to set team icon!");
                        string extension = Path.GetExtension(x.Filename.ToLower());
                        if (extension.Equals(".png") || extension.Equals(".jpeg") || extension.Equals(".jpg"))
                        {
                            url = x.Url;

                            using WebClient client = new WebClient();
                            using Stream stream = client.OpenRead(url);

                            using MagickImage image = new MagickImage(stream);
                            image.Resize(512, 512);

                            string savePath = Data.IconFolder + "\\" + team.TeamID + ".png";
                            File.Delete(savePath);

                            image.Write(savePath, MagickFormat.Png);
                        }
                        else
                        {
                            await e.Channel.SendMessageAsync("Upload a image (*.png | *.jpg)!");
                            return;
                        }
                        break;
                    }

                    team.CustomIcon = true;
                    team.Save();

                    await e.DeleteAsync();
                    Utils.LogImageToDiscord(e.Author, "Updated faction icon", url);
                } 
                else
                {
                    await e.Channel.SendMessageAsync("This message does not contain a file!");
                }
            }
        }

        private static async Task<bool> BtnJoinTeamCallback(SocketMessageComponent inter, WCTeam team)
        {
            await inter.DeferAsync(true);
            await inter.Message.ModifyAsync((e) =>
            {
                e.Components = new ComponentBuilder()
                                .WithButton("Accept", "ac", ButtonStyle.Success, disabled: true)
                                .WithButton("Deny", "dn", ButtonStyle.Danger, disabled: true).Build();
            });
            switch (inter.Data.CustomId)
            {
                case "dn":
                    team.MessageLeader(inter.User.Username + " has denied to join your team.");
                    await inter.Channel.SendMessageAsync($"Denied invite for {team.Name}");
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
                    team.MessagePlayers(inter.User.Username + " has joined your team!");
                    team.AddPlayer(inter.User.Id);
                    await inter.Channel.SendMessageAsync($"Accepted invite for {team.Name}!");
                    break;
            }
            return true;
        }

        private static byte[] ConvertImage(MagickImage icon, int mode)
        {
            icon.InterpolativeResize(178, 178, PixelInterpolateMethod.Bilinear);
            icon.Quantize(
                new QuantizeSettings()
                {
                    Colors = mode == 0 ? 8 : 32,
                    DitherMethod = DitherMethod.Riemersma,
                    ColorSpace = ColorSpace.sRGB,
                    TreeDepth = 100
                }
            );
            var builder = ImageConverter.ConvertImage(icon, mode == 0 ? ImageConverter.BitMode.Bit3 : ImageConverter.BitMode.Bit5);
            byte[] myByteArray = Encoding.UTF8.GetBytes(builder.ToString());
            return myByteArray;
        }

        private static bool GetTeamAndPlayer(SocketUser arg, out WCPlayer player, out WCTeam team)
        {
            player = Data.GetPlayer(arg.Id);
            team = Data.GetPlayerTeam(player);
            return player != null && team != null;
        }

    }
}
