using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Void_Bot
{
    [Group("admin")]
    [Aliases("a")]
    [Description("Commands for moderating servers")]
    public class AdministrationCommands : BaseCommandModule
    {
        [Command("eval")]
        [Hidden]
        public async Task Eval(CommandContext ctx, [RemainingText] [Description("Code to evaluate.")]
            string code)
        {
            if (ctx.User.Id == 379708744843395073L)
            {
                code = code.Trim('`');
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Evaluating...",
                    Color = new DiscordColor(13668786)
                };
                var msg = await ctx.RespondAsync("", false, embed.Build());
                var sopts = ScriptOptions.Default.AddImports("System", "System.Collections.Generic",
                    "System.Diagnostics", "System.IO",
                    "System.Linq", "System.Net.Http", "System.Net.Http.Headers", "System.Reflection", "System.Text",
                    "System.Threading", "System.Threading.Tasks", "DSharpPlus", "DSharpPlus.CommandsNext",
                    "DSharpPlus.Entities", "DSharpPlus.EventArgs", "DSharpPlus.Exceptions", "Void_Bot").AddReferences(
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
                    if (ctx.Guild == null)
                    {
                        css = await cs.RunAsync(new Globals
                        {
                            ctx = ctx,
                            user = ctx.User,
                            client = ctx.Client,
                            member = ctx.Member,
                            channel = ctx.Channel
                        });
                        rex = css.Exception;
                    }
                    else
                    {
                        css = await cs.RunAsync(new Globals
                        {
                            ctx = ctx,
                            user = ctx.User,
                            client = ctx.Client,
                            rest = Program.discordrest,
                            guild = ctx.Guild,
                            member = ctx.Member,
                            channel = ctx.Channel
                        });
                        rex = css.Exception;
                    }
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
                    if (css.ReturnValue != null)
                        embed.AddField("Return type", css.ReturnValue.GetType().ToString(), true);
                    await msg.ModifyAsync(default, embed.Build());
                }
            }
            else
            {
                await ctx.RespondAsync("Restricted command");
            }
        }

        [Command("setprefix")]
        [Aliases("channelprefix")]
        [Description("Sets custom command prefix for current guild. The bot will still respond to the default one.")]
        [RequirePermissions(Permissions.ManageGuild)]
        public async Task SetPrefixAsync(CommandContext ctx, [Description("The prefix to use for current guild.")]
            string prefix)
        {
            if (ctx.Guild.Equals(null))
            {
                await ctx.RespondAsync("You can't change the prefix of a DM channel!");
                return;
            }

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
        [Description("Clears messages, up to 214748347")]
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
            if (amount <= 100)
            {
                var messages = await ctx.Channel.GetMessagesAsync(amount);
                await ctx.Channel.DeleteMessagesAsync(messages);
            }
            else
            {
                await ctx.RespondAsync(
                    $"Are you sure you want to delete {amount} messages? (Deletion will take around {Math.Floor(Convert.ToDouble(amount) / 100) * 5} seconds)\nConfirm by responding \"y\" or \"n\".");
                var interactivity = ctx.Client.GetInteractivity();
                var responsemsg = await interactivity.WaitForMessageAsync(
                    xm => xm.Author == ctx.User &&
                          (xm.Content.ToLower().Contains("y") || xm.Content.ToLower().Contains("n")),
                    TimeSpan.FromSeconds(20.0));
                if (responsemsg.Result == null)
                {
                    await ctx.RespondAsync("Response timed out");
                    return;
                }

                if (responsemsg.Result.Content.ToLower() == "n")
                {
                    await ctx.RespondAsync("Deletion cancelled.");
                    return;
                }

                amount += 2;
                var total = 0;
                IReadOnlyList<DiscordMessage> msgs = new List<DiscordMessage>();

                try
                {
                    for (var i = 0; i < (amount - amount % 100) / 100 + 1; i++)
                    {
                        if (i == (amount - amount % 100) / 100)
                        {
                            msgs = await ctx.Channel.GetMessagesAsync(amount % 100);
                            if (msgs.Count == 0) break;
                            total += msgs.Count;
                            await ctx.Channel.DeleteMessagesAsync(msgs);
                        }
                        else
                        {
                            msgs = await ctx.Channel.GetMessagesAsync();
                            if (msgs.Count == 0) break;
                            total += msgs.Count;
                            await ctx.Channel.DeleteMessagesAsync(msgs);
                        }

                        await Task.Delay(5000);
                    }

                    var msg = await ctx.RespondAsync($"{total} messages deleted!");
                }
                catch (Exception)
                {
                    await ctx.RespondAsync("Cannot delete messages older than two weeks!");
                }
            }
        }

        [Command("send")]
        [Aliases("sendmessage", "botsend")]
        [Description("Sends a normal message in a channel, can be used for announcements")]
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

                        Program.CustomStatus = false;
                        await Program.discord.UpdateStatusAsync();
                        return;
                }

                if (status == null)
                {
                    await ctx.RespondAsync("Please enter a valid activity and status");
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

                Program.CustomStatus = true;
                await Program.discord.UpdateStatusAsync(activity);
                await ctx.RespondAsync("Status has been successfully updated");
            }
            else
            {
                await ctx.RespondAsync("Restricted to bot creator");
            }
        }

        /*[Command("embed")]
        [Aliases("sendembed")]
        [RequirePermissions(Permissions.ManageMessages)]*/
        public async Task Embed(CommandContext ctx, [RemainingText] string data)
        {
        }

        [Command("move")]
        public async Task Move(CommandContext ctx, DiscordChannel from, DiscordChannel to)
        {
            if (ctx.User.Id == 379708744843395073L)
            {
                var whs = await to.GetWebhooksAsync();
                var name = "Void Bot";
                var msgs = await from.GetMessagesAsync(10000);

                if (whs.Count(x => x.Name == name) == 0)
                {
                    await to.CreateWebhookAsync(name);
                }

                var wh = whs.SingleOrDefault(w => string.Equals(w.Name, name, StringComparison.CurrentCultureIgnoreCase));

                foreach (var msg in msgs.Reverse())
                {
                    var whbuilder = new DiscordWebhookBuilder
                    {
                        AvatarUrl = msg.Author.AvatarUrl,
                        Username = msg.Author.Username
                    };
                    whbuilder.WithContent(msg.Attachments.Count == 0 ? msg.Content : msg.Attachments[0].Url);
                    
                    await wh.ExecuteAsync(whbuilder);

                    await Task.Delay(3000);
                }
            }
            else
            {
                await ctx.RespondAsync("Restricted command");
            }
        }

        public class Globals
        {
            public DiscordChannel channel;

            public DiscordClient client;

            public CommandContext ctx;

            public DiscordGuild guild;

            public DiscordMember member;

            public DiscordRestClient rest;

            public DiscordUser user;
        }
    }
}