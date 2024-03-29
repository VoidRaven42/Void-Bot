﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;
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
        public static string[] E6Array = new string[40];
        public static int E6Elem;
        public static Dictionary<string, string> E6Cache = new Dictionary<string, string>();

        public static string[] E9Array = new string[40];
        public static int E9Elem;
        public static Dictionary<string, string> E9Cache = new Dictionary<string, string>();

        public static string[] R34Array = new string[40];
        public static int R34Elem;
        public static Dictionary<string, string> R34Cache = new Dictionary<string, string>();

        public static string[] RedditArray = new string[45];
        public static int RedditElem;
        public static Dictionary<string, List<Post>> RedditCache = new Dictionary<string, List<Post>>();

        private static readonly SQLUtils sqlUtils = new SQLUtils();

        [Command("reddit")]
        [Aliases("rd")]
        [Description("Gets one of the top 100 hot posts on the specified subreddit")]
        public async Task Reddit(CommandContext ctx, string subreddit)
        {
            var random = new Random();
            var rtoken = File.ReadAllText("reddittoken.txt");
            var refresh = File.ReadAllText("refresh.txt");
            var embed = new DiscordEmbedBuilder();
            var msg = ctx.Message;
            if (!RedditCache.ContainsKey(subreddit))
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = "Retrieving Post",
                    Color = DiscordColor.Yellow
                };
                msg = await ctx.RespondAsync(embed: embed.Build());
            }

            var reddit = new RedditClient("Kb6WAOupj1iW1Q", appSecret: rtoken, refreshToken: refresh);
            Subreddit sub = null;

            var hot = new List<Post>();
            if (RedditCache.ContainsKey(subreddit))
            {
                hot = RedditCache[subreddit];
            }
            else
            {
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
                    embed.WithFooter(
                        "If this occurs with a known working subreddit, Reddit may be down, so try again in a few minutes.");
                    msg = msg == ctx.Message
                        ? await ctx.RespondAsync(embed: embed.Build())
                        : await msg.ModifyAsync(embed: embed.Build());
                    return;
                }

                if (sub.Over18.Value && !ctx.Channel.IsNSFW && !Program.Override && ctx.Channel.Id != 778689148059385857 && !ctx.Channel.IsPrivate)
                {
                    embed = new DiscordEmbedBuilder
                    {
                        Title = "This subreddit is marked NSFW, please try again in an NSFW channel.",
                        Color = DiscordColor.Yellow
                    };
                    msg = msg == ctx.Message
                        ? await ctx.RespondAsync(embed: embed.Build())
                        : await msg.ModifyAsync(embed: embed.Build());
                    return;
                }

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
                msg = msg == ctx.Message
                    ? await ctx.RespondAsync(embed: embed.Build())
                    : await msg.ModifyAsync(embed: embed.Build());
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
                msg = msg == ctx.Message
                    ? await ctx.RespondAsync(embed: embed.Build())
                    : await msg.ModifyAsync(embed: embed.Build());
                RedditArray[RedditElem] = img.Permalink;
                RedditElem += 1;
                if (RedditElem == 45) RedditElem = 0;
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
                embed.WithFooter("No image? Try clicking the link");
                msg = msg == ctx.Message
                    ? await ctx.RespondAsync(embed: embed.Build())
                    : await msg.ModifyAsync(embed: embed.Build());
                RedditArray[RedditElem] = img.Permalink;
                RedditElem += 1;
                if (RedditElem == 45) RedditElem = 0;
            }

            Program.Override = false;
        }

        [Command("eyebleach")]
        [Aliases("eb")]
        [Description("Gets one of the top 100 hot posts on r/eyebleach")]
        public async Task Eyebleach(CommandContext ctx)
        {
            await Reddit(ctx, "eyebleach");
        }

        [Command("e621")]
        [Aliases("e6")]
        [Description(
            "Gets one of the top 50 posts from e621.net (furry, nsfw) by score for the specified tags, or all posts if no tags specified")]
        public async Task E621(CommandContext ctx, [RemainingText] string tags)
        {
            if (tags == null) tags = "";

            if (!ctx.Channel.IsNSFW && !Program.Override && ctx.Channel.Id != 778689148059385857 && !ctx.Channel.IsPrivate)
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

            var Embed = new DiscordEmbedBuilder();
            var msg = ctx.Message;
            /*if (!E6Cache.ContainsKey(tags))
            {
                Embed = new DiscordEmbedBuilder
                {
                    Title = "Retrieving Post",
                    Color = DiscordColor.Yellow
                };
                msg = await ctx.RespondAsync(embed: Embed.Build());
            }*/

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
                msg = msg == ctx.Message
                    ? await ctx.RespondAsync(embed: Embed.Build())
                    : await msg.ModifyAsync(embed: Embed.Build());
            }
            else
            {
                var split = e.Split('|');
                var img = split[0];
                var direct = split[1];
                if (img.EndsWith("webm"))
                {
                    Embed = new DiscordEmbedBuilder
                    {
                        Title = "Post Retrieved",
                        Color = DiscordColor.Aquamarine,
                        ImageUrl = img
                    };
                    Embed.AddField("Requested by:", ctx.User.Username + '#' + ctx.User.Discriminator);
                    msg = msg == ctx.Message
                        ? await ctx.RespondAsync(
                            $"Post Retrieved!\nThe file may be too large to display, if nothing shows after a few seconds, click the link.\n\nDirect link: {img}")
                        : await msg.ModifyAsync(
                            $"Post Retrieved!\nThe file may be too large to display, if nothing shows after a few seconds, click the link.\n\nDirect link: {img}",
                            null);
                }
                else
                {
                    Embed = new DiscordEmbedBuilder
                    {
                        Title = "Post Retrieved",
                        Color = DiscordColor.Aquamarine,
                        ImageUrl = img
                    };
                    Embed.AddField("Requested by:", ctx.User.Username + '#' + ctx.User.Discriminator);
                    Embed.AddField("Link:", $"[Direct Link]({direct})");
                    Embed.WithFooter("No image? Try clicking the link");
                    /*msg = msg == ctx.Message
                        ? */
                    await ctx.RespondAsync(embed: Embed.Build());
                    //: await msg.ModifyAsync(embed: Embed.Build());
                }


                E6Array[E6Elem] = img;
                E6Elem += 1;
                if (E6Elem == 40) E6Elem = 0;
            }

            Program.Override = false;
        }

        [Command("e926")]
        [Aliases("e9")]
        [Description(
            "Gets one of the top 50 posts from e926.net (furry, sfw) by score for the specified tags, or all posts if no tags specified")]
        public async Task E926(CommandContext ctx, [RemainingText] string tags)
        {
            if (tags == null) tags = "";

            var Embed = new DiscordEmbedBuilder();
            var msg = ctx.Message;
            if (!E9Cache.ContainsKey(tags))
            {
                Embed = new DiscordEmbedBuilder
                {
                    Title = "Retrieving Post",
                    Color = DiscordColor.Yellow
                };
                msg = await ctx.RespondAsync(embed: Embed.Build());
            }

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
                msg = msg == ctx.Message
                    ? await ctx.RespondAsync(embed: Embed.Build())
                    : await msg.ModifyAsync(embed: Embed.Build());
            }
            else
            {
                var split = e.Split('|');
                var img = split[0];
                var direct = split[1];
                if (img.EndsWith("webm"))
                {
                    Embed = new DiscordEmbedBuilder
                    {
                        Title = "Post Retrieved",
                        Color = DiscordColor.Aquamarine,
                        ImageUrl = img
                    };
                    Embed.AddField("Requested by:", ctx.User.Username + '#' + ctx.User.Discriminator);
                    msg = msg == ctx.Message
                        ? await ctx.RespondAsync(
                            $"Post Retrieved!\nThe file may be too large to display, if nothing shows after a few seconds, click the link.\n\nDirect link: {img}")
                        : await msg.ModifyAsync(
                            $"Post Retrieved!\nThe file may be too large to display, if nothing shows after a few seconds, click the link.\n\nDirect link: {img}",
                            null);
                }
                else
                {
                    Embed = new DiscordEmbedBuilder
                    {
                        Title = "Post Retrieved",
                        Color = DiscordColor.Aquamarine,
                        ImageUrl = img
                    };
                    Embed.AddField("Requested by:", ctx.User.Username + '#' + ctx.User.Discriminator);
                    Embed.AddField("Link:", $"[Direct Link]({direct})");
                    Embed.WithFooter("No image? Try clicking the link");
                    msg = msg == ctx.Message
                        ? await ctx.RespondAsync(embed: Embed.Build())
                        : await msg.ModifyAsync(embed: Embed.Build());
                }

                E9Array[E9Elem] = img;
                E9Elem += 1;
                if (E9Elem == 40) E9Elem = 0;
            }

            Program.Override = false;
        }

        [Command("rule34")]
        [Aliases("r34")]
        [Description(
            "Gets one of the top 50 posts from rule34.xxx (nsfw) by score for the specified tags, or all posts if no tags specified")]
        public async Task R34(CommandContext ctx, [RemainingText] string tags)
        {
            if (tags == null) tags = "";

            if (!ctx.Channel.IsNSFW && !Program.Override && ctx.Channel.Id != 778689148059385857 && !ctx.Channel.IsPrivate)
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

            var Embed = new DiscordEmbedBuilder();
            var msg = ctx.Message;

            if (!R34Cache.ContainsKey(tags))
            {
                Embed = new DiscordEmbedBuilder
                {
                    Title = "Retrieving Post",
                    Color = DiscordColor.Yellow
                };
                msg = await ctx.RespondAsync(embed: Embed.Build());
            }

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
                msg = msg == ctx.Message
                    ? await ctx.RespondAsync(embed: Embed.Build())
                    : await msg.ModifyAsync(embed: Embed.Build());
            }
            else if (e == "error")
            {
                Embed = new DiscordEmbedBuilder
                {
                    Title = "Error retrieving post, please notify `VoidRaven#0042`",
                    Color = DiscordColor.Red
                };
                msg = msg == ctx.Message
                    ? await ctx.RespondAsync(embed: Embed.Build())
                    : await msg.ModifyAsync(embed: Embed.Build());
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

                msg = msg == ctx.Message
                    ? await ctx.RespondAsync(embed: Embed.Build())
                    : await msg.ModifyAsync(embed: Embed.Build());
                if (e.Contains(' '))
                    R34Array[R34Elem] = split[0];
                else
                    R34Array[R34Elem] = e;
                R34Elem += 1;
                if (R34Elem == 40) R34Elem = 0;
            }

            Program.Override = false;
        }

        [Command]
        [Aliases("yfr")]
        [Hidden]
        public async Task Yiffer(CommandContext ctx, [RemainingText] string input)
        {
            if (!ctx.Channel.IsNSFW && !Program.Override && ctx.Channel.Id != 778689148059385857 && !ctx.Channel.IsPrivate)
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
            var query = $"SELECT cname, activepage FROM yiffer WHERE uid='{ctx.User.Id}'";
            var cmd = new MySqlCommand(query, sqlUtils.conn);
            var rdr = cmd.ExecuteReader();
            if (rdr != null)
            {
                var comic = rdr[0];
                var page = rdr[1];
            }
            
            if (input == null)
            { 
                var embed = new DiscordEmbedBuilder
                {
                    Title = "No Records Found!",
                    Description =
                        $"We couldn't find a previous use of this command, please use {ctx.Prefix}yfr new to load a new comic!",
                    Color = DiscordColor.Red
                };
                await ctx.RespondAsync(embed: embed);
                return;
            }


            var split = input.Split(' ', 2);
            var subcommand = split[0];
            var args = split[1];
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
                    foreach (var elem in tags) tagstring = tagstring + "+" + elem;
                    URI = URI + "?tags=" + "order:score" + tagstring;
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
                var num = random.Next(0, jo["posts"].Count());
                img = jo["posts"][num]["file"]["url"].ToString();
                var Break = !img.Contains("swf") && !E6Array.Contains(img);
                if (!Break) continue;
                url = url + jo["posts"][num]["id"];
                break;
            }

            if (img.Contains("swf")) return null;
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
                    foreach (var elem in tags) tagstring = tagstring + "+" + elem;
                    URI = URI + "?tags=" + "order:score" + tagstring;
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
                var num = random.Next(0, jo["posts"].Count());
                img = jo["posts"][num]["file"]["url"].ToString();
                var Break = !img.Contains("swf") && !E6Array.Contains(img);
                if (!Break) continue;
                url = url + jo["posts"][num]["id"];
                break;
            }

            if (img.Contains("swf")) return null;
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

                client.Headers.Add("user-agent", "VoidBot/1.0 (by nevardiov)");
                var data = Stream.Null;
                try
                {
                    data = client.OpenRead(URI);
                }
                catch (Exception)
                {
                    return "error";
                }

                var streamReader = new StreamReader(data);
                s = await streamReader.ReadToEndAsync();
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
                return jo[rand]["type"]!.ToString() != "image"
                    ? jo[rand]["preview_url"]!.ToString()
                    : jo[rand]["file_url"]!.ToString();

            return jo[rand]["type"]!.ToString() != "image"
                ? jo[rand]["preview_url"]!.ToString()
                : jo[rand]["file_url"]!.ToString() + ' ' + jo[rand]["source"];
        }
    }
}