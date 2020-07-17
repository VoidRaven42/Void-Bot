// Void_Bot.AdministrationCommands

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Void_Bot
{
    public class AdministrationCommands : BaseCommandModule
    {
        [Command("eval")]
        public async Task Eval(CommandContext ctx, [RemainingText] [Description("Code to evaluate.")]
            string code)
        {
            if (ctx.User.Id == 379708744843395073L || ctx.User.Id == 227672468695810049L)
            {
                code = code.Trim('`');
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Evaluating...",
                    Color = new DiscordColor(13668786)
                };
                var msg = await ctx.RespondAsync("", false, embed.Build());
                var sopts = ScriptOptions.Default.AddImports("System", "System.Collections.Generic", "System.Diagnostics",
                    "System.Linq", "System.Net.Http", "System.Net.Http.Headers", "System.Reflection", "System.Text",
                    "System.Threading", "System.Threading.Tasks", "DSharpPlus", "DSharpPlus.CommandsNext",
                    "DSharpPlus.Entities", "DSharpPlus.EventArgs", "DSharpPlus.Exceptions", "Void_Bot", "Void_Bot_Recovered").AddReferences(
                    from xa in AppDomain.CurrentDomain.GetAssemblies()
                    where !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)
                    select xa).WithAllowUnsafe(true);
                var sw1 = Stopwatch.StartNew();
                var cs = CSharpScript.Create(code, sopts, typeof(Globals));
                var csc = cs.Compile();
                sw1.Stop();
                if (csc.Any(xd => xd.Severity == DiagnosticSeverity.Error))
                {
                    var discordEmbedBuilder = new DiscordEmbedBuilder();
                    discordEmbedBuilder.Title = "Compilation failed";
                    discordEmbedBuilder.Description = "Compilation failed after " +
                                                      sw1.ElapsedMilliseconds.ToString("#,##0") + "ms with " +
                                                      csc.Length.ToString("#,##0") + " errors.";
                    discordEmbedBuilder.Color = new DiscordColor(16711680);
                    embed = discordEmbedBuilder;
                    foreach (var xd2 in csc.Take(3))
                    {
                        var ls = xd2.Location.GetLineSpan();
                        embed.AddField(
                            "Error at " + ls.StartLinePosition.Line.ToString("#,##0") + ", " +
                            ls.StartLinePosition.Character.ToString("#,##0"), Formatter.InlineCode(xd2.GetMessage()));
                    }

                    if (csc.Length > 3)
                        embed.AddField("Some errors omitted",
                            (csc.Length - 3).ToString("#,##0") + " more errors not displayed");
                    await msg.ModifyAsync(default, embed.Build());
                    return;
                }

                ScriptState<object> css = null;
                var sw2 = Stopwatch.StartNew();
                Exception rex;
                try
                {
                    css = await cs.RunAsync(new Globals
                    {
                        context = ctx,
                        user = ctx.User,
                        client = Program.discord,
                        guild = ctx.Guild,
                        member = ctx.Member
                    });
                    rex = css.Exception;
                }
                catch (Exception ex)
                {
                    rex = ex;
                }

                sw2.Stop();
                if (rex != null)
                {
                    var discordEmbedBuilder = new DiscordEmbedBuilder();
                    discordEmbedBuilder.Title = "Execution failed";
                    discordEmbedBuilder.Description = string.Concat("Execution failed after ",
                        sw2.ElapsedMilliseconds.ToString("#,##0"), "ms with `", rex.GetType(), ": ", rex.Message, "`.");
                    discordEmbedBuilder.Color = new DiscordColor(16711680);
                    embed = discordEmbedBuilder;
                    await msg.ModifyAsync(default, embed.Build());
                }
                else
                {
                    embed = new DiscordEmbedBuilder
                    {
                        Title = "Evaluation successful",
                        Color = new DiscordColor(65280)
                    };
                    embed.AddField("Result", css.ReturnValue != null ? css.ReturnValue.ToString() : "No value returned")
                        .AddField("Compilation time", sw1.ElapsedMilliseconds.ToString("#,##0") + "ms", true)
                        .AddField("Execution time", sw2.ElapsedMilliseconds.ToString("#,##0") + "ms", true);
                    if (css.ReturnValue != null) embed.AddField("Return type", css.ReturnValue.GetType().ToString(), true);
                    await msg.ModifyAsync(default, embed.Build());
                }
            }
            else
            {
                await ctx.RespondAsync("Restricted to bot creator");
            }
        }

        [Command("setprefix")]
        [Aliases("channelprefix")]
        [Description("Sets custom command prefix for current guild. The bot will still respond to the default one.")]
        [RequirePermissions(Permissions.ManageGuild)]
        public async Task SetPrefixAsync(CommandContext ctx, [Description("The prefix to use for current guild.")]
            string prefix)
        {
            var array = File.ReadAllLines("prefixes.txt").ToArray();
            var guildprefixes = new Dictionary<string, string>();
            var array2 = array;
            for (var i = 0; i < array2.Length; i++)
            {
                var split = array2[i].Split(':');
                guildprefixes.Add(split[0], split[1]);
            }

            if (guildprefixes.ContainsKey(ctx.Guild.Id.ToString()))
            {
                var toremove = ctx.Guild.Id + ":" + guildprefixes[ctx.Guild.Id.ToString()];
                File.WriteAllLines("prefixes.txt", (from l in File.ReadLines("prefixes.txt")
                    where l != toremove
                    select l).ToList());
            }

            await File.AppendAllTextAsync("prefixes.txt", ctx.Guild.Id + ":" + prefix + Environment.NewLine);
            await ctx.RespondAsync("Changed to " + prefix);
        }

        [Command("purge")]
        [Aliases("clear")]
        [RequirePermissions(Permissions.ManageMessages)]
        public async Task PurgeAsync(CommandContext ctx, [Description("The amount of messages to purge")]
            int amount)
        {
            if (amount <= 0)
            {
                await ctx.RespondAsync("Number purged cannot be negative");
                return;
            }

            await ctx.Channel.DeleteMessageAsync(ctx.Message);
            if ((amount - amount % 100) / 100 < 1)
            {
                var messages = await ctx.Channel.GetMessagesAsync(amount);
                await ctx.Channel.DeleteMessagesAsync(messages);
            }
            else
            {
                for (var i = 0; i < (amount - amount % 100) / 100; i++)
                {
                    var messages = await ctx.Channel.GetMessagesAsync();
                    if (messages.Count < 100) break;
                    await ctx.Channel.DeleteMessagesAsync(messages);
                    Thread.Sleep(5000);
                    messages = await ctx.Channel.GetMessagesAsync();
                    await ctx.Channel.DeleteMessagesAsync(messages);
                }
            }

            var obj = await ctx.Channel.SendMessageAsync($"{amount} messages have been deleted!");
            Thread.Sleep(1000);
            await obj.DeleteAsync();
        }

        [Command("send")]
        [Aliases("sendmessage", "botsend")]
        [RequirePermissions(Permissions.ManageMessages)]
        public async Task SendAsBot(CommandContext ctx, DiscordChannel channel, [RemainingText] string messagetosend)
        {
            if (channel.Guild.Id != ctx.Guild.Id)
            {
                await ctx.RespondAsync("You cannot send a message in another server");
            }
            else if (messagetosend == null)
            {
                await ctx.RespondAsync("Command Help:");
                await new CommandsNextExtension.DefaultHelpModule().DefaultHelpAsync(ctx, ctx.Command.Name);
            }
            else
            {
                await channel.SendMessageAsync(messagetosend);
                await ctx.RespondAsync($"Message sent in {channel}");
            }
        }

        [Command("setbotstatus")]
        [Aliases("status", "setstatus")]
        [Hidden]
        public async Task Status(CommandContext ctx, string act, [RemainingText] string status)
        {
            if (ctx.User.Id == 379708744843395073L)
            {
                switch (act)
                {
                    case "":
                    case "remove":
                    case "none":
                        await Program.discord.UpdateStatusAsync();
                        return;
                }

                new DiscordActivity();
                DiscordActivity activity;
                switch (act.ToLower())
                {
                    case "playing":
                        activity = new DiscordActivity(status, ActivityType.Playing);
                        break;
                    case "listening":
                        activity = new DiscordActivity(status, ActivityType.ListeningTo);
                        break;
                    case "listeningto":
                        activity = new DiscordActivity(status, ActivityType.ListeningTo);
                        break;
                    case "watching":
                        activity = new DiscordActivity(status, ActivityType.Watching);
                        break;
                    case "streaming":
                        activity = new DiscordActivity(status, ActivityType.Streaming);
                        break;
                    default:
                        await ctx.RespondAsync("Invalid activity specified");
                        return;
                }

                await Program.discord.UpdateStatusAsync(activity);
            }
            else
            {
                await ctx.RespondAsync("Restricted to bot creator");
            }
        }

        [Command("shutupharis")]
        [RequirePermissions(Permissions.ManageMessages)]
        public async Task Shutup(CommandContext ctx)
        {
            Settings.Default.IsHarisATwat = true;
            Settings.Default.Save();
            await ctx.RespondAsync("Haris is now being suppressed");
        }

        [Command("unshutupharis")]
        [RequirePermissions(Permissions.ManageMessages)]
        public async Task UnShutup(CommandContext ctx)
        {
            Settings.Default.IsHarisATwat = false;
            Settings.Default.Save();
            await ctx.RespondAsync("Haris is no longer being suppressed");
        }
        public class Globals
        {
            public DiscordClient client;

            public CommandContext context;

            public DiscordGuild guild;

            public DiscordMember member;

            public DiscordUser user;
        }
    }
}