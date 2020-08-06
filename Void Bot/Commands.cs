using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

//This file is here to allow easier use of commands, while allowing the help command to remain organised

namespace Void_Bot
{
    class Commands : BaseCommandModule
    {
        [Command("math")]
        [Aliases("maths")]
        [Description("Does simple math, (+, -, *, /), called with \"math (first num.) (operation) (second num.)\"")]
        [Hidden]
        public async Task Add(CommandContext ctx, [RemainingText] string problem)
        {
            if (problem == null)
            {
                await ctx.RespondAsync("Command Help:");
                await new CommandsNextExtension.DefaultHelpModule().DefaultHelpAsync(ctx, ctx.Command.Name);
                return;
            }

            string type;
            int one;
            int two;
            if (problem.Contains('+'))
            {
                var array = problem.Split('+');
                type = "+";
                one = int.Parse(array[0]);
                two = int.Parse(array[1]);
            }
            else if (problem.Contains('-'))
            {
                var array2 = problem.Split('-');
                type = "-";
                one = int.Parse(array2[0]);
                two = int.Parse(array2[1]);
            }
            else if (problem.Contains('*'))
            {
                var array3 = problem.Split('*');
                type = "*";
                one = int.Parse(array3[0]);
                two = int.Parse(array3[1]);
            }
            else
            {
                if (!problem.Contains('/')) throw new ArgumentException();
                var array4 = problem.Split('/');
                type = "/";
                one = int.Parse(array4[0]);
                two = int.Parse(array4[1]);
            }

            switch (type)
            {
                case "+":
                    await ctx.RespondAsync($"Result: {one + two}");
                    break;
                case "-":
                    await ctx.RespondAsync($"Result: {one - two}");
                    break;
                case "*":
                    await ctx.RespondAsync($"Result: {one * two}");
                    break;
                case "/":
                    await ctx.RespondAsync($"Result: {one / two}");
                    break;
            }
        }


