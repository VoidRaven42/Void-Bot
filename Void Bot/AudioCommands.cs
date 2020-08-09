using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;

namespace Void_Bot
{
    [Group("audio")]
    [Aliases("au")]
    internal class AudioCommands : BaseCommandModule
    {
        public static Dictionary<ulong, ConcurrentQueue<LavalinkTrack>> queues =
            new Dictionary<ulong, ConcurrentQueue<LavalinkTrack>>();

        [Command("join")]
        [Aliases("connect")]
        public async Task Join(CommandContext ctx)
        {
            var node = Program.LavalinkNode;

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You must be in a voice channel!");
                return;
            }

            await node.ConnectAsync(ctx.Member.VoiceState.Channel);
            await ctx.RespondAsync($"Joined {ctx.Member.VoiceState.Channel.Name}!").ConfigureAwait(false);
        }

        [Command("leave")]
        [Aliases("disconnect", "dc")]
        public async Task Leave(CommandContext ctx)
        {
            var node = Program.LavalinkNode;

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You must be in a voice channel!");
                return;
            }

            var conn = node.GetConnection(ctx.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("Not connected.");
                return;
            }

            if (ctx.Member.VoiceState.Channel != conn.Channel)
            {
                await ctx.RespondAsync("You must be in the same channel as the bot!");
                return;
            }

            foreach (var elem in queues[ctx.Guild.Id]) queues.Remove(ctx.Guild.Id);

            await conn.DisconnectAsync();
            await ctx.RespondAsync($"Left {ctx.Member.VoiceState.Channel.Name}!").ConfigureAwait(false);
        }


        [Command("aspeen")]
        public async Task Aspeen(CommandContext ctx)
        {
            await Play(ctx, "https://www.youtube.com/watch?v=cerkDJLuT_k");
        }

        [Command]
        public async Task Play(CommandContext ctx, [RemainingText] string search)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You must be in a voice channel!");
                return;
            }

            var node = Program.LavalinkNode;
            var conn = node.GetConnection(ctx.Member.VoiceState.Guild);

            if (conn == null) await Join(ctx);
            conn = node.GetConnection(ctx.Guild);
            var loadResult = await node.Rest.GetTracksAsync(search);

            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed ||
                loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await ctx.RespondAsync($"Track search failed for {search}.");
                await Leave(ctx);
                return;
            }

            var result = loadResult.Tracks.First();
            if (queues.ContainsKey(ctx.Guild.Id))
            {
                if (queues[ctx.Guild.Id].IsEmpty && conn.CurrentState.CurrentTrack == null)
                {
                    await conn.PlayAsync(result);
                }
                else
                {
                    queues[ctx.Guild.Id].Enqueue(result);
                    await ctx.RespondAsync(
                        "A track was already playing, the requested track has been added to the queue!");
                    return;
                }
            }
            else
            {
                queues.Add(ctx.Guild.Id, new ConcurrentQueue<LavalinkTrack>());
                await conn.PlayAsync(result);
            }

            await ctx.RespondAsync($"Now playing {loadResult.Tracks.First().Title}!").ConfigureAwait(false);
            conn.PlaybackFinished += Conn_PlaybackFinished;
        }

        [Command]
        public async Task Skip(CommandContext ctx)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You must be in a voice channel!");
                return;
            }

            var node = Program.LavalinkNode;
            var conn = node.GetConnection(ctx.Member.VoiceState.Guild);

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There is nothing playing!");
                return;
            }

            var hasperms = false;
            if (ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.ManageMessages))
                hasperms = true;
            else
                foreach (var elem in ctx.Member.Roles)
                    if (elem.Name.ToUpper() == "DJ")
                    {
                        hasperms = true;
                        break;
                    }

            if (hasperms == false)
            {
                await ctx.RespondAsync("You must have a role named \"DJ\", or manage messages permissions!");
                return;
            }

            await conn.StopAsync();
            await ctx.RespondAsync("Track skipped!");
        }

        [Command]
        public async Task Pause(CommandContext ctx)
        {
            var node = Program.LavalinkNode;

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You must be in a voice channel!");
                return;
            }

            var conn = node.GetConnection(ctx.Guild);


            if (conn == null)
            {
                await ctx.RespondAsync("Not connected.");
                return;
            }

            if (ctx.Member.VoiceState.Channel != conn.Channel)
            {
                await ctx.RespondAsync("You must be in the same channel as the bot!");
                return;
            }

            await conn.PauseAsync();
            await ctx.RespondAsync($"Track paused in {conn.Channel.Name}, use \"{ctx.Prefix}resume\" to resume");
        }

        [Command]
        public async Task Resume(CommandContext ctx)
        {
            var node = Program.LavalinkNode;

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You must be in a voice channel!");
                return;
            }

            var conn = node.GetConnection(ctx.Guild);


            if (conn == null)
            {
                await ctx.RespondAsync("Not connected.");
                return;
            }

            if (ctx.Member.VoiceState.Channel != conn.Channel)
            {
                await ctx.RespondAsync("You must be in the same channel as the bot!");
                return;
            }

            await conn.ResumeAsync();
            await ctx.RespondAsync("Track resumed!");
        }

        [Command("seek")]
        [Description("Seeks to specified time in current track.")]
        public async Task Seek(CommandContext ctx,
            [RemainingText] [Description("Which time point to seek to.")]
            TimeSpan position)
        {
            var node = Program.LavalinkNode;

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You must be in a voice channel!");
                return;
            }

            var conn = node.GetConnection(ctx.Guild);


            if (conn == null)
            {
                await ctx.RespondAsync("Not connected.");
                return;
            }

            if (ctx.Member.VoiceState.Channel != conn.Channel)
            {
                await ctx.RespondAsync("You must be in the same channel as the bot!");
                return;
            }

            await conn.SeekAsync(position);
            await ctx.RespondAsync($"Track set to {position}!");
        }

        private async Task Conn_PlaybackFinished(TrackFinishEventArgs e)
        {
            await Task.Delay(2000);
            if (e.Reason == TrackEndReason.Replaced || !e.Player.IsConnected) return;
            if (e.Reason == TrackEndReason.Finished || e.Reason == TrackEndReason.Stopped)
                if (!queues[e.Player.Guild.Id].IsEmpty)
                {
                    queues[e.Player.Guild.Id].TryDequeue(out var track);
                    await e.Player.PlayAsync(track);
                    e.Player.PlaybackFinished += Conn_PlaybackFinished;
                    return;
                }

            try
            {
                if (e.Player.CurrentState.CurrentTrack == null)
                    await e.Player.DisconnectAsync();
            }
            catch
            {
                // ignored
            }
        }
    }
}