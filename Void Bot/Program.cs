using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using DSharpPlus.VoiceNext;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace Void_Bot
{
    public class Program
    {
        public static DiscordShardedClient discord;

        public static DiscordRestClient discordrest;

        private static readonly string token = File.ReadAllText("token.txt");

        public static bool CustomStatus = false;

        public static bool Override;

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

        public static IReadOnlyDictionary<int, CommandsNextExtension> Commands { get; set; }

        public static IReadOnlyDictionary<int, LavalinkExtension> Lavalink { get; set; }

        public static Dictionary<int, LavalinkNodeConnection> LavalinkNodes { get; set; }

        public static IReadOnlyDictionary<int, InteractivityExtension> Interactivity { get; set; }

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            try
            {
                MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception value)
            {
                Console.WriteLine(value);
            }
        }

        private static async Task MainAsync(string[] args)
        {
            var DayTask = new Timer(43200000); //12 hours in ms
            DayTask.Elapsed += DayEvent;
            DayTask.Start();
            var config = new DiscordConfiguration
            {
                Token = token,
                TokenType = TokenType.Bot,
                ReconnectIndefinitely = true
            };
            discord = new DiscordShardedClient(config);

            discordrest = new DiscordRestClient(config);

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
                commands.SetHelpFormatter<HelpFormatter>();
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
                    AudioCommands.Lavalink = await extension.ConnectAsync(lavalinkConfig);
                discord.Logger.Log(LogLevel.Information, "Connected to Lavalink!");
            }
            catch (Exception ex)
            {
                if (ex is SocketException || ex is HttpRequestException || ex is WebSocketException)
                    Console.WriteLine("Can't connect to Lavalink! (music commands are disabled)");
                else
                    throw;
            }

            await Task.Delay(2000);
            while (true)
                try
                {
                    if (!CustomStatus)
                    {
                        var amount = 0;
                        foreach (var elem in discord.ShardClients.Values)
                        foreach (var guild in elem.Guilds)
                            amount += guild.Value.Members.Values.Count(x => !x.IsBot);
                        var status = amount + " users";
                        var activity = new DiscordActivity(status, ActivityType.Watching);
                        await discord.UpdateStatusAsync(activity);
                    }

                    await Task.Delay(TimeSpan.FromMinutes(1));

                    if (!CustomStatus)
                    {
                        var amount = 0;
                        foreach (var elem in discord.ShardClients.Values) amount += elem.Guilds.Count;

                        var status = amount + " servers";
                        var activity = new DiscordActivity(status, ActivityType.Watching);
                        await discord.UpdateStatusAsync(activity);
                    }

                    await Task.Delay(TimeSpan.FromMinutes(1));

                    if (!CustomStatus)
                        foreach (var elem in discord.ShardClients.Values)
                        {
                            var activity = new DiscordActivity($"Shard {elem.ShardId + 1} of {elem.ShardCount}",
                                ActivityType.Watching);
                            await discord.UpdateStatusAsync(activity);
                        }

                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
                catch
                {
                    await Task.Delay(1000);
                }
        }

        private static void DayEvent(object source, ElapsedEventArgs e)
        {
            ExternalCommands.RedditCache.Clear();
            ExternalCommands.E6Cache.Clear();
            ExternalCommands.E9Cache.Clear();
            ExternalCommands.R34Cache.Clear();
        }
        private static async Task Discord_MessageCreated(DiscordClient client, MessageCreateEventArgs e)
        {
        }

        private static async Task Commands_CommandErrored(CommandsNextExtension ext, CommandErrorEventArgs e)
        {
            Override = false;
            DiscordEmbedBuilder embed = null;
            var ex = e.Exception;
            while (ex is AggregateException) ex = ex.InnerException;
            if (e.Context.Guild == null)
            {
                await e.Context.RespondAsync("This command cannot be used in DM channels! Please try another!");
                return;
            }

            if (ex is ArgumentException)
            {
                await e.Context.RespondAsync("Command Help:");
                await new CommandsNextExtension.DefaultHelpModule().DefaultHelpAsync(e.Context, e.Command.Name);
                return;
            }

            if (ex is InvalidOperationException)
            {
                await new CommandsNextExtension.DefaultHelpModule().DefaultHelpAsync(e.Context, e.Command.Name);
            }
            else if (!ex.Message.Equals("Specified command was not found."))
            {
                if (ex is ChecksFailedException)
                {
                    embed = new DiscordEmbedBuilder
                    {
                        Title = "Permission denied",
                        Description = "You lack permissions necessary to run this command.",
                        Color = new DiscordColor(16711680)
                    };
                }
                else
                {
                    await e.Context.RespondAsync(
                        "An error occurred! Check the command help and if the error persists please contact `VoidRaven#0042`");
                    embed = new DiscordEmbedBuilder
                    {
                        Title = "A problem occured while executing the command",
                        Description =
                            $"{Formatter.InlineCode(e.Command.QualifiedName)} threw an exception: \n{ex.Message}\n\n {ex}",
                        Color = new DiscordColor(16711680)
                    };
                    var guild = await e.Context.Client.GetGuildAsync(750409700750786632);
                    await guild.GetChannel(750787712625410208).SendMessageAsync(embed: embed);
                    return;
                }
            }
            else if (ex.Message.Equals("Specified command was not found."))
            {
                await e.Context.RespondAsync("Specified command was not found.\nCommand Help:");
                await new CommandsNextExtension.DefaultHelpModule().DefaultHelpAsync(e.Context);
                return;
            }
            else
            {
                await e.Context.RespondAsync(
                    "An error occurred! Check the command help and if the error persists please contact `VoidRaven#0042`");
                await new CommandsNextExtension.DefaultHelpModule().DefaultHelpAsync(e.Context, e.Command.Name);
                embed = new DiscordEmbedBuilder
                {
                    Title = "A problem occured while executing the command",
                    Description =
                        $"{Formatter.InlineCode(e.Command.QualifiedName)} threw an exception: `{ex.GetType()}: {ex.Message}`",
                    Color = new DiscordColor(16711680)
                };
                var guild = await e.Context.Client.GetGuildAsync(750409700750786632);
                await guild.GetChannel(750787712625410208).SendMessageAsync(embed: embed);
                return;
            }

            e.Context.Client.Logger.Log(LogLevel.Error,
                $"User '{e.Context.User.Username}#{e.Context.User.Discriminator}' ({e.Context.User.Id}) tried to execute '{e.Command?.QualifiedName ?? "<unknown command>"}' " +
                $"in #{e.Context.Channel.Name} ({e.Context.Channel.Id}) in {e.Context.Guild.Name} ({e.Context.Guild.Id}) and failed with {e.Exception.GetType()}: {e.Exception.Message}",
                DateTime.Now);
            if (embed != null) await e.Context.RespondAsync("", false, embed.Build());
        }

        private static async Task Commands_CommandExecuted(CommandsNextExtension ext, CommandExecutionEventArgs e)
        {
            discord.Logger.Log(LogLevel.Information,
                e.Context.Guild == null
                    ? $"{e.Context.User.Username} executed '{e.Command.QualifiedName}' in {e.Context.Channel.Name} ({e.Context.Channel.Id} (a DM channel))."
                    : $"{e.Context.User.Username} executed '{e.Command.QualifiedName}' in {e.Context.Channel.Name} ({e.Context.Channel.Id}) in {e.Context.Guild.Name} ({e.Context.Guild.Id}).",
                DateTime.Now);
        }

        private static async Task Discord_GuildMemberAdded(DiscordClient client, GuildMemberAddEventArgs e)
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
            foreach (var elem in discord.ShardClients.Values) elem.DisconnectAsync();
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
                if (guildprefixes.ContainsKey(msg.Channel.Guild.Id.ToString()))
                    customPrefix[1] = guildprefixes[msg.Channel.Guild.Id.ToString()];
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