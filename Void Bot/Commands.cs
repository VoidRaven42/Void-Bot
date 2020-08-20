﻿using System;
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
        private UtilityCommands util = new UtilityCommands();
        private AdministrationCommands admin = new AdministrationCommands();
        private FunCommands fun = new FunCommands();
        private ExternalCommands ex = new ExternalCommands();
        private AudioCommands au = new AudioCommands();

        [Command("avatar")]
        [Aliases("pfp")]
        [Description("Takes the username or id and returns the pfp in high quality.")]
        [Hidden]
        public async Task Avatar(CommandContext ctx, DiscordUser usr)
        {
            await util.Avatar(ctx, usr);
        }

        [Command("eval")]
        [Hidden]
        public async Task Eval(CommandContext ctx, [RemainingText] [Description("Code to evaluate.")]
            string code)
        {
            await admin.Eval(ctx, code);
        }

        [Command("setprefix")]
        [Aliases("channelprefix")]
        [Description("Sets custom command prefix for current guild. The bot will still respond to the default one.")]
        [RequirePermissions(Permissions.ManageGuild)]
        [Hidden]
        public async Task SetPrefixAsync(CommandContext ctx, [Description("The prefix to use for current guild.")]
            string prefix)
        {
            await admin.SetPrefixAsync(ctx, prefix);
        }

        [Command("purge")]
        [Aliases("clear")]
        [Description("Clears messages, up to 100 at a time")]
        [RequirePermissions(Permissions.ManageMessages)]
        [Hidden]
        public async Task PurgeAsync(CommandContext ctx, [Description("The amount of messages to purge")]
            int amount)
        {
            await admin.PurgeAsync(ctx, amount);
        }

        [Command("send")]
        [Aliases("sendmessage", "botsend")]
        [Description("Sends a normal message in a channel, can be used for announcements")]
        [RequirePermissions(Permissions.ManageMessages)]
        [Hidden]
        public async Task SendAsBot(CommandContext ctx, DiscordChannel channel, [RemainingText] string messagetosend)
        {
            await admin.SendAsBot(ctx, channel, messagetosend);
        }

        [Command("setbotstatus")]
        [Aliases("status", "setstatus")]
        [Hidden]
        public async Task Status(CommandContext ctx, string act, [RemainingText] string status)
        {
            await admin.Status(ctx, act, status);
        }

        /*[Command("embed")]
        [Aliases("sendembed")]
        [RequirePermissions(Permissions.ManageMessages)]
        [Hidden]
        THIS IS A WORK IN PROGRESS*/
        public async Task Embed(CommandContext ctx, [RemainingText] string data)
        {
            await admin.Embed(ctx, data);
        }

        [Command("mathduel")]
        [Aliases("duel", "mathsduel")]
        [Description("Challenges another user to a math duel!")]
        [Hidden]
        public async Task Duel(CommandContext ctx, [RemainingText] string userintake)
        {
            await fun.Duel(ctx, userintake);
        }

        [Command("speen")]
        [Aliases("spin")]
        [Description("SPEEN")]
        [Hidden]
        public async Task Speen(CommandContext ctx)
        {
            await fun.Speen(ctx);
        }

        [Command("gun")]
        [Aliases("gunsong")]
        [Description("gun.")]
        [Hidden]
        public async Task Gun(CommandContext ctx)
        {
            await fun.Gun(ctx);
        }

        [Command("no")]
        [Description("no.")]
        [Hidden]
        public async Task No(CommandContext ctx)
        {
            await fun.No(ctx);
        }

        [Command("loaf")]
        [Aliases("bunny")]
        [Description("sends BUNNI!")]
        [Hidden]
        public async Task Loaf(CommandContext ctx)
        {
            await fun.Loaf(ctx);
        }

        [Command("knuckles")]
        [Description("you don't want to use this command")]
        [Hidden]
        public async Task Knuckles(CommandContext ctx)
        {
            await fun.Knuckles(ctx);
        }

        [Command("horny")]
        [Aliases("bonk")]
        [Description("bonk.")]
        [Hidden]
        public async Task Horny(CommandContext ctx)
        {
            await fun.Horny(ctx);
        }

        [Command("gitgud")]
        [Description("git gud!")]
        [Hidden]
        public async Task Gitgud(CommandContext ctx)
        {
            await fun.Gitgud(ctx);
        }

        [Command("turnofflifesupport")]
        [Description("rip grandma")]
        [Hidden]
        public async Task LifeSupport(CommandContext ctx)
        {
            await fun.LifeSupport(ctx);
        }

        [Command("nonce")]
        [Description("nonce!")]
        [Hidden]
        public async Task Nonce(CommandContext ctx)
        {
            await fun.Nonce(ctx);
        }

        [Command("reddit")]
        [Aliases("rd")]
        [Description("Gets one of the top 100 hot posts on the specified subreddit")]
        [Hidden]
        public async Task Reddit(CommandContext ctx, string subreddit)
        {
            await ex.Reddit(ctx, subreddit);
        }

        [Command("eyebleach")]
        [Aliases("eb")]
        [Description("Gets one of the top 100 hot posts on r/eyebleach")]
        [Hidden]
        public async Task Eyebleach(CommandContext ctx)
        {
            await ex.Eyebleach(ctx);
        }

        [Command("e621")]
        [Aliases("e6")]
        [Description("Gets one of the top 50 posts by score for the specified tags, or all posts if no tags specified")]
        [Hidden]
        public async Task E621(CommandContext ctx, [RemainingText] string tags)
        {
            await ex.E621(ctx, tags);
        }

        [Command("rule34")]
        [Aliases("r34")]
        [Hidden]
        public async Task R34(CommandContext ctx, [RemainingText] string tags)
        {
            await ex.R34(ctx, tags);
        }

        [Command("beemovie")]
        [Description("the whole bee movie.")]
        [Hidden]
        public async Task BeeMovie(CommandContext ctx)
        {
            await fun.BeeMovie(ctx);
        }

        [Command("join")]
        [Aliases("connect")]
        [Hidden]
        public async Task Join(CommandContext ctx)
        {
            await au.Join(ctx);
        }

        [Command("leave")]
        [Aliases("disconnect", "dc")]
        [Hidden]
        public async Task Leave(CommandContext ctx)
        {
            await au.Leave(ctx);
        }


        [Command("aspeen")]
        [Hidden]
        public async Task Aspeen(CommandContext ctx)
        {
            await au.Aspeen(ctx);
        }

        [Command]
        [Hidden]
        public async Task Play(CommandContext ctx, [RemainingText] string search)
        {
            await au.Play(ctx, search);
        }

        [Command]
        [Hidden]
        public async Task Skip(CommandContext ctx)
        {
            await au.Skip(ctx);
        }

        [Command]
        [Hidden]
        public async Task Pause(CommandContext ctx)
        {
            await au.Pause(ctx);
        }

        [Command]
        [Hidden]
        public async Task Resume(CommandContext ctx)
        {
            await au.Resume(ctx);
        }

        [Command("seek")]
        [Description("Seeks to specified time in current track.")]
        [Hidden]
        public async Task Seek(CommandContext ctx,
            [RemainingText] [Description("Which time point to seek to.")]
            TimeSpan position)
        {
            await au.Seek(ctx, position);
        }

        [Command]
        [Aliases("q")]
        [Hidden]
        public async Task Queue(CommandContext ctx)
        {
            await au.Queue(ctx);
        }

        [Command]
        [Aliases("np")]
        [Hidden]
        public async Task NowPlaying(CommandContext ctx)
        {
            await au.NowPlaying(ctx);
        }

        [Command("fuckoff")]
        [Hidden]
        public async Task Fuckoff(CommandContext ctx)
        {
            await fun.Fuckoff(ctx);
        }
    }
}