        [Command("avatar")]
        [Aliases("pfp")]
        [Description("Takes the username or id and returns the pfp in high quality.")]
        [Hidden]
        public async Task Avatar(CommandContext ctx, DiscordMember usr)
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = "User Avatar",
                Color = DiscordColor.Magenta,
                ImageUrl = usr.AvatarUrl
            };
            await ctx.RespondAsync(embed: embed);
        }

        [Command("eval")]
        [Hidden]
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
                var sopts = ScriptOptions.Default.AddImports("System", "System.Collections.Generic",
                    "System.Diagnostics", "System.IO",
                    "System.Linq", "System.Net.Http", "System.Net.Http.Headers", "System.Reflection", "System.Text",
                    "System.Threading", "System.Threading.Tasks", "DSharpPlus", "DSharpPlus.CommandsNext",
                    "DSharpPlus.Entities", "DSharpPlus.EventArgs", "DSharpPlus.Exceptions", "Void_Bot").AddReferences(
                    from xa in AppDomain.CurrentDomain.GetAssemblies()
                    where !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)
                    select xa).WithAllowUnsafe(true);
                var sw1 = Stopwatch.StartNew();
                var cs = CSharpScript.Create(code, sopts, typeof(AdministrationCommands.Globals));
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
                    css = await cs.RunAsync(new AdministrationCommands.Globals
                    {
                        context = ctx,
                        user = ctx.User,
                        client = Program.discord,
                        guild = ctx.Guild,
                        member = ctx.Member,
                        channel = ctx.Channel
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
        [Hidden]
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
        [Description("Clears messages, up to 100 at a time")]
        [RequirePermissions(Permissions.ManageMessages)]
        [Hidden]
        public async Task PurgeAsync(CommandContext ctx, [Description("The amount of messages to purge")]
            int amount)
        {
            if (amount <= 0)
            {
                await ctx.RespondAsync("Number purged cannot be negative");
                return;
            }

            await ctx.Channel.DeleteMessageAsync(ctx.Message);
            if ((amount - amount % 100) / 100 <= 1)
            {
                var messages = await ctx.Channel.GetMessagesAsync(amount);
                await ctx.Channel.DeleteMessagesAsync(messages);
            }
            else
            {
                await ctx.RespondAsync("Cannot purge more than 100 messages!");
                //for (var i = 0; i < (amount - amount % 100) / 100; i++)
                //{
                //    if ()
                //}
            }

            var obj = await ctx.Channel.SendMessageAsync($"{amount} messages have been deleted!");
            Thread.Sleep(1000);
            await obj.DeleteAsync();
        }

        [Command("send")]
        [Aliases("sendmessage", "botsend")]
        [Description("Sends a normal message in a channel, can be used for announcements")]
        [RequirePermissions(Permissions.ManageMessages)]
        [Hidden]
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

                        Program.customstatus = false;
                        await Program.discord.UpdateStatusAsync();
                        return;
                }

                if (status == null)
                {
                    await ctx.RespondAsync("Please enter a valid activity and status");
                    return;
                }

                if (status.ToLower() == "users")
                {
                    var amount = 0;
                    var client = Program.discord;
                    foreach (var elem in client.Guilds) amount += elem.Value.Members.Count;

                    status = amount + " users";
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

                Program.customstatus = true;
                await Program.discord.UpdateStatusAsync(activity);
                await ctx.RespondAsync("Status has been successfully updated");
            }
            else
            {
                await ctx.RespondAsync("Restricted to bot creator");
            }
        }

        [Command("shutupnomiki")]
        [RequirePermissions(Permissions.ManageMessages)]
        [Hidden]
        public async Task Shutup(CommandContext ctx)
        {
            if (!ctx.Guild.Members.ContainsKey(291665243992752141) &&
                !ctx.Guild.Members.ContainsKey(264462171528757250))
            {
                await ctx.RespondAsync("The nomiki are not here");
                return;
            }

            Settings.Default.IsHarisATwat = true;
            Settings.Default.Save();
            await ctx.RespondAsync("The nomiki are is now being suppressed");
        }

        [Command("unshutupnomiki")]
        [RequirePermissions(Permissions.ManageMessages)]
        [Hidden]
        public async Task UnShutup(CommandContext ctx)
        {
            if (!ctx.Guild.Members.ContainsKey(291665243992752141) &&
                !ctx.Guild.Members.ContainsKey(264462171528757250))
            {
                await ctx.RespondAsync("The nomiki are not here");
                return;
            }

            Settings.Default.IsHarisATwat = false;
            Settings.Default.Save();
            await ctx.RespondAsync("The nomiki are no longer being suppressed");
        }

        /*[Command("embed")]
        [Aliases("sendembed")]
        [RequirePermissions(Permissions.ManageMessages)]
        [Hidden]*/
        public async Task Embed(CommandContext ctx, [RemainingText] string data)
        {
        }

        public async Task Test(CommandContext ctx)
        {
        }

        [Command("mathduel")]
        [Aliases("duel", "mathsduel")]
        [Description("Challenges another user to a math duel!")]
        [Hidden]
        public async Task Duel(CommandContext ctx, [RemainingText] string userintake)
        {
            if (userintake == null)
            {
                await ctx.RespondAsync("You must specify a user to duel!");
                return;
            }

            if (userintake.Contains(' ') || userintake.Contains(','))
            {
                await ctx.RespondAsync("You cannot challenge multiple users!");
                return;
            }

            if (userintake.StartsWith("<@&"))
            {
                await ctx.RespondAsync("You can't duel a role!");
                return;
            }

            var usertrim = userintake.Trim('<', '>', '@', '!');
            var user = ctx.User;
            if (usertrim.All(char.IsDigit))
            {
                user = await Program.discord.GetUserAsync(ulong.Parse(usertrim));

                var interactivity = ctx.Client.GetInteractivity();
                var random = new Random();
                decimal arg1 = random.Next(5, 20);
                decimal arg2 = random.Next(5, 20);
                var ops = new string[4]
                {
                    "+",
                    "-",
                    "*",
                    "/"
                };
                var op = ops[random.Next(ops.Length)];
                var result = 0.0m;
                if (op != null)
                    switch (op)
                    {
                        case "+":
                            result = arg1 + arg2;
                            break;
                        case "-":
                            result = arg1 - arg2;
                            break;
                        case "*":
                            result = arg1 * arg2;
                            break;
                        case "/":
                            result = arg1 / arg2;
                            break;
                    }

                result = decimal.Round(result, 1);
                if (ctx.User == user)
                {
                    await ctx.RespondAsync("You can't challenge yourself to a duel!");
                    return;
                }

                if (user.IsBot)
                {
                    await ctx.RespondAsync("You can't challenge a bot to a duel!");
                    return;
                }

                await ctx.RespondAsync(ctx.User.Mention + " has challenged " + user.Mention +
                                       " to a math duel! They have 20 seconds to accept or decline by typing \"accept\" or \"decline\".");
                var msg = await interactivity.WaitForMessageAsync(
                    xm => xm.Author == user &&
                          (xm.Content.ToLower().Contains("accept") || xm.Content.ToLower().Contains("decline")),
                    TimeSpan.FromSeconds(20.0));
                if (msg.Result == null)
                {
                    await ctx.RespondAsync("User did not accept or decline");
                }
                else if (msg.Result.Content.ToLower() == "accept")
                {
                    await ctx.RespondAsync(
                        "The challenged user has accepted! The challenge will be sent in 5 seconds, and both competitors will have 15 seconds to answer, whoever does first will win! (If a decimal, give to 1 decimal place)");
                    Thread.Sleep(5000);
                    await ctx.RespondAsync($"The problem is `{arg1}{op}{arg2}`");
                    msg = await interactivity.WaitForMessageAsync(
                        xm => xm.Content.Contains(result.ToString()) && (xm.Author == ctx.User || xm.Author == user),
                        TimeSpan.FromSeconds(15.0));
                    if (!msg.TimedOut)
                        await ctx.RespondAsync(msg.Result.Author.Mention +
                                               " has successfully answered the question! They win!");
                    else
                        await ctx.RespondAsync($"Nobody answered in time! The answer was `{result}`");
                }
                else if (msg.Result.Content.ToLower() == "decline")
                {
                    await ctx.RespondAsync("The challenged user declined the duel");
                }
            }
            else
            {
                await ctx.RespondAsync("You must @ the user you wish to duel");
            }
        }

        [Command("speen")]
        [Aliases("spin")]
        [Description("SPEEN")]
        [Hidden]
        public async Task Speen(CommandContext ctx)
        {
            await ctx.RespondWithFileAsync("speen.mp4");
        }

        [Command("gun")]
        [Aliases("gunsong")]
        [Description("gun.")]
        [Hidden]
        public async Task Gun(CommandContext ctx)
        {
            await ctx.RespondWithFileAsync("gun.mp4");
        }

        [Command("no")]
        [Description("no.")]
        [Hidden]
        public async Task No(CommandContext ctx)
        {
            await ctx.RespondWithFileAsync("no.mp4");
        }

        [Command("loaf")]
        [Aliases("bunny")]
        [Description("sends BUNNI!")]
        [Hidden]
        public async Task Loaf(CommandContext ctx)
        {
            await ctx.RespondAsync(
                "https://media.discordapp.net/attachments/670925998434418688/734602804362084455/image0.gif");
        }

        [Command("knuckles")]
        [Description("you don't want to use this command")]
        [Hidden]
        public async Task Knuckles(CommandContext ctx)
        {
            await ctx.RespondAsync("https://cdn.discordapp.com/emojis/595433217440350248.gif");
        }

        [Command("horny")]
        [Aliases("bonk")]
        [Description("bonk.")]
        [Hidden]
        public async Task Horny(CommandContext ctx)
        {
            await ctx.RespondAsync("https://tenor.com/view/horny-jail-bonk-dog-hit-head-stop-being-horny-gif-17298755");
        }

        [Command("gitgud")]
        [Description("git gud!")]
        [Hidden]
        public async Task Gitgud(CommandContext ctx)
        {
            await ctx.RespondAsync(
                "https://cdn.discordapp.com/attachments/681973069333659654/740325342438359091/image0.png");
        }

        [Command("turnofflifesupport")]
        [Description("rip grandma")]
        [Hidden]
        public async Task LifeSupport(CommandContext ctx)
        {
            await ctx.RespondAsync("Grandma has been terminated.");
        }

        [Command("nonce")]
        [Description("nonce!")]
        [Hidden]
        public async Task Nonce(CommandContext ctx)
        {
            await ctx.RespondAsync(
                "https://tenor.com/view/nonce-pedo-loop-itz_cam-gif-16228867");
        }
    }
}
