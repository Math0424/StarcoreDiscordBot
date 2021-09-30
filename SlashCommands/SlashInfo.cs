using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarcoreDiscordBot.SlashCommands
{
    class SlashInfo
    {

        public static void Init()
        {
            var cmd = new SlashCommandBuilder()
                .WithName("info")
                .WithDescription("Bot information");

            CommandManager.RegisterSlashCommand(cmd, Callback);
        }

        private static async Task Callback(SocketSlashCommand arg)
        {
            await arg.RespondAsync(embed: new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithIconUrl(Bot.Instance.Client.CurrentUser.GetAvatarUrl())
                    .WithName("Starcore")
                    .WithUrl("https://starcore.tv"))
                .WithFields(new EmbedFieldBuilder()
                .WithName("Information")
                .WithValue("Version: `2.0.0`\n" +
                    "Created: `04/26/21`\n" +
                    "Is running: `true`\n" +
                    $"`{Data.RegisteredPlayers.Count}` registered players\n" +
                    $"`{Data.RegisteredTeams.Count}` registered teams\n" +
                    $"`{Data.RegisteredBlueprints.Count}` registered blueprints\n" +
                    $"Hosting `{Data.RegisteredTournaments.Count + 1}` tournaments\n" +
                    $"Watching `{Bot.Instance.PeopleCount}` people\n" +
                    "Created by: `Math#0424`"))
                .Build(), ephemeral: true);
        }


    }
}
