using System.Linq;
using System.Threading.Tasks;
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

            await conn.PlayAsync(loadResult.Tracks.First());

            await ctx.RespondAsync($"Now playing {loadResult.Tracks.First().Title}!").ConfigureAwait(false);
            conn.PlaybackFinished += Conn_PlaybackFinished;
        }


        private async Task Conn_PlaybackFinished(TrackFinishEventArgs e)
        {
            await Task.Delay(2000);
            if (e.Reason == TrackEndReason.Replaced || !e.Player.IsConnected) return;
            try
            {
                await e.Player.DisconnectAsync();
            }
            catch
            {
                // ignored
            }
        }
    }
}