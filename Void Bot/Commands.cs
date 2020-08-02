// Void_Bot.Commands

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using Reddit;
using Reddit.Controllers;

namespace Void_Bot
{
    public class Commands : BaseCommandModule
    {
        private static readonly string dbinfo = File.ReadAllText(@"sqlinfo.txt");

        public readonly MySqlConnection conn = new MySqlConnection(dbinfo);

        public string[] E6Array = new string[20];
        public int E6Elem;

        public string[] RedditArray = new string[20];
        public int RedditElem;
        
        public string[] EBArray = new string[20];
        public int EBElem;

        [Command("math")]
        [Aliases("maths")]
        [Description("Does simple math, (+, -, *, /), called with \"math (first num.) (operation) (second num.)\"")]
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

        [Command("mathduel")]
        [Aliases("duel", "mathsduel")]
        public async Task Duel(CommandContext ctx, [RemainingText] string userintake)
        {
            if (userintake == null)
            {
                await ctx.RespondAsync("Command Help:");
                await new CommandsNextExtension.DefaultHelpModule().DefaultHelpAsync(ctx, ctx.Command.Name);
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

        [Command("e621")]
        [Aliases("e6")]
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
            var e = await EHttpGet("https://e621.net/posts.json", tagssplit.ToArray());
            if (e == null)
            {
                var message = await ctx.RespondAsync("No results found!");
                Thread.Sleep(1000);
                IReadOnlyList<DiscordMessage> messages = new DiscordMessage[2]
                {
                    ctx.Message,
                    message
                };
                await ctx.Channel.DeleteMessagesAsync(messages);
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
                E6Array[E6Elem] = e;
                E6Elem += 1;
                if (E6Elem == 20) E6Elem = 0;
            }
        }

        [Command("rule34")]
        [Aliases("r34")]
        [Hidden]
        public async Task R34(CommandContext ctx, [RemainingText] string tags)
        {
            await ctx.RespondAsync("This command is currently down, it may be days before it is back up");
            return;
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

            var tagssplit = new List<string>();
            if (tags != null) tagssplit = tags.Split(' ').ToList();
            var e = await RHttpGet("https://r34-json-api.herokuapp.com/posts", tagssplit.ToArray());
            if (e == null)
            {
                var message = await ctx.RespondAsync("No results found!");
                Thread.Sleep(1000);
                IReadOnlyList<DiscordMessage> messages = new DiscordMessage[2]
                {
                    ctx.Message,
                    message
                };
                await ctx.Channel.DeleteMessagesAsync(messages);
            }
            else
            {
                var Embed = new DiscordEmbedBuilder
                {
                    Title = "Post Retrieved",
                    Color = DiscordColor.Aquamarine,
                    ImageUrl = e
                };
                Embed.AddField("Requested by:", ctx.User.Username + '#' + ctx.User.Discriminator);
                await ctx.RespondAsync(null, false, Embed);
            }
        }

        public async Task<string> EHttpGet(string URI, string[] tags)
        {
            var client = new WebClient();
            if (tags != null)
            {
                var tagstring = "";
                foreach (var elem in tags) tagstring = tagstring + elem + "+";
                URI = URI + "?tags=" + tagstring + "order:score";
                URI += "&limit=50";
            }

            client.Headers.Add("user-agent", "PostmanRuntime/7.25.0");
            var data = client.OpenRead(URI);
            var streamReader = new StreamReader(data);
            var s = streamReader.ReadToEnd();
            data.Close();
            streamReader.Close();
            var jo = JObject.Parse(s);
            var random = new Random();
            if (!jo["posts"].Any()) return null;
            var img = "";
            for (var i = 0; i < 1000; i++)
            {
                img = jo["posts"][random.Next(0, jo["posts"].Count() - 1)]["file"]["url"].ToString();
                if (!img.EndsWith("webm") && !img.EndsWith("swf") && !E6Array.Contains(img))
                    break;
            }

            if (img.EndsWith("webm") || img.EndsWith("swf")) img = null;
            return img;
        }

        public async Task<string> RHttpGet(string URI, string[] tags)
        {
            var array = tags;
            for (var i = 0; i < array.Length; i++)
                if (array[i].ToLower() == "order:score")
                {
                    var tagslist = tags.ToList();
                    tags = tagslist.Where(val => val.ToLower() != "order:score").ToArray();
                }

            var client = new WebClient();
            if (tags != null)
            {
                var tagstring = "";
                array = tags;
                foreach (var elem in array) tagstring = tagstring + elem + "+";
                var trimmed = tagstring.TrimEnd('+');
                URI = URI + "?tags=" + trimmed;
                URI += "&limit=50";
            }

            client.Headers.Add("user-agent", "PostmanRuntime/7.25.0");
            var data = client.OpenRead(URI);
            var streamReader = new StreamReader(data);
            var s = streamReader.ReadToEnd();
            data.Close();
            streamReader.Close();
            var jo = JArray.Parse(s);
            var random = new Random();
            if (jo.Count == 0) return null;
            var rand = random.Next(0, jo.Count - 1);
            return !(jo[rand]["type"]!.ToString() == "image")
                ? jo[rand]["preview_url"]!.ToString()
                : jo[rand]["file_url"]!.ToString();
        }


        [Command("eyebleach")]
        [Aliases("eb")]
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
                EBArray[E6Elem] = e;
                EBElem += 1;
                if (EBElem == 20) EBElem = 0;
        }

        [Command("avatar")]
        [Aliases("pfp")]
        [Description("Takes the username and returns the pfp in high quality.")]
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

        [Command("knuckles")]
        public async Task Knuckles(CommandContext ctx)
        {
            await ctx.RespondAsync("https://cdn.discordapp.com/emojis/595433217440350248.gif");
        }

        [Command("reddit")]
        [Aliases("rd")]
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
                RedditArray[RedditElem] = img.Permalink;
                RedditElem += 1;
                if (RedditElem == 20) RedditElem = 0;
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
                RedditArray[RedditElem] = img.Permalink;
                RedditElem += 1;
                if (RedditElem == 20) RedditElem = 0;
            }
        }

        [Command("speen")]
        [Aliases("spin")]
        public async Task Speen(CommandContext ctx)
        {
            await ctx.RespondWithFileAsync("speen.mp4");
        }

        [Command("gun")]
        [Aliases("gunsong")]
        public async Task Gun(CommandContext ctx)
        {
            await ctx.RespondWithFileAsync("gun.mp4");
        }
    }
}