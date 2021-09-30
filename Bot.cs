using Discord;
using Discord.Commands;
using Discord.WebSocket;
using StarcoreDiscordBot.Networking;
using StarcoreDiscordBot.SlashCommands;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace StarcoreDiscordBot
{
    class Bot
    {
        public static Bot Instance { get; private set; }

        //private CommandHandler commands;
        public int PeopleCount = 0;

        public SocketGuild MainServer { get; private set; }
        public DiscordSocketClient Client { get; private set; }

        public SocketTextChannel LogChannel { get; private set; }
        public SocketTextChannel General { get; private set; }
        public SocketTextChannel Admin { get; private set; }

        public static Pipelines Pipes;

        static void Main(string[] args) {

            Pipes = new Pipelines();
            
            try
            {
                new Task(async () => ConsoleIn()).Start();
                new Bot().MainAsync().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Utils.Log("A critical error has occured!");
                Utils.Log(e);
            }
        }

        public static void ConsoleIn()
        {
            while (true)
            {
                switch (Console.ReadLine().ToString())
                {
                    case "stop":
                        Instance.Client.SetActivityAsync(new Game("shutting down"));
                        Instance.Client.StopAsync();
                        Environment.Exit(0);
                        break;
                }
            }
            
        }

        public async Task MainAsync()
        {
            Instance = this;

            DiscordSocketConfig config = new DiscordSocketConfig();
            config.GatewayIntents = GatewayIntents.GuildMembers | GatewayIntents.AllUnprivileged;
            config.AlwaysDownloadUsers = true;
            config.AlwaysAcknowledgeInteractions = false;

            Client = new DiscordSocketClient(config);
            Client.Log += Utils.Log;

            Utils.Log("Initalizing bot");
            var commands = new CommandHandler(Client);
            await commands.InstallCommandsAsync();

            Utils.Log("Loading main data");
            Config.Load();
            Config.Instance.Save();
            Data.Load();

            Utils.Log("Logging in...");
            await Client.LoginAsync(TokenType.Bot, Config.Instance.Token);
            await Client.StartAsync();

            Client.Ready += LoggedIn;

            Game activity = new Game("with snakes");
            await Client.SetActivityAsync(activity);

            await Task.Delay(-1);
        }

        private async Task LoggedIn()
        {
            Utils.Log("Logged in!");
            if (!LoadChannels())
            {
                await Client.StopAsync();
                return;
            }

            Utils.Log($"Loading {Client.Guilds.Count} servers");
            foreach (var s in Client.Guilds)
            {
                Utils.Log($"{s.Name}: Found {s.MemberCount} members!");
                PeopleCount += s.MemberCount;
            }
            
            await Task.Delay(1000);

            Utils.Log("Loading casual data");
            WCCasuals.Load();

            Utils.Log("Loading team data");
            WCTeam.Load();
            Utils.Log($"Loaded {Data.RegisteredTeams.Count} teams");

            Utils.Log("Loading player data");
            WCPlayer.Load();
            Utils.Log($"Loaded {Data.RegisteredPlayers.Count} players");

            Utils.Log("Loading blueprint data");
            WCBlueprint.Load();
            Utils.Log($"Loaded {Data.RegisteredBlueprints.Count} blueprints");

            Utils.Log("Loading tournament data");
            WCTournament.Load();
            Utils.Log($"Loaded {Data.RegisteredTournaments.Count} tournaments");

            new CommandManager(Client);
        }

        private bool LoadChannels()
        {
            MainServer = Client.GetGuild(Config.Instance.GuildID);
            if (MainServer == null)
            {
                Utils.Log("Main server not found!");
                return false;
            }

            LogChannel = getChannel(Config.Instance.LogChannel) as SocketTextChannel;
            if (LogChannel == null)
            {
                Utils.Log("Log channel not found!");
                return false;
            }

            return true;
        }

        private SocketChannel getChannel(ulong id)
        {
            var channel = Client.GetChannel(id);
            return channel;
        }


    }
}
