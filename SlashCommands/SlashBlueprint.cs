using Discord;
using Discord.WebSocket;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace StarcoreDiscordBot.SlashCommands
{
    class SlashBlueprint
    {

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Nonsense", "HAA0101")]
        public static void Init()
        {
            Bot.Instance.Client.MessageReceived += ReadNextMessageIn;

            var cmd = new SlashCommandBuilder()
                .WithName("blueprint")
                .WithDescription("Blueprint managment commands")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("upload")
                    .WithDescription("upload a blueprint")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("workshop-link", ApplicationCommandOptionType.String, "workshop link", false))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("delete")
                    .WithDescription("delete a blueprint")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("bp-id", ApplicationCommandOptionType.Integer, "blueprint id", true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("list")
                    .WithDescription("list all blueprints")
                    .WithType(ApplicationCommandOptionType.SubCommand))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("info")
                    .WithDescription("list basic information on blueprint")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("bp-id", ApplicationCommandOptionType.Integer, "blueprint id", true))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("showcase")
                    .WithDescription("showcase select blueprint")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddOption("bp-id", ApplicationCommandOptionType.Integer, "blueprint id", true));

            CommandManager.RegisterSlashCommand(cmd, Callback);
        }

        private static Dictionary<ulong, SocketSlashCommand> uploadBPDict = new Dictionary<ulong, SocketSlashCommand>();

        private static async Task Callback(SocketSlashCommand arg)
        {
            var firstName = arg.Data.Options.First();

            WCPlayer p = Data.GetPlayer(arg.User.Id);
            if (p == null)
            {
                await arg.RespondAsync("You are not registed!", ephemeral: true);
                return;
            }

            switch(firstName.Name)
            {
                case "delete":
                    int value = (int)(long)firstName.Options.Get(0).Value;
                    var bp = Data.GetBlueprint(value);
                    if(bp != null && bp.IsOwner(p))
                    {
                        p.RemoveFromRegistedBlueprints(bp.GID);
                        Utils.LogToDiscord(arg.User, $"Deleted blueprint '{bp.Title}'");
                        await arg.RespondAsync($"Deleted blueprint '{bp.Title}'", ephemeral: true);
                    }
                    else if(p.GetRegistedBlueprints().Contains(value))
                    {
                        p.RemoveFromRegistedBlueprints(value);
                        await arg.RespondAsync($"Removed invalid blueprint with ID: '{value}'", ephemeral: true);
                    }
                    else
                    {
                        await arg.RespondAsync("Unknown blueprint ID", ephemeral: true);
                    }
                    break;
                case "list":
                    EmbedBuilder builder = new EmbedBuilder();
                    builder.WithAuthor("Blueprints");
                    builder.Color = Color.Orange;
                    foreach (var x in p.GetRegistedBlueprints())
                    {
                        var b = Data.GetBlueprint(x);
                        if (b != null)
                        {
                            builder.Fields.Add(b.GetEmbed());
                        }
                        else
                        {
                            EmbedFieldBuilder field = new EmbedFieldBuilder();
                            field.Name = "Blueprint error";
                            field.Value = $"ID: {x}";
                            builder.Fields.Add(field);
                        }
                    }
                    await arg.RespondAsync(embed: builder.Build(), ephemeral: true);
                    break;
                case "showcase":
                    bp = Data.GetBlueprint((int)(long)firstName.Options.Get(0).Value);
                    if (bp != null && bp.IsOwner(p))
                    {
                        var thumb = bp.GetThumbnail();
                        if (thumb != null)
                        {
                            await arg.RespondAsync($"Preparing thumbnail for '{bp.Title}'", ephemeral: true);
                            var icon = new MagickImage(Path.Combine(Utils.GetResourcesFolder(), "StarcoreIcon.png"));
                            int resize = Math.Min(thumb.Width, thumb.Height) / 3;
                            icon.Resize(resize, resize);
                            thumb.Composite(icon, Gravity.Southeast, CompositeOperator.Atop);
                            thumb.Label = bp.Title;

                            using (WebClient client = new WebClient())
                            {
                                using Stream stream = client.OpenRead(p.Discord().GetAvatarUrl(ImageFormat.Png, 1024) ?? p.Discord().GetDefaultAvatarUrl());
                                using MagickImage pfp = new MagickImage(stream);
                                pfp.Resize((int)(resize * .8), (int)(resize * .8));
                                pfp.BackgroundColor = MagickColors.None;

                                pfp.Distort(DistortMethod.DePolar, 0);
                                pfp.VirtualPixelMethod = VirtualPixelMethod.HorizontalTile;
                                pfp.BackgroundColor = MagickColors.None;
                                pfp.Distort(DistortMethod.Polar, 0);

                                thumb.Composite(pfp, Gravity.Southwest, CompositeOperator.Atop);
                            }

                            using (Stream stream = new MemoryStream())
                            {
                                thumb.Write(stream);
                                stream.Position = 0;
                                await arg.Channel.SendFileAsync(stream, $"{bp.Title}.png");
                            }
                        }
                        else
                        {
                            await arg.RespondAsync($"Blueprint does not have a thumbnail :(", ephemeral: true);
                        }
                    }
                    else
                    {
                        await arg.RespondAsync("Unknown blueprint ID", ephemeral: true);
                    }
                    break;
                case "upload":
                    if (firstName?.Options?.Count == 1)
                    {
                        string workshopID = (string)firstName.Options.Get(0).Value;
                        int index = workshopID.LastIndexOf("?id=");
                        if (index != -1)
                        {
                            workshopID = workshopID.Substring(index + 4);
                            if (workshopID.IndexOf("&") != -1)
                            {
                                workshopID = workshopID.Substring(0, workshopID.IndexOf("&"));
                            }

                            if (ulong.TryParse(workshopID, out ulong ID))
                            {
                                await arg.DeferAsync(true);
                                var bpDown = new BlueprintDownloader(ID);
                                var downloaded = bpDown.RequestDownload(arg.User.Id);
                                if (bpDown.IsValid)
                                {
                                    p.AddToRegistedBlueprints(downloaded.GID);

                                    builder = new EmbedBuilder();
                                    builder.WithAuthor("Created blueprint");
                                    builder.Color = Color.Orange;
                                    builder.AddField(downloaded.GetEmbed());
                                    
                                    await arg.FollowupAsync(embed: builder.Build(), ephemeral: true);
                                }
                                else
                                {
                                    await arg.FollowupAsync($"Failed to download blueprint!\n{bpDown.Error}", ephemeral: true);
                                }
                                break;

                            }
                        }
                        await arg.RespondAsync("That doesnt look like a blueprint link", ephemeral: true);
                    }
                    else
                    {
                        uploadBPDict.Remove(arg.User.Id);
                        uploadBPDict.Add(arg.User.Id, arg);
                        await arg.RespondAsync("Please upload a blueprint file as your next message", ephemeral: true);
                    }
                    break;
            }
        }

        public static async Task ReadNextMessageIn(SocketMessage e)
        {
            if (uploadBPDict.ContainsKey(e.Author.Id))
            {
                SocketSlashCommand cmd = uploadBPDict[e.Author.Id];
                uploadBPDict.Remove(e.Author.Id);

                if (!cmd.IsValidToken)
                    return;

                WCPlayer p = Data.GetPlayer(e.Author.Id);
                if (p == null)
                {
                    await cmd.FollowupAsync("You are not registed! -- how did you manage that?!");
                    return;
                }

                if (e.Attachments.Count != 0)
                {
                    foreach (var x in e.Attachments)
                    {
                        if (x.Url.ToLower().EndsWith(".zip"))
                        {
                            var bpDown = new BlueprintDownloader();
                            var downloaded = bpDown.RequestDownload(p.DiscordID, p.SteamID, x.Url);
                            if (bpDown.IsValid)
                            {
                                p.AddToRegistedBlueprints(downloaded.GID);

                                var builder = new EmbedBuilder();
                                builder.WithAuthor("Created blueprint");
                                builder.Color = Color.Orange;
                                builder.AddField(downloaded.GetEmbed());

                                await e.DeleteAsync();

                                await cmd.FollowupAsync(embed: builder.Build(), ephemeral: true);
                            }
                            else
                            {
                                await cmd.FollowupAsync($"Failed to download blueprint!\n{bpDown.Error}", ephemeral: true);
                            }
                            break;
                        }
                    }
                }
                else
                {
                    await cmd.FollowupAsync("This message does not contain any files!");
                }
            }
        }

    }
}
