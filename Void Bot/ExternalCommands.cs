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
using Newtonsoft.Json.Linq;
using Reddit;
using Reddit.Controllers;

namespace Void_Bot
{
    [Group("external")]
    [Aliases("e", "ex")]
    [Description("Commands that get posts from external websites")]
    public class ExternalCommands : BaseCommandModule
    {
        public static string[] E6Array = new string[20];
        public static int E6Elem;

        public static string[] EBArray = new string[20];
        public static int EBElem;

        public static string[] R34Array = new string[20];
        public static int R34Elem;

        public static string[] RedditArray = new string[20];
        public static int RedditElem;

        [Command("reddit")]
        [Aliases("rd")]
        [Description("Gets one of the top 100 hot posts on the specified subreddit")]
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
                    !img.Listing.URL.Contains("i.redd.it") && !img.Listing.URL.Contains("i.imgur.com")) continue;
                if (!img.Listing.Over18 || allownsfw && img.Listing.Over18) break;
            }

            if ((img.Listing.URL.Contains("gifv") ||
                 !img.Listing.URL.Contains("i.redd.it") && !img.Listing.URL.Contains("i.imgur.com")) && img.Listing.SelfText == "")
            {
                await ctx.RespondAsync("No valid post could be found, please try again.");
                return;
            }


            if (img.Listing.SelfText != "")
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = img.Title + " [Textpost]",
                    Color = DiscordColor.Aquamarine
                };
                embed.AddField("Subreddit", "r/" + subreddit);
                embed.AddField("Link:", $"[Direct Link](https://www.reddit.com{img.Permalink})");
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
                    Color = DiscordColor.Aquamarine,
                    ImageUrl = img.Listing.URL
                };
                embed.AddField("Subreddit", "r/" + subreddit);
                embed.AddField("Link:", $"[Direct Link](https://www.reddit.com{img.Permalink})");
                await msg.ModifyAsync(embed: embed.Build());
                RedditArray[RedditElem] = img.Permalink;
                RedditElem += 1;
                if (RedditElem == 20) RedditElem = 0;
            }
        }

        [Command("eyebleach")]
        [Aliases("eb")]
        [Description("Gets one of the top 100 hot posts on r/eyebleach")]
        public async Task Eyebleach(CommandContext ctx)
        {
            Post img = null;
            var embed = new DiscordEmbedBuilder
            {
                Title = "Retrieving Post",
                Color = DiscordColor.Yellow
            };
            var msg = await ctx.RespondAsync(embed: embed.Build());

            var random = new Random();
            var rtoken = File.ReadAllText("reddittoken.txt");
            var refresh = File.ReadAllText("refresh.txt");
            var reddit = new RedditClient("Kb6WAOupj1iW1Q", appSecret: rtoken, refreshToken: refresh);
            var sub = reddit.Subreddit("eyebleach").About();
            var hot = sub.Posts.Hot;
            
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
            embed.AddField("Link:", $"[Direct Link](https://www.reddit.com{img.Permalink})");
            EBArray[EBElem] = img.Listing.URL;
            EBElem += 1;
            if (EBElem == 20) EBElem = 0;
        }

        [Command("e621")]
        [Aliases("e6")]
        [Description("Gets one of the top 50 posts by score for the specified tags, or all posts if no tags specified")]
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
                Embed = new DiscordEmbedBuilder
                {
                    Title = "No results found!",
                    Color = DiscordColor.Red
                };
                await msg.ModifyAsync(embed: Embed.Build());
            }
            else
            {
                var split = e.Split('|');
                var img = split[0];
                var direct = split[1];
                Embed = new DiscordEmbedBuilder
                {
                    Title = "Post Retrieved",
                    Color = DiscordColor.Aquamarine,
                    ImageUrl = img
                };
                Embed.AddField("Requested by:", ctx.User.Username + '#' + ctx.User.Discriminator);
                Embed.AddField("Link:", $"[Direct Link]({direct})");
                await msg.ModifyAsync(embed: Embed.Build());
                E6Array[E6Elem] = img;
                E6Elem += 1;
                if (E6Elem == 20) E6Elem = 0;
            }
        }

        [Command("rule34")]
        [Aliases("r34")]
        [Description("Gets one of the top 50 posts by score for the specified tags, or all posts if no tags specified")]
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
            var e = await RHttpGet("https://r34-json-api.herokuapp.com/posts", tagssplit.ToArray());
            if (e == null)
            {
                Embed = new DiscordEmbedBuilder
                {
                    Title = "No results found!",
                    Color = DiscordColor.Red
                };
                await msg.ModifyAsync(embed: Embed.Build());
            }
            else if (e == "error")
            {
                Embed = new DiscordEmbedBuilder
                {
                    Title = "Error retrieving post, please notify `VoidRaven#0042`",
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
                R34Array[R34Elem] = e;
                R34Elem += 1;
                if (R34Elem == 20) R34Elem = 0;
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
            var url = "https://e621.net/posts/";
            for (var i = 0; i < 1000; i++)
            {
                var num = random.Next(0, jo["posts"].Count() - 1);
                img = jo["posts"][num]["file"]["url"].ToString();
                url = url + jo["posts"][num]["id"].ToString();
                if (!img.EndsWith("webm") && !img.EndsWith("swf") && !E6Array.Contains(img))
                    break;
            }

            if (img.EndsWith("webm") || img.EndsWith("swf")) img = null;
            return img + '|' + url;
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
            var data = Stream.Null;
            try
            {
                data = client.OpenRead(URI);
            }
            catch (Exception e)
            {
                return "error";
            }
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
    }
}