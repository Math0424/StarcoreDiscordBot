using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StarcoreDiscordBot
{
    class CommandHandler
    {

        private readonly DiscordSocketClient _client;
        public static CommandService Commands { get; private set; }

        private static List<ulong> BlackList = new List<ulong>() {
            343823737897877515, //june
            530750476472549386, //twsd
            242785518763376641, //math
        };

        public CommandHandler(DiscordSocketClient client)
        {
            CommandServiceConfig config = new CommandServiceConfig()
            {
                CaseSensitiveCommands = false,
                LogLevel = LogSeverity.Info
            };
            Commands = new CommandService(config);
            _client = client;
        }

        public async Task InstallCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;

            await Commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            var context = new SocketCommandContext(_client, message);
            
            if (context.IsPrivate && !context.User.IsBot)
            {
                if (!message.Content.Equals(string.Empty))
                {
                    Utils.Log("DM: " + message.Author.Username + ": " + message.Content);
                }
                foreach (var a in message.Attachments)
                {
                    Utils.Log("DM: " + message.Author.Username + ": " + a.Url);
                }
            }

            if (!BlackList.Contains(message.Author.Id))
            {
                if (!string.IsNullOrEmpty(message.Content))
                {
                    if (message.Content.ToLower().Contains("plugin"))
                    {
                        Utils.GetUser(242785518763376641).SendMessageAsync(message.GetJumpUrl());
                        message.AddReactionAsync(new Emoji("\uD83D\uDC0D"));
                    }
                    foreach (var u in message.MentionedUsers)
                    {
                        switch (u.Id)
                        {
                            case 242785518763376641: //@Math
                                message.AddReactionAsync(Emote.Parse("<:rat:858135403978031115>"));
                                break;
                            case 819361775350448159: //@StarCore
                                message.AddReactionAsync(Emote.Parse("<:flush:855274606772486183>"));
                                break;
                        }
                    }
                    if (message.MentionedEveryone)
                    {
                        message.AddReactionAsync(Emote.Parse("<:veryangry:855272409615302696>"));
                    }
                }
            }

            int argPos = 0;
            if (message.HasStringPrefix("!sc ", ref argPos) && !message.Author.IsBot)
                await message.Channel.SendMessageAsync("`!sc` is no longer supported!");


            /*Thread t = new Thread(async () => {
                long start = DateTime.Now.Ticks;
                Utils.Log("Command start");
                await Commands.ExecuteAsync(context, argPos, null);
                Utils.Log($"Command finish ({(int)((DateTime.Now.Ticks - start) / 100000)}ms)");
            });
            t.IsBackground = true;
            t.Start();*/
        }
    }
}
