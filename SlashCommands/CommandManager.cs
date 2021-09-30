using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StarcoreDiscordBot.SlashCommands
{
    class CommandManager
    {

        private static Dictionary<string, Func<SocketSlashCommand, Task>> interactions = new Dictionary<string, Func<SocketSlashCommand, Task>>();
        private static Dictionary<ulong, Func<SocketMessageComponent, Task<bool>>> buttons = new Dictionary<ulong, Func<SocketMessageComponent, Task<bool>>>();

        public CommandManager(DiscordSocketClient client)
        {
            //Bot.Instance.Client.Rest.DeleteAllGlobalCommandsAsync().GetAwaiter().GetResult();
            //Bot.Instance.MainServer.DeleteSlashCommandsAsync().GetAwaiter().GetResult();

            SlashTeam.Init();
            SlashAdmin.Init();
            SlashSubs.Init();
            SlashRegister.Init();
            SlashInfo.Init();
            SlashTournament.Init();
            SlashBlueprint.Init();
            SlashTournamentAdmin.Init();

            client.InteractionCreated += InteractionCreated;
        }

        public async static void RegisterButtonSelection(ulong id, Func<SocketMessageComponent, Task<bool>> callback = null)
        {
            buttons.Add(id, callback);
        }

        public async static void RegisterSlashCommand(SlashCommandBuilder builder, Func<SocketSlashCommand, Task> callback = null, params ApplicationCommandPermission[] perms)
        {
            try
            {
                /*if (global)
                {
                    var g = await Bot.Instance.Client.Rest.CreateGlobalCommand(builder.Build());
                    if (callback != null)
                        interactions.Add(g.Name, callback);
                } 
                else
                {*/
                var x = await Bot.Instance.Client.Rest.CreateGuildCommand(builder.Build(), Bot.Instance.MainServer.Id);

                if (callback != null)
                {
                    interactions.Remove(x.Name);
                    interactions.Add(x.Name, callback);
                }
                if (perms != null && perms.Length != 0)
                    await x.ModifyCommandPermissions(perms);
                //}

                Utils.Log($"Registered slash command '{builder.Name}'");
            }
            catch (ApplicationCommandException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Error, Formatting.Indented);
                Utils.Log(json);
            }
        }

        private async Task InteractionCreated(SocketInteraction arg)
        {
            switch (arg.Type)
            {
                //Slash commands
                case InteractionType.ApplicationCommand:
                    if (arg is SocketSlashCommand cmd && interactions.ContainsKey(cmd.Data.Name))
                    {
                        long start = DateTime.Now.Ticks;
                        string input = GetOutputString(cmd.Data.Options);
                        Utils.Log($"{arg.User} has executed the command \"{cmd.Data.Name}{input}\"");

                        await interactions[cmd.Data.Name]?.Invoke(cmd);
                        Utils.Log($"Command finish ({(int)((DateTime.Now.Ticks - start) / 100000)}ms)");
                    }
                    break;
                //Button clicks/selection dropdowns
                case InteractionType.MessageComponent:
                    if(arg is SocketMessageComponent interaction)
                    {
                        if (buttons.ContainsKey(interaction.Message.Id))
                        {
                            Utils.Log($"{arg.User} has pushed a buttonID '{interaction.Data.CustomId}' on messageID '{interaction.Message.Id}'");
                            if (await buttons[interaction.Message.Id].Invoke(interaction))
                            {
                                buttons.Remove(interaction.Message.Id);
                            }
                        }
                        else
                        {
                            await interaction.AcknowledgeAsync();
                        }
                    }
                    break;
                default:
                    Console.WriteLine("Unsupported interaction type: " + arg.Type);
                    break;
            }
        }

        public string GetOutputString(IReadOnlyCollection<SocketSlashCommandDataOption> data)
        {
            string returned = string.Empty;
            if (data != null)
                foreach (var c in data)
                {
                    returned += $" {c.Name}{(c.Value != null ? $": '{c.Value}'" : string.Empty)}";
                    if (c.Options != null)
                        returned += GetOutputString(c.Options);
                }
            return returned;
        }


    }
}
