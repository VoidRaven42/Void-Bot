using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

//This file is here to allow easier use of commands, while allowing the help command to remain organised

namespace Void_Bot
{
    public class Commands : BaseCommandModule
    {
        [Command("avatar")]
        [Aliases("pfp")]
        [Description("Takes the username or id and returns the pfp in high quality.")]
        [Hidden]
        public static async Task Avatar(CommandContext ctx, DiscordMember usr)
        {
            await UtilityCommands.Avatar(ctx, usr);
        }

        [Command("eval")]
        [Hidden]
        public static async Task Eval(CommandContext ctx, [RemainingText] [Description("Code to evaluate.")]
            string code)
        {
            await AdministrationCommands.Eval(ctx, code);
        }

        [Command("setprefix")]
        [Aliases("channelprefix")]
        [Description("Sets custom command prefix for current guild. The bot will still respond to the default one.")]
        [RequirePermissions(Permissions.ManageGuild)]
        [Hidden]
        public static async Task SetPrefixAsync(CommandContext ctx,
            [Description("The prefix to use for current guild.")]
            string prefix)
        {
            await AdministrationCommands.SetPrefixAsync(ctx, prefix);
        }

        [Command("purge")]
        [Aliases("clear")]
        [Description("Clears messages, up to 100 at a time")]
        [RequirePermissions(Permissions.ManageMessages)]
        [Hidden]
        public static async Task PurgeAsync(CommandContext ctx, [Description("The amount of messages to purge")]
            int amount)
        {
            await AdministrationCommands.PurgeAsync(ctx, amount);
        }

        [Command("send")]
        [Aliases("sendmessage", "botsend")]
        [Description("Sends a normal message in a channel, can be used for announcements")]
        [RequirePermissions(Permissions.ManageMessages)]
        [Hidden]
        public static async Task SendAsBot(CommandContext ctx, DiscordChannel channel,
            [RemainingText] string messagetosend)
        {
            await AdministrationCommands.SendAsBot(ctx, channel, messagetosend);
        }

        [Command("setbotstatus")]
        [Aliases("status", "setstatus")]
        [Hidden]
        public static async Task Status(CommandContext ctx, string act, [RemainingText] string status)
        {
            await AdministrationCommands.Status(ctx, act, status);
        }

        /*[Command("embed")]
        [Aliases("sendembed")]
        [RequirePermissions(Permissions.ManageMessages)]
        [Hidden]
        THIS IS A WORK IN PROGRESS*/
        public static async Task Embed(CommandContext ctx, [RemainingText] string data)
        {
            await AdministrationCommands.Embed(ctx, data);
        }

        [Command("mathduel")]
        [Aliases("duel", "mathsduel")]
        [Description("Challenges another user to a math duel!")]
        [Hidden]
        public static async Task Duel(CommandContext ctx, [RemainingText] string userintake)
        {
            await FunCommands.Duel(ctx, userintake);
        }

        [Command("speen")]
        [Aliases("spin")]
        [Description("SPEEN")]
        [Hidden]
        public static async Task Speen(CommandContext ctx)
        {
            await FunCommands.Speen(ctx);
        }

        [Command("gun")]
        [Aliases("gunsong")]
        [Description("gun.")]
        [Hidden]
        public static async Task Gun(CommandContext ctx)
        {
            await FunCommands.Gun(ctx);
        }

        [Command("no")]
        [Description("no.")]
        [Hidden]
        public static async Task No(CommandContext ctx)
        {
            await FunCommands.No(ctx);
        }

        [Command("loaf")]
        [Aliases("bunny")]
        [Description("sends BUNNI!")]
        [Hidden]
        public static async Task Loaf(CommandContext ctx)
        {
            await FunCommands.Loaf(ctx);
        }

        [Command("knuckles")]
        [Description("you don't want to use this command")]
        [Hidden]
        public static async Task Knuckles(CommandContext ctx)
        {
            await FunCommands.Knuckles(ctx);
        }

        [Command("horny")]
        [Aliases("bonk")]
        [Description("bonk.")]
        [Hidden]
        public static async Task Horny(CommandContext ctx)
        {
            await FunCommands.Horny(ctx);
        }

