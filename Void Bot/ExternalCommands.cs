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
        public static Dictionary<string, string> E6Cache = new Dictionary<string, string>();

        public static string[] E9Array = new string[20];
        public static int E9Elem;
        public static Dictionary<string, string> E9Cache = new Dictionary<string, string>();

        public static string[] R34Array = new string[20];
        public static int R34Elem;
        public static Dictionary<string, string> R34Cache = new Dictionary<string, string>();

        public static string[] RedditArray = new string[35];
        public static int RedditElem;
        public static Dictionary<string, List<Post>> RedditCache = new Dictionary<string, List<Post>>();

        public static string[] EBArray = new string[20];
        public static int EBElem;

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
                embed = new DiscordEmbedBuilder
                {
                    Title = "Subreddit could not be found/accessed, please try another!",
                    Color = DiscordColor.Yellow
                };
                await msg.ModifyAsync(embed: embed.Build());
                return;
            }

            if (sub.Over18.Value && !ctx.Channel.IsNSFW && !Program.Override && !ctx.Channel.IsPrivate)
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = "This subreddit is marked NSFW, please try again in an NSFW channel.",
                    Color = DiscordColor.Yellow
                };
                await msg.ModifyAsync(embed: embed.Build());
                return;
            }
            var hot = new List<Post>();
            if (RedditCache.ContainsKey(subreddit))
            {
                hot = RedditCache[subreddit];
            }
            else
            {
                hot = sub.Posts.Hot;
                RedditCache.Add(subreddit, hot);
            }
            Post img = null;
            var allownsfw = ctx.Channel.IsNSFW;

            for (var i = 0; i < 1000; i++)
            {
                img = hot[random.Next(0, hot.Count - 1)];
                if (img.Listing.SelfText != "") break;
                if (img.Listing.URL.Contains("gifv") ||
                    !img.Listing.URL.Contains("i.redd.it") && !img.Listing.URL.Contains("i.imgur.com")) continue;
                if ((!img.Listing.Over18 || allownsfw && img.Listing.Over18 || Program.Override ||
                     ctx.Channel.IsPrivate) && !RedditArray.Contains(img.Permalink)) break;
            }

            if ((img.Listing.URL.Contains("gifv") ||
                 !img.Listing.URL.Contains("i.redd.it") && !img.Listing.URL.Contains("i.imgur.com")) &&
                img.Listing.SelfText == "")
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = "No valid post could be found, please try again.",
                    Color = DiscordColor.Yellow
                };
                await msg.ModifyAsync(embed: embed.Build());
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
                if (RedditElem == 35) RedditElem = 0;
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
                if (RedditElem == 35) RedditElem = 0;
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
            if (!ctx.Channel.IsNSFW && !Program.Override && !ctx.Channel.IsPrivate)
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
            var e = await E6HttpGet("https://e621.net/posts.json", tagssplit.ToArray(), tags);
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
                Embed.WithFooter("No image? Try clicking the link");
                await msg.ModifyAsync(embed: Embed.Build());
                E6Array[E6Elem] = img;
                E6Elem += 1;
                if (E6Elem == 20) E6Elem = 0;
            }
        }

        [Command("e926")]
        [Aliases("e9")]
        [Description("Gets one of the top 50 posts by score for the specified tags, or all posts if no tags specified")]
        public async Task E926(CommandContext ctx, [RemainingText] string tags)
        {
            var Embed = new DiscordEmbedBuilder
            {
                Title = "Retrieving Post",
                Color = DiscordColor.Yellow
            };
            var msg = await ctx.RespondAsync(embed: Embed.Build());
            var tagssplit = new List<string>();
            if (tags != null) tagssplit = tags.Split(' ').ToList();
            var e = await E9HttpGet("https://e926.net/posts.json", tagssplit.ToArray(), tags);
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
                Embed.WithFooter("No image? Try clicking the link");
                await msg.ModifyAsync(embed: Embed.Build());
                E9Array[E9Elem] = img;
                E9Elem += 1;
                if (E9Elem == 20) E9Elem = 0;
            }
        }

        [Command("rule34")]
        [Aliases("r34")]
        [Description("Gets one of the top 50 posts by score for the specified tags, or all posts if no tags specified")]
        public async Task R34(CommandContext ctx, [RemainingText] string tags)
        {
            if (!ctx.Channel.IsNSFW && !Program.Override && !ctx.Channel.IsPrivate)
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
            var e = await R34HttpGet("https://r34-json-api.herokuapp.com/posts", tagssplit.ToArray(), tags);
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
                var split = e.Split(' ');
                if (e.Contains(' '))
                {
                    Embed = new DiscordEmbedBuilder
                    {
                        Title = "Post Retrieved",
                        Color = DiscordColor.Aquamarine,
                        ImageUrl = split[0]
                    };
                    Embed.AddField("Requested by:", ctx.User.Username + '#' + ctx.User.Discriminator);
                    Embed.AddField("Link:", $"[Direct Link]({split[1]})");
                    Embed.WithFooter("No image? Try clicking the link");
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
                }

                await msg.ModifyAsync(embed: Embed.Build());
                if (e.Contains(' '))
                    R34Array[R34Elem] = split[0];
                else
                    R34Array[R34Elem] = e;
                R34Elem += 1;
                if (R34Elem == 20) R34Elem = 0;
            }
        }

        public async Task<string> E6HttpGet(string URI, string[] tags, string rawtags)
        {
            string s;
            if (E6Cache.ContainsKey(rawtags))
            {
                s = E6Cache[rawtags];
            }
            else
            {
                var client = new WebClient();
                if (tags != null)
                {
                    var tagstring = "";
                    foreach (var elem in tags) tagstring = tagstring + elem + "+";
                    URI = URI + "?tags=" + tagstring + "order:score";
                    URI += "&limit=50";
                }

                client.Headers.Add("user-agent", "VoidBot/1.0 (by nevardiov)");
                var data = client.OpenRead(URI);
                var streamReader = new StreamReader(data);
                s = streamReader.ReadToEnd();
                data.Close();
                streamReader.Close();
                E6Cache.Add(rawtags, s);
            }
            var jo = JObject.Parse(s);
            var random = new Random();
            if (!jo["posts"].Any()) return null;
            var img = "";
            var url = "https://e621.net/posts/";
            for (var i = 0; i < 1000; i++)
            {
                var num = random.Next(0, jo["posts"].Count() - 1);
                img = jo["posts"][num]["file"]["url"].ToString();
                var Break = !img.Contains("webm") && !img.Contains("swf") && !E6Array.Contains(img);
                if (!Break) continue;
                url = url + jo["posts"][num]["id"];
                break;
            }

            return img + '|' + url;
        }

        public async Task<string> E9HttpGet(string URI, string[] tags, string rawtags)
        {
            string s;
            if (E9Cache.ContainsKey(rawtags))
            {
                s = E9Cache[rawtags];
            }
            else
            {
                var client = new WebClient();
                if (tags != null)
                {
                    var tagstring = "";
                    foreach (var elem in tags) tagstring = tagstring + elem + "+";
                    URI = URI + "?tags=" + tagstring + "order:score";
                    URI += "&limit=50";
                }

                client.Headers.Add("user-agent", "VoidBot/1.0 (by nevardiov)");
                var data = client.OpenRead(URI);
                var streamReader = new StreamReader(data);
                s = streamReader.ReadToEnd();
                data.Close();
                streamReader.Close();
                E9Cache.Add(rawtags, s);
            }
            var jo = JObject.Parse(s);
            var random = new Random();
            if (!jo["posts"].Any()) return null;
            var img = "";
            var url = "https://e926.net/posts/";
            for (var i = 0; i < 1000; i++)
            {
                var num = random.Next(0, jo["posts"].Count() - 1);
                img = jo["posts"][num]["file"]["url"].ToString();
                var Break = !img.Contains("webm") && !img.Contains("swf") && !E9Array.Contains(img);
                if (!Break) continue;
                url = url + jo["posts"][num]["id"];
                break;
            }

            return img + '|' + url;
        }

        public async Task<string> R34HttpGet(string URI, string[] tags, string rawtags)
        {
            var array = tags;
            for (var i = 0; i < array.Length; i++)
                if (array[i].ToLower() == "order:score")
                {
                    var tagslist = tags.ToList();
                    tags = tagslist.Where(val => val.ToLower() != "order:score").ToArray();
                }

            string s;
            if (R34Cache.ContainsKey(rawtags))
            {
                s = R34Cache[rawtags];
            }
            else
            {
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
                s = streamReader.ReadToEnd();
                data.Close();
                streamReader.Close();
                R34Cache.Add(rawtags, s);
            }
            var jo = JArray.Parse(s);
            var random = new Random();
            if (jo.Count == 0) return null;
            var rand = 0;
            for (var i = 0; i < 1000; i++)
            {
                rand = random.Next(0, jo.Count - 1);
                if (jo[rand]["source"] != null) break;
            }

            if (jo[rand]["source"] == null)
                return !(jo[rand]["type"]!.ToString() == "image")
                    ? jo[rand]["preview_url"]!.ToString()
                    : jo[rand]["file_url"]!.ToString();

            return !(jo[rand]["type"]!.ToString() == "image")
                ? jo[rand]["preview_url"]!.ToString()
                : jo[rand]["file_url"]!.ToString() + ' ' + jo[rand]["source"];
        }
    }
}