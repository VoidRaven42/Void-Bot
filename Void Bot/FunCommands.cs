using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity.Extensions;

namespace Void_Bot
{
    [Group("fun")]
    [Aliases("f")]
    [Description("A group of fun commands")]
    internal class FunCommands : BaseCommandModule
    {
        [Command("mathduel")]
        [Aliases("duel", "mathsduel")]
        [Description("Challenges another user to a math duel!")]
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
                user = await ctx.Client.GetUserAsync(ulong.Parse(usertrim));

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
        public async Task Speen(CommandContext ctx)
        {
            await ctx.RespondWithFileAsync("speen.mp4");
        }

        [Command("gun")]
        [Aliases("gunsong")]
        [Description("gun.")]
        public async Task Gun(CommandContext ctx)
        {
            await ctx.RespondWithFileAsync("gun.mp4");
        }

        [Command("no")]
        [Description("no.")]
        public async Task No(CommandContext ctx)
        {
            await ctx.RespondWithFileAsync("no.mp4");
        }

        [Command("loaf")]
        [Aliases("bunny")]
        [Description("sends BUNNI!")]
        public async Task Loaf(CommandContext ctx)
        {
            await ctx.RespondAsync(
                "https://media.discordapp.net/attachments/670925998434418688/734602804362084455/image0.gif");
        }

        [Command("knuckles")]
        [Description("you don't want to use this command")]
        public async Task Knuckles(CommandContext ctx)
        {
            await ctx.RespondAsync("https://cdn.discordapp.com/emojis/595433217440350248.gif");
        }

        [Command("horny")]
        [Aliases("bonk")]
        [Description("bonk.")]
        public async Task Horny(CommandContext ctx)
        {
            await ctx.RespondAsync("https://tenor.com/view/horny-jail-bonk-dog-hit-head-stop-being-horny-gif-17298755");
        }

        [Command("gitgud")]
        [Description("git gud!")]
        public async Task Gitgud(CommandContext ctx)
        {
            await ctx.RespondAsync(
                "https://cdn.discordapp.com/attachments/681973069333659654/740325342438359091/image0.png");
        }

        [Command("turnofflifesupport")]
        [Description("rip grandma")]
        public async Task LifeSupport(CommandContext ctx)
        {
            await ctx.RespondAsync("Grandma has been terminated.");
        }

        [Command("nonce")]
        [Description("nonce!")]
        public async Task Nonce(CommandContext ctx)
        {
            await ctx.RespondAsync(
                "https://tenor.com/view/nonce-pedo-loop-itz_cam-gif-16228867");
        }

        [Command("beemovie")]
        [Description("the whole bee movie.")]
        public async Task BeeMovie(CommandContext ctx)
        {
            await ctx.RespondWithFileAsync("beemovie.png");
        }

        [Command("fuckoff")]
        public async Task Fuckoff(CommandContext ctx)
        {
            await ctx.RespondAsync(
                "https://cdn.discordapp.com/attachments/730455843484467292/742434971716681879/image0-2.png");
        }

        [Command]
        public async Task Hug(CommandContext ctx)
        {
            await ctx.RespondAsync("https://tenor.com/view/hug-peachcat-cat-cute-gif-13985247");
        }
    }
}