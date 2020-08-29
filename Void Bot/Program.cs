using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;

namespace Void_Bot
{
    public class Program
    {
        public static DiscordShardedClient discord;

        private static readonly string token = File.ReadAllText("token.txt");

        public static bool customstatus = false;

        private static readonly List<string> bannedwords = new List<string>
        {
            "aidan  not cute", "aidan not cute", "a idan not cute", "aidan no t cute", "aiidan not cute",
            "aaiidaan not cuute", "aidan ulge", "aidan not cu te", "aidan ugle", "aidan n o t cute",
            "aidan is not cute", "aidanugly", "aidan ugly", "ai dan not cute", "aidan is uncute", "aidann not cute",
            "aidan is ugle", "a i d a n  n o t  c u t e"
        };

        public static IReadOnlyDictionary<int, CommandsNextExtension> Commands { get; set; }

        public static IReadOnlyDictionary<int, LavalinkExtension> Lavalink { get; set; }

        public static Dictionary<int, LavalinkNodeConnection> LavalinkNodes { get; set; }

        public static IReadOnlyDictionary<int, InteractivityExtension> Interactivity { get; set; }

        public static ConnectionEndpoint endpoint = new ConnectionEndpoint
        {
            Hostname = "127.0.0.1",
            Port = 2333
        };

        public static LavalinkConfiguration lavalinkConfig = new LavalinkConfiguration
        {
            Password = "youshallnotpass",
            RestEndpoint = endpoint,
            SocketEndpoint = endpoint
        };

    public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            while (true)
            {
                try
                {
                    var a = MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
                }
                catch (Exception value)
                {
                    Console.WriteLine(value);
                    throw;
                }
            }
        }

        private static async Task<string> MainAsync(string[] args)
        {
            discord = new DiscordShardedClient(new DiscordConfiguration
            {
                Token = token,
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Info
            });
            
            Lavalink = await discord.UseLavalinkAsync();

            var icfg = new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromSeconds(20.0)
            };
            Interactivity = await discord.UseInteractivityAsync(icfg);
            discord.GuildMemberAdded += Discord_GuildMemberAdded;


            Commands = await discord.UseCommandsNextAsync(new CommandsNextConfiguration
            {
                PrefixResolver = ResolvePrefix,
                EnableMentionPrefix = false
            });


            foreach (var commands in Commands.Values)
            {
                commands.RegisterCommands<Commands>();
                commands.RegisterCommands<UtilityCommands>();
                commands.RegisterCommands<AudioCommands>();
                commands.RegisterCommands<AdministrationCommands>();
                commands.RegisterCommands<FunCommands>();
                commands.RegisterCommands<ExternalCommands>();
                commands.CommandErrored += Commands_CommandErrored;
                commands.CommandExecuted += Commands_CommandExecuted;
            }


            discord.MessageCreated += Discord_MessageCreated;
            await discord.StartAsync();

            try
            {
                foreach (var extension in Lavalink.Values)
                { 
                    AudioCommands.Lavalink = await extension.ConnectAsync(lavalinkConfig);
                }
            }
            catch (Exception ex)
            {
                if (ex is SocketException || ex is HttpRequestException || ex is WebSocketException)
                    Console.WriteLine("Can't connect to lavalink! (music commands are disabled)");
                else
                    throw;
            }

