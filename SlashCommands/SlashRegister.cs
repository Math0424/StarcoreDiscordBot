using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarcoreDiscordBot.SlashCommands
{
    class SlashRegister
    {

        public static void Init()
        {
            var registerCmd = new SlashCommandBuilder()
                .WithName("register")
                .WithDescription("Link your steam and discord account")
                .AddOption("steam-id", ApplicationCommandOptionType.String, "Your steamID", false);

            CommandManager.RegisterSlashCommand(registerCmd, Callback);
        }

        private static async Task Callback(SocketSlashCommand arg)
        {
            EmbedBuilder response = new EmbedBuilder().WithColor(Color.Red).WithTitle("Starcore").WithUrl("https://starcore.tv/");
            if (arg.Data.Options == null)
            {
                var p = Data.GetPlayer(arg.User.Id);
                if (p == null)
                {
                    response.AddField(new EmbedFieldBuilder().WithName("Resgistration").WithValue("Please visit the site below to find your SteamID (steamID64) \nhttps://www.steamidfinder.com/"));
                }
                else
                {
                    response.AddField(new EmbedFieldBuilder().WithName("Registration").WithValue($"Your registered steamID is {p.SteamID}"));
                    response.WithImageUrl($"https://www.steamidfinder.com/signature/{p.SteamID}.png");
                }
            }
            else
            {
                string id = arg.Data.Options.First().Value as string;
                if (ulong.TryParse(id, out ulong longId))
                {
                    Data.GetOrCreatePlayer(arg.User.Id, out WCPlayer newP);
                    newP.SteamID = longId;
                    response.AddField(new EmbedFieldBuilder().WithName("Registration").WithValue($"Your registered steamID is now {newP.SteamID}"));
                    response.WithImageUrl($"https://www.steamidfinder.com/signature/{newP.SteamID}.png");
                    newP.Save();
                }
                else
                {
                    response.AddField(new EmbedFieldBuilder().WithName("Registration").WithValue($"{id}' is not a valid steam ID.\nIt should look something like this '76561198161316860'"));
                }
            }
            await arg.RespondAsync(embed: response.Build(), ephemeral: true);
        }


    }
}
