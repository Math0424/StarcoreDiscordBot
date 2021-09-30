using Discord;
using Discord.WebSocket;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace StarcoreDiscordBot
{
    static class Utils
    {

        public static Random Rng = new Random();


        public static string GetDataFolder()
        {
            string str = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StarcoreData");

            if (!Directory.Exists(str))
                Directory.CreateDirectory(str);

            return str;
        }

        public static string Format(string msg, WCPlayer player, WCTeam team = null)
        {
            if (msg == null)
                return msg;

            string output = msg;
            if (player != null)
            {
                output = output.Replace("{steamID}", player.SteamID.ToString());
                output = output.Replace("{discordID}", player.DiscordID.ToString());
                output = output.Replace("{username}", GetUsername(player.DiscordID));
            }
            if (team != null)
            {
                output = output.Replace("{teamID}", team.GetsID());
                output = output.Replace("{teamMMR}", team.MMR.ToString());
                output = output.Replace("{teamName}", team.Name);
                output = output.Replace("{teamTag}", team.Tag);
            }
            return output;
        }

        public static string GetResourcesFolder()
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources");
        }

        public static Task Log(LogMessage data)
        {
            Console.WriteLine(data.ToString());
            return Task.CompletedTask;
        }

        public static string GetUsername(ulong ID)
        {
            string s = Bot.Instance.Client.GetUser(ID)?.Username ?? ID.ToString();
            return s.Replace('@', '$');
        }

        public static SocketUser GetUser(ulong ID)
        {
            return Bot.Instance.Client.GetUser(ID);
        }

        public static void Log(object data)
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " Log         " + (data ?? "null").ToString());
            LogToFile(DateTime.Now.ToString("dd/MM HH:mm:ss") + " Log " + (data ?? "null").ToString());
        }

        private static StreamWriter myLog;
        public static void LogToFile(object data)
        {
            if (myLog == null)
            {
                Directory.CreateDirectory(GetDataFolder() + "\\Logs\\");
                myLog = new StreamWriter(File.Open(GetDataFolder() + "\\Logs\\" + DateTime.Now.Ticks + ".txt", FileMode.OpenOrCreate));
            }
            myLog.WriteLine(DateTime.Now.ToString("HH:mm:ss") + " " + data);
            myLog.Flush();
        }

        public static void LogToDiscord(object sender, object data)
        {
            if (data == null || sender == null)
            {
                Console.WriteLine("Trying to send invalid log to discord!");
            }

            EmbedBuilder builder = new EmbedBuilder();
            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = "Starcore log";

            builder.Author = author;
            builder.Color = Color.Blue;

            EmbedFieldBuilder field = new EmbedFieldBuilder();
            field.Name = sender.ToString();
            field.Value = data.ToString();

            builder.Fields.Add(field);

            Log(sender.ToString() + ": " + data.ToString());
            Bot.Instance.LogChannel.SendMessageAsync(null, false, builder.Build());
        }

        public static SlashCommandOptionBuilder AddAllValidTournChoices(this SlashCommandOptionBuilder builder)
        {
            SlashCommandOptionBuilder options = new SlashCommandOptionBuilder()
                .WithName("options")
                .WithDescription("Joinable Tournaments")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.Integer);

            foreach (var x in Data.RegisteredTournaments)
                options.AddChoice(x.Name, x.TornID);

            return builder.AddOption(options);
        }

        public static void LogImageToDiscord(object sender, object data, string iconUrl)
        {
            if (data == null || sender == null)
            {
                Console.WriteLine("Trying to send invalid image to discord!");
            }

            EmbedBuilder builder = new EmbedBuilder();
            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = "Starcore log";
            author.IconUrl = iconUrl;

            builder.Author = author;
            builder.Color = Color.Blue;

            EmbedFieldBuilder field = new EmbedFieldBuilder();
            field.Name = sender.ToString();
            field.Value = data.ToString();

            builder.Fields.Add(field);

            Log(sender.ToString() + ": " + data.ToString());
            Bot.Instance.LogChannel.SendMessageAsync(null, false, builder.Build());
        }

        public static T Get<T>(this IReadOnlyCollection<T> args, int value)
        {
            if (value == 0)
                return args.First();

            using var x = args.GetEnumerator();
            x.MoveNext();
            for(int i = 0; i < value; i++)
            {
                x.MoveNext();
            }
            return x.Current;
        }

    }
}
