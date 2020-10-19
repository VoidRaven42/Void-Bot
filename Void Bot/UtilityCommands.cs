using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace Void_Bot
{
    [Group("utility")]
    [Aliases("u", "util")]
    [Description("Commands that do useful stuff")]
    public class UtilityCommands : BaseCommandModule
    {
        [Command("ping")]
        public async Task Ping(CommandContext ctx)
        {
            await ctx.RespondAsync($"Pong! ({ctx.Client.Ping} ms)");
        }

        [Command("avatar")]
        [Aliases("pfp")]
        [Description("Takes the username or id and returns the pfp in high quality.")]
        public async Task Avatar(CommandContext ctx, [RemainingText] DiscordUser usr)
        {
            if (usr == null) usr = ctx.User;
            var embed = new DiscordEmbedBuilder
            {
                Title = "User Avatar",
                Color = DiscordColor.Magenta,
                ImageUrl = usr.AvatarUrl
            };
            await ctx.RespondAsync(embed: embed);
        }

        [Command("roll")]
        [Aliases("dice")]
        [Description(
            "Takes an input of dice notation, (\"2d20 + 6d6 + 1d2\"), and returns the result of all the rolls")]
        public async Task Roll(CommandContext ctx, [RemainingText] string input)
        {
            try
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Rolls",
                    Color = DiscordColor.Green
                };
                var dice = input.Split('+');
                var output = new List<string>();
                var totalrolls = 0;
                var totnum = 0;
                for (var index = 1; index <= dice.Length; index++)
                {
                    var die = dice[index - 1];
                    var args = die.Trim().Split("d");
                    var num = int.Parse(args[0]);
                    var sides = int.Parse(args[1]);
                    totnum += num;
                }

                for (var index = 1; index <= dice.Length; index++)
                {
                    var die = dice[index - 1];
                    var args = die.Trim().Split("d");
                    var num = int.Parse(args[0]);
                    var sides = int.Parse(args[1]);

                    if (sides > 10000)
                    {
                        await ctx.RespondAsync("Over 10000 sides? Why did you just try and roll a ball?");
                        return;
                    }

                    if (totnum <= 24)
                    {
                        for (var i = 0; i < num; i++)
                        {
                            var rand = new Random().Next(1, sides + 1);
                            totalrolls += rand;
                            embed.AddField($"Dice {i + 1} (d{args[1]})", rand.ToString(), true);
                        }
                    }
                    else if (totnum > 500)
                    {
                        await ctx.RespondAsync("Too many dice! (Max 500)");
                        return;
                    }
                    else
                    {
                        for (var i = 0; i < num; i++)
                        {
                            var rand = new Random().Next(1, sides + 1);
                            totalrolls += rand;
                            output.Add($"(d{sides}) " + rand);
                        }
                    }
                }

                if (output.Count == 0)
                {
                    embed.AddField("Total", totalrolls.ToString());
                    await ctx.RespondAsync(embed: embed);
                }
                else
                {
                    var msgcontent = "";
                    foreach (var elem in output) msgcontent += elem + ", ";

                    msgcontent = msgcontent.Trim(',', ' ');
                    msgcontent += $"\n\nTotal: {totalrolls}";
                    if (msgcontent.Length > 2000)
                        msgcontent = $"Too many dice!\n\nTotal: {totalrolls}";
                    await ctx.RespondAsync(msgcontent);
                }
            }
            catch (Exception)
            {
                await ctx.RespondAsync("Invalid input! (Did you roll a ridiculous amount of dice?)");
            }
        }
    }
}