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
    internal class ExternalCommands : BaseCommandModule
    {
        public string[] E6Array = new string[20];
        public int E6Elem;

        public string[] EBArray = new string[20];
        public int EBElem;

        public string[] R34Array = new string[20];
        public int R34Elem;

        public string[] RedditArray = new string[20];
        public int RedditElem;

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

        [Command("eyebleach")]
        [Aliases("eb")]
        [Description("Gets one of the top 100 hot posts on r/eyebleach")]
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
    }
}