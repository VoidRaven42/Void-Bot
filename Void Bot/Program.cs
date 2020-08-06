using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;

namespace Void_Bot
{
    public class Program
    {
        public static DiscordClient discord;

        private static CommandsNextExtension commands;

        private static readonly string token = File.ReadAllText("token.txt");

        private static InteractivityExtension interactivity;

        public static bool customstatus = false;

        private static readonly List<string> bannedwords = new List<string>
        {
            "aidan  not cute", "aidan not cute", "a idan not cute", "aidan no t cute", "aiidan not cute",
            "aaiidaan not cuute", "aidan ulge", "aidan not cu te", "aidan ugle", "aidan n o t cute",
            "aidan is not cute", "aidanugly", "aidan ugly", "ai dan not cute", "aidan is uncute", "aidann not cute",
            "aidan is ugle", "a i d a n  n o t  c u t e"
        };

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
                throw;
            }
        }

        private static async Task MainAsync(string[] args)
        {
            discord = new DiscordClient(new DiscordConfiguration
            {
                Token = token,
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true,
                LogLevel = LogLevel.Info
            });

            var icfg = new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromSeconds(20.0)
            };
            interactivity = discord.UseInteractivity(icfg);
            discord.GuildMemberAdded += Discord_GuildMemberAdded;
            commands = discord.UseCommandsNext(new CommandsNextConfiguration
            {
                PrefixResolver = ResolvePrefix,
                EnableDms = false,
                EnableMentionPrefix = false
            });
            commands.RegisterCommands<Commands>();
            commands.RegisterCommands<UtilityCommands>();
            commands.RegisterCommands<AdministrationCommands>();
            commands.RegisterCommands<FunCommands>();
            commands.RegisterCommands<ExternalCommands>();
            commands.CommandErrored += Commands_CommandErrored;
            commands.CommandExecuted += Commands_CommandExecuted;
            discord.MessageCreated += Discord_MessageCreated;
            await discord.ConnectAsync();
            await Task.Delay(2000);
            while (true)
            {
                if (!customstatus)
                {
                    var amount = 0;

                    foreach (var elem in discord.Guilds) amount += elem.Value.Members.Count;

                    var status = amount + " users";
                    var activity = new DiscordActivity(status, ActivityType.Watching);
                    await discord.UpdateStatusAsync(activity);
                }

                await Task.Delay(TimeSpan.FromMinutes(1));

                if (!customstatus)
                {
                    var amount = discord.Guilds.Count;

                    var status = amount + " servers";
                    var activity = new DiscordActivity(status, ActivityType.Watching);
                    await discord.UpdateStatusAsync(activity);
                }

                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        private static async Task Discord_MessageCreated(MessageCreateEventArgs e)
        {
            if (!(e.Guild == null) && e.Guild.Id.ToString() == "642067509931147264")
            {
                foreach (var elem in bannedwords)
                {
                    if (!e.Message.Content.ToLower().Contains(elem)) continue;
                    await e.Message.DeleteAsync();
                }

                if (e.Author.Id.Equals(291665243992752141) && Settings.Default.IsHarisATwat)
                    await e.Message.DeleteAsync();
            }

            if (!(e.Guild == null) &&
                (e.Author.Id.Equals(291665243992752141) || e.Author.Id.Equals(264462171528757250)) &&
                e.Guild.Id.ToString() == "302869055746998275" &&
                Settings.Default.IsHarisATwat) await e.Message.DeleteAsync();
        }

        private static async Task Commands_CommandErrored(CommandErrorEventArgs e)
        {
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "Void Bot",
                string.Format("User '{0}#{1}' ({2}) tried to execute '{3}' ", e.Context.User.Username,
                    e.Context.User.Discriminator, e.Context.User.Id, e.Command?.QualifiedName ?? "<unknown command>") +
                $"in #{e.Context.Channel.Name} ({e.Context.Channel.Id}) in {e.Context.Guild.Name} ({e.Context.Guild.Id}) and failed with {e.Exception.GetType()}: {e.Exception.Message}",
                DateTime.Now);
            DiscordEmbedBuilder embed = null;
            var ex = e.Exception;
            while (ex is AggregateException) ex = ex.InnerException;
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
                await e.Context.RespondAsync("Incorrect usage of command.\nCommand Help:");
                await new CommandsNextExtension.DefaultHelpModule().DefaultHelpAsync(e.Context, e.Command.Name);
            }

            if (embed != null) await e.Context.RespondAsync("", false, embed.Build());
        }

        private static async Task Commands_CommandExecuted(CommandExecutionEventArgs e)
        {
            discord.DebugLogger.LogMessage(LogLevel.Info, "Void Bot",
                $"{e.Context.User.Username} executed '{e.Command.QualifiedName}' in {e.Context.Channel.Name} ({e.Context.Channel.Id}) in {e.Context.Guild.Name} ({e.Context.Guild.Id}).",
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
            await e.Guild.SystemChannel.SendMessageAsync(null, false, Embed);
        }

        //private static async Task 
        private static void OnProcessExit(object sender, EventArgs e)
        {
            Console.WriteLine("Disconnecting, then exiting in 2 seconds");
            discord.DisconnectAsync();
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

        public static async Task shutdown(CommandContext ctx, string text)
        {
            if (text.ToLower() == "jasper'ssexdrive" || text.ToLower() == "jasperssexdrive")
                await ctx.RespondAsync("Sex drive removed!");
        }

        public static async Task sexdrive(CommandContext ctx)
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = "Sex drive restored!",
                Color = new DiscordColor(16711680)
            };
            var msg = await ctx.RespondAsync(null, false, embed.Build());
            for (var i = 0; i < 10; i++)
            {
                Thread.Sleep(1000);
                embed.Color = new DiscordColor(16753920);
                await msg.ModifyAsync(default, embed.Build());
                Thread.Sleep(1000);
                embed.Color = new DiscordColor(16776960);
                await msg.ModifyAsync(default, embed.Build());
                Thread.Sleep(1000);
                embed.Color = new DiscordColor(65280);
                await msg.ModifyAsync(default, embed.Build());
                Thread.Sleep(1000);
                embed.Color = new DiscordColor(255);
                await msg.ModifyAsync(default, embed.Build());
                Thread.Sleep(1000);
                embed.Color = new DiscordColor(6815999);
                await msg.ModifyAsync(default, embed.Build());
                Thread.Sleep(1000);
                embed.Color = new DiscordColor(11927807);
                await msg.ModifyAsync(default, embed.Build());
                Thread.Sleep(1000);
                embed.Color = new DiscordColor(16711680);
                await msg.ModifyAsync(default, embed.Build());
            }
        }
    }
}