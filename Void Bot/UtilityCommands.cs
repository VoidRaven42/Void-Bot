using System;
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


        [Command("avatar")]
        [Aliases("pfp")]
        [Description("Takes the username or id and returns the pfp in high quality.")]
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
    }
}