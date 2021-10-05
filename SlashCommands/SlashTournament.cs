using Discord;
using Discord.WebSocket;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StarcoreDiscordBot.SlashCommands
{
    class SlashTournament
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "HAA0101:Array allocation for params parameter")]
        public static void Init()
        {
            var cmd = new SlashCommandBuilder()
                .WithName("tournament")
                .WithDescription("Tournament commands")
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("join")
                    .WithDescription("Join a tournament")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddAllValidTournChoices())
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("leave")
                    .WithDescription("Leave a tournament")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddAllValidTournChoices())
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("showcase")
                    .WithDescription("Showcase your tournament setup")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddAllValidTournChoices()
                    .AddOption("team-id", ApplicationCommandOptionType.String, "Team to showcase", false))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("addblueprint")
                    .WithDescription("Add a blueprint to the tournament")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddAllValidTournChoices()
                    .AddOption("blueprint-id", ApplicationCommandOptionType.Integer, "Blueprint ID"))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("removeblueprint")
                    .WithDescription("Remove a blueprint from the tournament")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddAllValidTournChoices()
                    .AddOption("blueprint-id", ApplicationCommandOptionType.Integer, "Blueprint ID"))
                .AddOption(new SlashCommandOptionBuilder()
                    .WithName("listblueprint")
                    .WithDescription("List all blueprints for this tournament")
                    .WithType(ApplicationCommandOptionType.SubCommand)
                    .AddAllValidTournChoices());

            CommandManager.RegisterSlashCommand(cmd, Callback);
        }

        private static async Task Callback(SocketSlashCommand arg)
        {
            var firstName = arg.Data.Options.First();

            WCPlayer p = Data.GetPlayer(arg.User.Id);
            if (p == null)
            {
                await arg.RespondAsync("You are not registed!", ephemeral: true);
                return;
            }
            WCTeam team = p.GetTeam();
            if (team == null)
            {
                await arg.RespondAsync("You are not on a team!", ephemeral: true);
                return;
            }

            TournamentEntry entry;
            switch (firstName.Name)
            {
                case "leave":
                    if (team.Leader != arg.User.Id)
                    {
                        await arg.RespondAsync("You are not team leader!", ephemeral: true);
                        return;
                    }
                    WCTournament tourn = Data.GetTournament((int)(long)firstName.Options.Get(0).Value);
                    if (tourn != null)
                    {
                        if (tourn.Locked)
                        {
                            await arg.RespondAsync("Tournament is locked!", ephemeral: true);
                            return;
                        }
                        if (tourn.RemoveEntry(team))
                        {
                            await arg.RespondAsync($"Left Tournament", ephemeral: true);
                            team.MessagePlayers($"You have left Tournament '{tourn.Name}'");
                        }
                        else
                        {
                            await arg.RespondAsync("you are not in this Tournament", ephemeral: true);
                        }
                    }
                    break;
                case "join":
                    if (team.Leader != arg.User.Id)
                    {
                        await arg.RespondAsync("You are not team leader!", ephemeral: true);
                        return;
                    }
                    tourn = Data.GetTournament((int)(long)firstName.Options.Get(0).Value);
                    if (tourn != null)
                    {
                        if (tourn.Locked)
                        {
                            await arg.RespondAsync("Tournament is locked!", ephemeral: true);
                            return;
                        }
                        if (tourn.IsFull)
                        {
                            await arg.RespondAsync("Tournament is full!", ephemeral: true);
                            return;
                        }
                        if (tourn.AddEntry(team))
                        {
                            await arg.RespondAsync($"Joined Tournament", ephemeral: true);
                            team.MessagePlayers($"You have joined Tournament '{tourn.Name}'");
                        }
                        else
                        {
                            await arg.RespondAsync("You have already entered for this Tournament", ephemeral: true);
                        }
                    }
                    break;
                case "addblueprint":
                    tourn = Data.GetTournament((int)(long)firstName.Options.Get(0).Value);
                    if (tourn != null)
                    {
                        if (tourn.GetEntry(team, out entry))
                        {
                            if (!tourn.Locked)
                            {
                                int blueprintId = (int)(long)firstName.Options.Get(1).Value;
                                if (tourn.MaxTeamShips == -1 || tourn.MaxTeamShips > entry.GetShips().Count)
                                {
                                    if (tourn.CanAddBlueprint(entry, blueprintId, out BlueprintReader.BlueprintInfo info, out string Error)) 
                                    {
                                        entry.AddShip(blueprintId, info.BattlePoints, info.BlockCount);
                                        tourn.Save();
                                        await arg.RespondAsync($"Added blueprint '{blueprintId}'", ephemeral: true);
                                    } 
                                    else
                                    {
                                        await arg.RespondAsync($"Cannot add blueprint! '{Error}'", ephemeral: true);
                                    }
                                }
                                else
                                {
                                    await arg.RespondAsync($"Team blueprint limit reached!", ephemeral: true);
                                }
                            } 
                            else
                            {
                                await arg.RespondAsync($"Tournament blueprints locked!", ephemeral: true);
                            }
                        }
                        else
                        {
                            await arg.RespondAsync("you are not in this Tournament", ephemeral: true);
                        }
                    }
                    break;
                case "removeblueprint":
                    tourn = Data.GetTournament((int)(long)firstName.Options.Get(0).Value);
                    if (tourn != null)
                    {
                        if (tourn.GetEntry(team, out entry))
                        {
                            int blueprintId = (int)(long)firstName.Options.Get(1).Value;
                            if (!tourn.Locked)
                            {
                                if (tourn.GetBlueprintInfo(blueprintId, out _, out BlueprintReader.BlueprintInfo info))
                                {
                                    if (entry.RemoveShip(blueprintId, info.BattlePoints, info.BlockCount))
                                    {
                                        tourn.Save();
                                        await arg.RespondAsync($"Removed blueprint '{blueprintId}'", ephemeral: true);
                                    }
                                }
                                await arg.RespondAsync($"Blueprint not found", ephemeral: true);
                                return;
                            }
                            else
                            {
                                await arg.RespondAsync($"Tournament blueprints locked!", ephemeral: true);
                            }
                        }
                        else
                        {
                            await arg.RespondAsync("you are not in this Tournament", ephemeral: true);
                        }
                    }
                    break;
                case "listblueprint":
                    tourn = Data.GetTournament((int)(long)firstName.Options.Get(0).Value);
                    if (tourn != null)
                    {
                        if (tourn.GetEntry(team, out entry))
                        {
                            EmbedBuilder builder = new EmbedBuilder();
                            builder.WithAuthor("Blueprints");
                            builder.Color = Color.Orange;
                            foreach (var x in entry.GetShips())
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
                        } 
                        else
                        {
                            await arg.RespondAsync("you are not in this Tournament", ephemeral: true);
                        }
                    }
                    break;
                case "showcase":
                    tourn = Data.GetTournament((int)(long)firstName.Options.Get(0).Value);
                    if (firstName.Options.Count == 2)
                    {
                        string teamString = (string)firstName.Options.Get(1).Value;
                        team = Data.GetTeam(teamString);
                        if (team == null)
                        {
                            await arg.RespondAsync($"Team {teamString} not found!", ephemeral: true);
                            return;
                        }
                    }
                    if (tourn != null && tourn.GetEntry(team, out entry))
                    {
                        using MagickImage image = CreateShowcase(tourn, team, entry);
                        using (Stream stream = new MemoryStream())
                        {
                            image.Write(stream);
                            stream.Position = 0;
                            await arg.RespondAsync("Generated image", ephemeral: true);
                            await arg.Channel.SendFileAsync(stream, "Showcase.png", $"Team showcase requested By: <@{arg.User.Id}>");
                        }
                        image.Dispose();
                    }
                    else
                    {
                        await arg.RespondAsync("Team is not in this Tournament", ephemeral: true);
                    }
                    break;
            }
        }


        public static MagickImage CreateShowcase(WCTournament tourn, WCTeam team, TournamentEntry entry)
        {
            MagickImage background = new MagickImage(Path.Combine(Utils.GetResourcesFolder(), "background_square.png"));

            using MagickImage tournImage = tourn.GetIcon();

            using MagickImage teamIcon = team.GetIcon();
            teamIcon.BackgroundColor = MagickColors.None;
            teamIcon.Resize(500, 500);
            teamIcon.Extent(512, 512, Gravity.Center, MagickColors.None);

            foreach (var p in team.GetPlayers())
            {
                var u = Utils.GetUser(p);
                int offset = 0;
                if (u != null)
                {
                    using WebClient client = new WebClient();
                    using Stream stream = client.OpenRead(u.GetAvatarUrl(ImageFormat.Png) ?? u.GetDefaultAvatarUrl());
                    using MagickImage icon = new MagickImage(stream);
                    icon.Resize(80, 80);
                    icon.Distort(DistortMethod.DePolar, 0);
                    icon.VirtualPixelMethod = VirtualPixelMethod.HorizontalTile;
                    icon.BackgroundColor = MagickColors.None;
                    icon.Distort(DistortMethod.Polar, 0);
                    teamIcon.Composite(icon, Gravity.Southwest, 0, offset, CompositeOperator.Over);
                    offset -= 85;
                }
            }

            List<MagickImage> ships = new List<MagickImage>();
            foreach (var x in entry.GetShips())
            {
                var b = Data.GetBlueprint(x);
                if (b != null)
                {
                    MagickImage image = b.GetThumbnail();

                    double scale = image.Height / (double)image.Width;
                    image.Resize((int)(512 / scale), (int)(512 / scale));
                    image.Crop(490, 490, Gravity.Center);
                    image.Extent(500, 500, Gravity.Center, team.GetTeamColor().ToMagickColor());
                    image.Extent(512, 512, Gravity.Center, new MagickColor(0, 0, 0, 0));

                    ships.Add(image);
                }
            }

            switch (ships.Count)
            {
                case 1:
                    background.Crop(512 * 2, 512);
                    background.Composite(teamIcon, Gravity.Northwest, 0, 0, CompositeOperator.Atop);

                    tournImage.Resize(100, 100);
                    background.Composite(tournImage, Gravity.Northwest, 0, 0, CompositeOperator.Atop);

                    background.Composite(ships[0], Gravity.East, 0, 0, CompositeOperator.Atop);
                    break;
                case 2:
                    background.Resize(512 * 2, 512 * 2);
                    background.Composite(teamIcon, Gravity.Northwest, 0, 0, CompositeOperator.Atop);

                    tournImage.Resize(512, 512);
                    background.Composite(tournImage, Gravity.Southeast, 0, 0, CompositeOperator.Atop);

                    background.Composite(ships[0], Gravity.Northeast, 0, 0, CompositeOperator.Atop);
                    background.Composite(ships[1], Gravity.Southwest, 0, 0, CompositeOperator.Atop);
                    break;
                case 3:
                case 4:
                    background.Resize(512 * 3, 512 * 3);
                    teamIcon.Resize(1024, 1024);
                    background.Composite(teamIcon, Gravity.Northwest, 0, 0, CompositeOperator.Atop);

                    tournImage.Resize(512, 512);
                    background.Composite(tournImage, Gravity.Southeast, 0, 0, CompositeOperator.Atop);

                    background.Composite(ships[0], Gravity.Northeast, 0, 0, CompositeOperator.Atop);
                    background.Composite(ships[1], Gravity.East, 0, 0, CompositeOperator.Atop);
                    background.Composite(ships[2], Gravity.Southwest, 0, 0, CompositeOperator.Atop);
                    if (ships.Count == 4)
                        background.Composite(ships[3], Gravity.South, 0, 0, CompositeOperator.Atop);
                    break;
                case 5:
                case 6:
                case 7:
                case 8:
                    background.Resize(512 * 3, 512 * 3);
                    tournImage.Resize(512 * 3, 512 * 3);
                    background.Composite(tournImage, Gravity.Center, 0, 0, CompositeOperator.Atop);
                    background.Composite(teamIcon, Gravity.Center, 0, 0, CompositeOperator.Atop);
                    
                    int row = 0, collum = 0;
                    for(int i = 0; i < ships.Count; i++)
                    {
                        background.Composite(ships[i], 512 * row, 512 * collum, CompositeOperator.Atop);
                        if (row == 2)
                            collum++;

                        row = (row + 1) % 3;
                        if (collum == 1 && row == 1)
                            row++;
                    }
                    break;
                default:
                    background.Composite(teamIcon, Gravity.Center, 0, 0, CompositeOperator.Atop);
                    break;
            }

            foreach (var i in ships)
                i.Dispose();

            return background;
        }
    }
}
