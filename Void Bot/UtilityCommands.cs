using System;
using System.Numerics;
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
        [Command("avatar")]
        [Aliases("pfp")]
        [Description("Takes the username or id and returns the pfp in high quality.")]
        public async Task Avatar(CommandContext ctx, DiscordUser usr)
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