        [Command("gitgud")]
        [Description("git gud!")]
        [Hidden]
        public static async Task Gitgud(CommandContext ctx)
        {
            await FunCommands.Gitgud(ctx);
        }

        [Command("turnofflifesupport")]
        [Description("rip grandma")]
        [Hidden]
        public static async Task LifeSupport(CommandContext ctx)
        {
            await FunCommands.LifeSupport(ctx);
        }

        [Command("nonce")]
        [Description("nonce!")]
        [Hidden]
        public static async Task Nonce(CommandContext ctx)
        {
            await FunCommands.Nonce(ctx);
        }

        [Command("reddit")]
        [Aliases("rd")]
        [Description("Gets one of the top 100 hot posts on the specified subreddit")]
        [Hidden]
        public static async Task Reddit(CommandContext ctx, string subreddit)
        {
            await ExternalCommands.Reddit(ctx, subreddit);
        }

        [Command("eyebleach")]
        [Aliases("eb")]
        [Description("Gets one of the top 100 hot posts on r/eyebleach")]
        [Hidden]
        public static async Task Eyebleach(CommandContext ctx)
        {
            await ExternalCommands.Eyebleach(ctx);
        }

        [Command("e621")]
        [Aliases("e6")]
        [Description("Gets one of the top 50 posts by score for the specified tags, or all posts if no tags specified")]
        [Hidden]
        public static async Task E621(CommandContext ctx, [RemainingText] string tags)
        {
            await ExternalCommands.E621(ctx, tags);
        }

        [Command("rule34")]
        [Aliases("r34")]
        [Hidden]
        public static async Task R34(CommandContext ctx, [RemainingText] string tags)
        {
            await ExternalCommands.R34(ctx, tags);
        }

        [Command("beemovie")]
        [Description("the whole bee movie.")]
        [Hidden]
        public static async Task BeeMovie(CommandContext ctx)
        {
            await FunCommands.BeeMovie(ctx);
        }

        [Command("join")]
        [Aliases("connect")]
        [Hidden]
        public static async Task Join(CommandContext ctx)
        {
            await AudioCommands.Join(ctx);
        }

        [Command("leave")]
        [Aliases("disconnect", "dc")]
        [Hidden]
        public static async Task Leave(CommandContext ctx)
        {
            await AudioCommands.Leave(ctx);
        }


        [Command("aspeen")]
        [Hidden]
        public static async Task Aspeen(CommandContext ctx)
        {
            await AudioCommands.Aspeen(ctx);
        }

        [Command]
        [Hidden]
        public static async Task Play(CommandContext ctx, [RemainingText] string search)
        {
            await AudioCommands.Play(ctx, search);
        }

        [Command]
        [Hidden]
        public static async Task Skip(CommandContext ctx)
        {
            await AudioCommands.Skip(ctx);
        }

        [Command]
        [Hidden]
        public static async Task Pause(CommandContext ctx)
        {
            await AudioCommands.Pause(ctx);
        }

        [Command]
        [Hidden]
        public static async Task Resume(CommandContext ctx)
        {
            await AudioCommands.Resume(ctx);
        }

        [Command("seek")]
        [Description("Seeks to specified time in current track.")]
        [Hidden]
        public static async Task Seek(CommandContext ctx,
            [RemainingText] [Description("Which time point to seek to.")]
            TimeSpan position)
        {
            await AudioCommands.Seek(ctx, position);
        }

        [Command]
        [Aliases("q")]
        [Hidden]
        public static async Task Queue(CommandContext ctx)
        {
            await AudioCommands.Queue(ctx);
        }

        [Command]
        [Aliases("np")]
        [Hidden]
        public static async Task NowPlaying(CommandContext ctx)
        {
            await AudioCommands.NowPlaying(ctx);
        }

        [Command("fuckoff")]
        [Hidden]
        public static async Task Fuckoff(CommandContext ctx)
        {
            await FunCommands.Fuckoff(ctx);
        }
    }
}