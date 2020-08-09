using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Reddit;
using Reddit.Controllers;

//This file is here to allow easier use of commands, while allowing the help command to remain organised

namespace Void_Bot
{
    internal class Commands : BaseCommandModule
    {
        private readonly ExternalCommands ex = new ExternalCommands();

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
            BigInteger one;
            BigInteger two;
            if (problem.Contains('+'))
            {
                var array = problem.Split('+');
                type = "+";
                one = BigInteger.Parse(array[0]);
                two = BigInteger.Parse(array[1]);
            }
            else if (problem.Contains('-'))
            {
                var array = problem.Split('-');
                type = "-";
                one = BigInteger.Parse(array[0]);
                two = BigInteger.Parse(array[1]);
            }
            else if (problem.Contains('*'))
            {
                var array = problem.Split('*');
                type = "*";
                one = BigInteger.Parse(array[0]);
                two = BigInteger.Parse(array[1]);
            }
            else if (problem.Contains('%'))
            {
                var array = problem.Split('%');
                type = "%";
                one = BigInteger.Parse(array[0]);
                two = BigInteger.Parse(array[1]);
            }
            else
            {
                if (!problem.Contains('/')) throw new ArgumentException();
                var array = problem.Split('/');
                type = "/";
                one = BigInteger.Parse(array[0]);
                two = BigInteger.Parse(array[1]);
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
                case "%":
                    await ctx.RespondAsync($"Result: {one % two}");
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

        [Command("reddit")]
        [Aliases("rd")]
        [Description("Gets one of the top 100 hot posts on the specified subreddit")]
        [Hidden]
        public async Task Reddit(CommandContext ctx, string subreddit)
        {
            var random = new Random();
            var rtoken = File.ReadAllText("reddittoken.txt");
            var refresh = File.ReadAllText("refresh.txt");
            var reddit = new RedditClient("Kb6WAOupj1iW1Q", appSecret: rtoken, refreshToken: refresh);
            Subreddit sub = null;
            var embed = new DiscordEmbedBuilder
            {
                Title = "Retrieving Post",
                Color = DiscordColor.Yellow
            };
            var msg = await ctx.RespondAsync(embed: embed.Build());
            try
            {
                sub = reddit.Subreddit(subreddit).About();
            }
            catch
            {
                await ctx.RespondAsync("Subreddit could not be found/accessed, please try another!");
                return;
            }

            var hot = sub.Posts.Hot;
            Post img = null;
            var allownsfw = ctx.Channel.IsNSFW;

            if (sub.Over18.Value && !ctx.Channel.IsNSFW)
            {
                await ctx.RespondAsync("This subreddit is marked NSFW, please try again in an NSFW channel.");
                return;
            }


            for (var i = 0; i < 1000; i++)
            {
                img = hot[random.Next(0, hot.Count - 1)];
                if (img.Listing.SelfText != "") break;
                if (img.Listing.URL.Contains("gifv") ||
                    !img.Listing.URL.Contains("i.redd.it") && !img.Listing.URL.Contains("i.imgur.com") ||
                    !img.Listing.URL.Contains("png")) continue;
                if (!img.Listing.Over18 || allownsfw && img.Listing.Over18) break;
            }

            if ((img.Listing.URL.Contains("gifv") ||
                 !img.Listing.URL.Contains("i.redd.it") && !img.Listing.URL.Contains("i.imgur.com") ||
                 !img.Listing.URL.Contains("png")) && img.Listing.SelfText == "")
            {
                await ctx.RespondAsync("No valid post could be found, please try again.");
                return;
            }


            if (img.Listing.SelfText != "")
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = img.Title + " [Textpost]",
                    Url = "https://www.reddit.com/" + img.Permalink,
                    Color = DiscordColor.Aquamarine
                };
                embed.AddField("Subreddit", "r/" + subreddit);
                embed.AddField("Link:", "https://www.reddit.com" + img.Permalink);
                await msg.ModifyAsync(embed: embed.Build());
                ex.RedditArray[ex.RedditElem] = img.Permalink;
                ex.RedditElem += 1;
                if (ex.RedditElem == 20) ex.RedditElem = 0;
            }
            else
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = img.Title,
                    Url = "https://www.reddit.com" + img.Permalink,
                    Color = DiscordColor.Aquamarine,
                    ImageUrl = img.Listing.URL
                };
                embed.AddField("Subreddit", "r/" + subreddit);
                await msg.ModifyAsync(embed: embed.Build());
                ex.RedditArray[ex.RedditElem] = img.Permalink;
                ex.RedditElem += 1;
                if (ex.RedditElem == 20) ex.RedditElem = 0;
            }
        }

        [Command("eyebleach")]
        [Aliases("eb")]
        [Description("Gets one of the top 100 hot posts on r/eyebleach")]
        [Hidden]
        public async Task Eyebleach(CommandContext ctx)
        {
            var random = new Random();
            var rtoken = File.ReadAllText("reddittoken.txt");
            var refresh = File.ReadAllText("refresh.txt");
            var reddit = new RedditClient("Kb6WAOupj1iW1Q", appSecret: rtoken, refreshToken: refresh);
            var sub = reddit.Subreddit("eyebleach").About();
            var hot = sub.Posts.Hot;
            Post img = null;
            var embed = new DiscordEmbedBuilder
            {
                Title = "Retrieving Post",
                Color = DiscordColor.Yellow
            };
            var msg = await ctx.RespondAsync(embed: embed.Build());
            for (var i = 0; i < 1000; i++)
            {
                img = hot[random.Next(0, 99)];
                if (!img.Listing.URL.Contains("gifv") && img.Listing.URL.Contains("i.redd.it")) break;
            }

            if (img.Listing.URL.Contains("gifv") || !img.Listing.URL.Contains("i.redd.it"))
            {
                await ctx.RespondAsync("No valid post could be found, please try again.");
                return;
            }

            embed = new DiscordEmbedBuilder
            {
                Title = "Post Retrieved",
                Color = DiscordColor.Aquamarine,
                ImageUrl = img.Listing.URL
            };
            await msg.ModifyAsync(embed: embed.Build());
            ex.EBArray[ex.EBElem] = img.Listing.URL;
            ex.EBElem += 1;
            if (ex.EBElem == 20) ex.EBElem = 0;
        }

        [Command("e621")]
        [Aliases("e6")]
        [Description("Gets one of the top 50 posts by score for the specified tags, or all posts if no tags specified")]
        [Hidden]
        public async Task E621(CommandContext ctx, [RemainingText] string tags)
        {
            if (!ctx.Channel.IsNSFW)
            {
                var message2 = await ctx.RespondAsync("Channel must be NSFW for this command");
                Thread.Sleep(1000);
                IReadOnlyList<DiscordMessage> messages2 = new DiscordMessage[2]
                {
                    ctx.Message,
                    message2
                };
                await ctx.Channel.DeleteMessagesAsync(messages2);
                return;
            }

            var Embed = new DiscordEmbedBuilder
            {
                Title = "Retrieving Post",
                Color = DiscordColor.Yellow
            };
            var msg = await ctx.RespondAsync(embed: Embed.Build());
            var tagssplit = new List<string>();
            if (tags != null) tagssplit = tags.Split(' ').ToList();
            var e = await ex.EHttpGet("https://e621.net/posts.json", tagssplit.ToArray());
            if (e == null)
            {
                Embed = new DiscordEmbedBuilder
                {
                    Title = "No results found!",
                    Color = DiscordColor.Red
                };
                await msg.ModifyAsync(embed: Embed.Build());
            }
            else
            {
                Embed = new DiscordEmbedBuilder
                {
                    Title = "Post Retrieved",
                    Color = DiscordColor.Aquamarine,
                    ImageUrl = e
                };
                Embed.AddField("Requested by:", ctx.User.Username + '#' + ctx.User.Discriminator);
                await msg.ModifyAsync(embed: Embed.Build());
                ex.E6Array[ex.E6Elem] = e;
                ex.E6Elem += 1;
                if (ex.E6Elem == 20) ex.E6Elem = 0;
            }
        }

        [Command("rule34")]
        [Aliases("r34")]
        [Hidden]
        public async Task R34(CommandContext ctx, [RemainingText] string tags)
        {
            if (!ctx.Channel.IsNSFW)
            {
                var message2 = await ctx.RespondAsync("Channel must be NSFW for this command");
                Thread.Sleep(1000);
                IReadOnlyList<DiscordMessage> messages2 = new DiscordMessage[2]
                {
                    ctx.Message,
                    message2
                };
                await ctx.Channel.DeleteMessagesAsync(messages2);
                return;
            }

            var Embed = new DiscordEmbedBuilder
            {
                Title = "Retrieving Post",
                Color = DiscordColor.Yellow
            };
            var msg = await ctx.RespondAsync(embed: Embed.Build());
            var tagssplit = new List<string>();
            if (tags != null) tagssplit = tags.Split(' ').ToList();
            var e = await ex.RHttpGet("https://r34-json-api.herokuapp.com/posts", tagssplit.ToArray());
            if (e == null)
            {
                Embed = new DiscordEmbedBuilder
                {
                    Title = "No results found!",
                    Color = DiscordColor.Red
                };
                await msg.ModifyAsync(embed: Embed.Build());
            }
            else
            {
                Embed = new DiscordEmbedBuilder
                {
                    Title = "Post Retrieved",
                    Color = DiscordColor.Aquamarine,
                    ImageUrl = e
                };
                Embed.AddField("Requested by:", ctx.User.Username + '#' + ctx.User.Discriminator);
                await msg.ModifyAsync(embed: Embed.Build());
                ex.R34Array[ex.R34Elem] = e;
                ex.R34Elem += 1;
                if (ex.R34Elem == 20) ex.R34Elem = 0;
            }
        }

        [Command("beemovie")]
        [Description("the whole bee movie.")]
        [Hidden]
        public async Task BeeMovie(CommandContext ctx)
        {
            await ctx.RespondWithFileAsync("beemovie.png");
        }

        //[Command("cputemp")]
        public async Task CPUTemp(CommandContext ctx)
        {
            double CPUtprt = 0;
            var mos = new ManagementObjectSearcher(@"root\WMI", "Select * From MSAcpi_ThermalZoneTemperature");
            foreach (ManagementObject mo in mos.Get())
                CPUtprt = Convert.ToDouble(
                    Convert.ToDouble(mo.GetPropertyValue("CurrentTemperature").ToString()) - 2732) / 10;

            await ctx.RespondAsync(CPUtprt.ToString());
        }

        [Command("join")]
        [Aliases("connect")]
        [Hidden]
        public async Task Join(CommandContext ctx)
        {
            var node = Program.LavalinkNode;

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You must be in a voice channel!");
                return;
            }

            await node.ConnectAsync(ctx.Member.VoiceState.Channel);
            await ctx.RespondAsync($"Joined {ctx.Member.VoiceState.Channel.Name}!").ConfigureAwait(false);
        }

        [Command("leave")]
        [Aliases("disconnect", "dc")]
        [Hidden]
        public async Task Leave(CommandContext ctx)
        {
            var node = Program.LavalinkNode;

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You must be in a voice channel!");
                return;
            }

            var conn = node.GetConnection(ctx.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("Not connected.");
                return;
            }

            if (ctx.Member.VoiceState.Channel != conn.Channel)
            {
                await ctx.RespondAsync("You must be in the same channel as the bot!");
                return;
            }

            foreach (var elem in AudioCommands.queues[ctx.Guild.Id]) AudioCommands.queues.Remove(ctx.Guild.Id);

            await conn.DisconnectAsync();
            await ctx.RespondAsync($"Left {ctx.Member.VoiceState.Channel.Name}!").ConfigureAwait(false);
        }


        [Command("aspeen")]
        [Hidden]
        public async Task Aspeen(CommandContext ctx)
        {
            await Play(ctx, "https://www.youtube.com/watch?v=cerkDJLuT_k");
        }

        [Command]
        [Hidden]
        public async Task Play(CommandContext ctx, [RemainingText] string search)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You must be in a voice channel!");
                return;
            }

            var node = Program.LavalinkNode;
            var conn = node.GetConnection(ctx.Member.VoiceState.Guild);

            if (conn == null) await Join(ctx);
            conn = node.GetConnection(ctx.Guild);
            var loadResult = await node.Rest.GetTracksAsync(search);

            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed ||
                loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await ctx.RespondAsync($"Track search failed for {search}.");
                await Leave(ctx);
                return;
            }

            var result = loadResult.Tracks.First();
            if (AudioCommands.queues.ContainsKey(ctx.Guild.Id))
            {
                if (AudioCommands.queues[ctx.Guild.Id].IsEmpty && conn.CurrentState.CurrentTrack == null)
                {
                    await conn.PlayAsync(result);
                }
                else
                {
                    AudioCommands.queues[ctx.Guild.Id].Enqueue(result);
                    await ctx.RespondAsync(
                        "A track was already playing, the requested track has been added to the queue!");
                    return;
                }
            }
            else
            {
                AudioCommands.queues.Add(ctx.Guild.Id, new ConcurrentQueue<LavalinkTrack>());
                await conn.PlayAsync(result);
            }

            await ctx.RespondAsync($"Now playing {loadResult.Tracks.First().Title}!").ConfigureAwait(false);
            conn.PlaybackFinished += Conn_PlaybackFinished;
        }

        [Command]
        [Hidden]
        public async Task Skip(CommandContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You must be in a voice channel!");
                return;
            }

            var node = Program.LavalinkNode;
            var conn = node.GetConnection(ctx.Member.VoiceState.Guild);

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There is nothing playing!");
                return;
            }

            var hasperms = false;
            if (ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.ManageMessages))
                hasperms = true;
            else
                foreach (var elem in ctx.Member.Roles)
                    if (elem.Name.ToUpper() == "DJ")
                    {
                        hasperms = true;
                        break;
                    }

            if (hasperms == false)
            {
                await ctx.RespondAsync("You must have a role named \"DJ\", or manage messages permissions!");
                return;
            }

            await conn.StopAsync();
            await ctx.RespondAsync("Track skipped!");
        }

        [Command]
        [Hidden]
        public async Task Pause(CommandContext ctx)
        {
            var node = Program.LavalinkNode;

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You must be in a voice channel!");
                return;
            }

            var conn = node.GetConnection(ctx.Guild);


            if (conn == null)
            {
                await ctx.RespondAsync("Not connected.");
                return;
            }

            if (ctx.Member.VoiceState.Channel != conn.Channel)
            {
                await ctx.RespondAsync("You must be in the same channel as the bot!");
                return;
            }

            await conn.PauseAsync();
            await ctx.RespondAsync($"Track paused in {conn.Channel.Name}, use \"{ctx.Prefix}resume\" to resume");
        }

        [Command]
        [Hidden]
        public async Task Resume(CommandContext ctx)
        {
            var node = Program.LavalinkNode;

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You must be in a voice channel!");
                return;
            }

            var conn = node.GetConnection(ctx.Guild);


            if (conn == null)
            {
                await ctx.RespondAsync("Not connected.");
                return;
            }

            if (ctx.Member.VoiceState.Channel != conn.Channel)
            {
                await ctx.RespondAsync("You must be in the same channel as the bot!");
                return;
            }

            await conn.ResumeAsync();
            await ctx.RespondAsync("Track resumed!");
        }

        [Command("seek")]
        [Description("Seeks to specified time in current track.")]
        [Hidden]
        public async Task Seek(CommandContext ctx,
            [RemainingText] [Description("Which time point to seek to.")]
            TimeSpan position)
        {
            var node = Program.LavalinkNode;

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You must be in a voice channel!");
                return;
            }

            var conn = node.GetConnection(ctx.Guild);


            if (conn == null)
            {
                await ctx.RespondAsync("Not connected.");
                return;
            }

            if (ctx.Member.VoiceState.Channel != conn.Channel)
            {
                await ctx.RespondAsync("You must be in the same channel as the bot!");
                return;
            }

            await conn.SeekAsync(position);
            await ctx.RespondAsync($"Track set to {position}!");
        }

        private async Task Conn_PlaybackFinished(TrackFinishEventArgs e)
        {
            await Task.Delay(2000);
            if (e.Reason == TrackEndReason.Replaced || !e.Player.IsConnected) return;
            if (e.Reason == TrackEndReason.Finished || e.Reason == TrackEndReason.Stopped)
                if (!AudioCommands.queues[e.Player.Guild.Id].IsEmpty)
                {
                    AudioCommands.queues[e.Player.Guild.Id].TryDequeue(out var track);
                    await e.Player.PlayAsync(track);
                    e.Player.PlaybackFinished += Conn_PlaybackFinished;
                    return;
                }

            try
            {
                if (e.Player.CurrentState.CurrentTrack == null)
                    await e.Player.DisconnectAsync();
            }
            catch
            {
                // ignored
            }
        }
    }
}