            await Task.Delay(2000);
            while (true)
            {
                if (!customstatus)
                {
                    var amount = 0;
                    foreach (var elem in discord.ShardClients.Values)
                    {
                        foreach (var guild in elem.Guilds) amount += guild.Value.Members.Values.Count(x => !x.IsBot);
                    }
                    var status = amount + " users";
                    var activity = new DiscordActivity(status, ActivityType.Watching);
                    await discord.UpdateStatusAsync(activity);
                }

                await Task.Delay(TimeSpan.FromMinutes(1));

                if (!customstatus)
                {
                    var amount = 0;
                    foreach (var elem in discord.ShardClients.Values)
                    {
                        amount += elem.Guilds.Count;
                    }

                    var status = amount + " servers";
                    var activity = new DiscordActivity(status, ActivityType.Watching);
                    await discord.UpdateStatusAsync(activity);
                }

                await Task.Delay(TimeSpan.FromMinutes(1)); 
                
                if (!customstatus)
                {
                    foreach (var elem in discord.ShardClients.Values)
                    {
                        var activity = new DiscordActivity($"Shard {elem.ShardId + 1} of {elem.ShardCount}", ActivityType.Watching);
                        await discord.UpdateStatusAsync(activity);
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        private static async Task Discord_MessageCreated(MessageCreateEventArgs e)
        {
            if (!(e.Guild == null) && e.Guild.Id.ToString() == "642067509931147264")
                foreach (var elem in bannedwords)
                {
                    if (!e.Message.Content.ToLower().Contains(elem)) continue;
                    await e.Message.DeleteAsync();
                }
        }

        private static async Task Commands_CommandErrored(CommandErrorEventArgs e)
        {

            
            DiscordEmbedBuilder embed = null;
            var ex = e.Exception;
            while (ex is AggregateException) ex = ex.InnerException;
            if (e.Context.Guild == null)
            {
                await e.Context.RespondAsync("This command cannot be used in DM channels! Please try another!");
                return;
            }

            if (ex is ArgumentException && e.Context.RawArgumentString == "")
            {
                await e.Context.RespondAsync("Command Help:");
                await new CommandsNextExtension.DefaultHelpModule().DefaultHelpAsync(e.Context, e.Command.Name);
            }
            else if (ex is InvalidOperationException)
            {
                await new CommandsNextExtension.DefaultHelpModule().DefaultHelpAsync(e.Context, e.Command.Name);
            }
            else if (!(ex is ArgumentException) && !ex.Message.Equals("Specified command was not found."))
            {
                embed = !(ex is ChecksFailedException)
                    ? new DiscordEmbedBuilder
                    {
                        Title = "A problem occured while executing the command",
                        Description =
                            $"{Formatter.InlineCode(e.Command.QualifiedName)} threw an exception: `{ex.GetType()}: {ex.Message}`",
                        Color = new DiscordColor(16711680)
                    }
                    : new DiscordEmbedBuilder
                    {
                        Title = "Permission denied",
                        Description = "You lack permissions necessary to run this command.",
                        Color = new DiscordColor(16711680)
                    };
            }
            else if (ex.Message.Equals("Specified command was not found."))
            {
                await e.Context.RespondAsync("Specified command was not found.\nCommand Help:");
                await new CommandsNextExtension.DefaultHelpModule().DefaultHelpAsync(e.Context);
            }
            else
            {
                await e.Context.RespondAsync("An error occurred! Check the command help and if the error persists please contact `VoidRaven#0042`");
                await new CommandsNextExtension.DefaultHelpModule().DefaultHelpAsync(e.Context, e.Command.Name);
            }
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "Void Bot",
                string.Format("User '{0}#{1}' ({2}) tried to execute '{3}' ", e.Context.User.Username,
                    e.Context.User.Discriminator, e.Context.User.Id, e.Command?.QualifiedName ?? "<unknown command>") +
                $"in #{e.Context.Channel.Name} ({e.Context.Channel.Id}) in {e.Context.Guild.Name} ({e.Context.Guild.Id}) and failed with {e.Exception.GetType()}: {e.Exception.Message}",
                DateTime.Now);
            if (embed != null) await e.Context.RespondAsync("", false, embed.Build());
        }

        private static async Task Commands_CommandExecuted(CommandExecutionEventArgs e)
        {
            discord.DebugLogger.LogMessage(LogLevel.Info, "Void Bot",
                e.Context.Guild == null
                    ? $"{e.Context.User.Username} executed '{e.Command.QualifiedName}' in {e.Context.Channel.Name} ({e.Context.Channel.Id}) in (a DM channel))."
                    : $"{e.Context.User.Username} executed '{e.Command.QualifiedName}' in {e.Context.Channel.Name} ({e.Context.Channel.Id}) in {e.Context.Guild.Name} ({e.Context.Guild.Id}).",
                DateTime.Now);
        }

        private static async Task Discord_GuildMemberAdded(GuildMemberAddEventArgs e)
        {
            var Embed = new DiscordEmbedBuilder
            {
                Title = "New Member!",
                Color = DiscordColor.Gold,
                ImageUrl = e.Member.AvatarUrl
            };
            Embed.AddField("Welcome to ", e.Guild.Name, true);
            Embed.AddField("New member: ", e.Member.DisplayName + "#" + e.Member.Discriminator, true);
            if (e.Guild.SystemChannel != null)
                await e.Guild.SystemChannel.SendMessageAsync(null, false, Embed);
        }

        private static void OnProcessExit(object sender, EventArgs e)
        {
            Console.WriteLine("Disconnecting from all shards, then exiting in 2 seconds");
            foreach (var elem in discord.ShardClients.Values)
            {
                elem.DisconnectAsync();
            }
            Thread.Sleep(2000);
        }

        private static Task<int> ResolvePrefix(DiscordMessage msg)
        {
            var array = File.ReadAllLines("prefixes.txt").ToArray();
            var guildprefixes = new Dictionary<string, string>();
            var customPrefix = new string[2]
            {
                "v.",
                null
            };
            var array2 = array;
            for (var k = 0; k < array2.Length; k++)
            {
                var split = array2[k].Split(':');
                guildprefixes.Add(split[0], split[1]);
            }

            if (msg.Channel.Guild != null)
            {
                if (guildprefixes.ContainsKey(msg.Channel.Guild.Id.ToString()))
                    customPrefix[1] = guildprefixes[msg.Channel.Guild.Id.ToString()];
            }
            var argPos = msg.GetMentionPrefixLength(discord.CurrentUser);
            if (customPrefix[1] == null)
            {
                var j = 0;
                while (argPos == -1 && j < customPrefix.Length - 1)
                {
                    argPos = msg.GetStringPrefixLength(customPrefix[j]);
                    j++;
                }
            }
            else
            {
                var i = 0;
                while (argPos == -1 && i < customPrefix.Length)
                {
                    argPos = msg.GetStringPrefixLength(customPrefix[i]);
                    i++;
                }
            }

            return Task.FromResult(argPos);
        }
    }
}