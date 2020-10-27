using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using Google.Cloud.TextToSpeech.V1;

namespace Void_Bot
{
    [Group("audio")]
    [Aliases("au")]
    public class AudioCommands : BaseCommandModule
    {
        public static Dictionary<ulong, ConcurrentQueue<LavalinkTrack>> queues =
            new Dictionary<ulong, ConcurrentQueue<LavalinkTrack>>();

        public static Dictionary<ulong, bool> loops = new Dictionary<ulong, bool>();

        public static LavalinkNodeConnection Lavalink { get; set; }

        [Command("join")]
        [Aliases("connect")]
        public async Task Join(CommandContext ctx)
        {
            var node = Lavalink;

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You must be in a voice channel!");
                return;
            }

            var conn = node.GetGuildConnection(ctx.Guild);
            if (conn != null)
                if (conn.IsConnected)
                {
                    await ctx.RespondAsync($"Already connected to {conn.Channel}!");
                    return;
                }

            await node.ConnectAsync(ctx.Member.VoiceState.Channel);
            queues.Add(ctx.Guild.Id, new ConcurrentQueue<LavalinkTrack>());
            loops.Add(ctx.Guild.Id, false);
            await ctx.RespondAsync($"Joined {ctx.Member.VoiceState.Channel.Name}!").ConfigureAwait(false);
        }

        [Command("leave")]
        [Aliases("disconnect", "dc")]
        public async Task Leave(CommandContext ctx)
        {
            var node = Lavalink;

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You must be in a voice channel!");
                return;
            }

            var conn = node.GetGuildConnection(ctx.Guild);

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

            queues.Remove(ctx.Guild.Id);
            loops.Remove(ctx.Guild.Id);

            await conn.DisconnectAsync();
            await ctx.RespondAsync($"Left {ctx.Member.VoiceState.Channel.Name}!").ConfigureAwait(false);
        }


        [Command("aspeen")]
        public async Task Aspeen(CommandContext ctx)
        {
            await Play(ctx, "https://www.youtube.com/watch?v=cerkDJLuT_k");
        }

        [Command("amemory")]
        public async Task Amemory(CommandContext ctx)
        {
            await Play(ctx, "https://www.youtube.com/watch?v=CWutV6yO7Wo");
        }

        [Command]
        public async Task Play(CommandContext ctx, [RemainingText] string search)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You must be in a voice channel!");
                return;
            }

            var node = Lavalink;
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);


            if (conn == null) await Join(ctx);
            conn = node.GetGuildConnection(ctx.Guild);
            var loadResult = new LavalinkLoadResult();
            if (search.StartsWith("D:/"))
                loadResult = await Lavalink.Rest.GetTracksAsync(new Uri(search, UriKind.Relative));
            else
                loadResult = await Lavalink.Rest.GetTracksAsync(search);

            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed ||
                loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await ctx.RespondAsync("Track search failed.");
                if (conn.CurrentState.CurrentTrack == null)
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
                    if (!result.Identifier.StartsWith("D:/"))
                        await ctx.RespondAsync(
                            $"A track was already playing, the requested track ({result.Title}, `{"https://youtu.be/" + result.Uri.ToString()[^11..]}`) has been added to the queue!");
                    else
                        await ctx.RespondAsync("Enqueued Text-To-Speech.");

                    return;
                }
            }
            else
            {
                queues.Add(ctx.Guild.Id, new ConcurrentQueue<LavalinkTrack>());
                await conn.PlayAsync(result);
            }

            if (!result.Identifier.StartsWith("D:/"))
                await ctx.RespondAsync(
                        $"Now playing {result.Title} by {result.Author} (`{"https://youtu.be/" + result.Uri.ToString()[^11..]}`)!")
                    .ConfigureAwait(false);
            else
                await ctx.RespondAsync("Playing Text-To-Speech!");
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

            var node = Lavalink;
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There is nothing playing!");
                return;
            }

            var hasperms = false;
            if (ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.ManageChannels))
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
                await ctx.RespondAsync("You must have a role named \"DJ\", or manage channel permissions!");
                return;
            }

            await conn.StopAsync();
            await ctx.RespondAsync("Track skipped!");
        }

        [Command]
        public async Task Pause(CommandContext ctx)
        {
            var node = Lavalink;

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You must be in a voice channel!");
                return;
            }

            var conn = node.GetGuildConnection(ctx.Guild);


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
            var node = Lavalink;

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You must be in a voice channel!");
                return;
            }

            var conn = node.GetGuildConnection(ctx.Guild);


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
            var node = Lavalink;

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You must be in a voice channel!");
                return;
            }

            var conn = node.GetGuildConnection(ctx.Guild);


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

            if (conn.CurrentState.CurrentTrack.Length < position)
            {
                await ctx.RespondAsync($"You cannot seek past the end of the song! (Tried to seek to {position})");
                return;
            }

            await conn.SeekAsync(position);
            await ctx.RespondAsync($"Track set to {position}!");
        }

        [Command]
        [Aliases("q")]
        public async Task Queue(CommandContext ctx)
        {
            if (queues.ContainsKey(ctx.Guild.Id))
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Queue",
                    Color = DiscordColor.Azure
                };
                var node = Lavalink;
                var conn = node.GetGuildConnection(ctx.Guild);
                if (queues[ctx.Guild.Id] == null) return;
                if (queues[ctx.Guild.Id].IsEmpty) queues.Remove(ctx.Guild.Id);

                if (!conn.CurrentState.CurrentTrack.Identifier.StartsWith("D:/"))
                    embed.AddField("Currently Playing",
                        conn.CurrentState.CurrentTrack.Title + " by " + conn.CurrentState.CurrentTrack.Author);
                else
                    embed.AddField("Currently Playing", "Text-To-Speech");
                var i = 1;
                foreach (var elem in queues[ctx.Guild.Id])
                {
                    if (!elem.Identifier.StartsWith("D:/"))
                        embed.AddField($"Track {i}", elem.Title + " by " + elem.Author, true);
                    else
                        embed.AddField($"Track {i}", "Text-To-Speech", true);
                    i++;
                }

                try
                {
                    await ctx.RespondAsync(embed: embed);
                }
                catch (ArgumentException)
                {
                    await ctx.RespondAsync(
                        $"Queue is too long to display, it has {queues[ctx.Guild.Id].Count} tracks in it.");
                }
            }
            else
            {
                await ctx.RespondAsync("Queue is empty!");
            }
        }

        [Command]
        [Aliases("np")]
        public async Task NowPlaying(CommandContext ctx)
        {
            var node = Lavalink;
            var conn = node.GetGuildConnection(ctx.Guild);
            var embed = new DiscordEmbedBuilder
            {
                Title = "Now Playing",
                Color = DiscordColor.Lilac
            };
            var current = conn.CurrentState.CurrentTrack;
            if (!current.Identifier.StartsWith("D:/"))
            {
                embed.AddField("Title", current.Title);
                embed.AddField("Position",
                    conn.CurrentState.PlaybackPosition.ToString()
                        .TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9')
                        .TrimEnd('.') + '/' + current.Length, true);
                embed.AddField("Channel", current.Author, true);
                embed.AddField("Direct Link", "https://youtu.be/" + current.Uri.ToString()[^11..]);
                embed.WithThumbnail("https://img.youtube.com/vi/" + current.Uri.ToString()[^11..] + "/0.jpg");
            }
            else
            {
                embed.AddField("Title", "(Text-To-Speech)");
            }

            await ctx.RespondAsync(embed: embed);
        }

        [Command("loop")]
        public async Task Loop(CommandContext ctx)
        {
            var node = Lavalink;

            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You must be in a voice channel!");
                return;
            }

            var conn = node.GetGuildConnection(ctx.Guild);

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

            var hasperms = false;
            if (ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.ManageChannels))
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
                await ctx.RespondAsync("You must have a role named \"DJ\", or manage channel permissions!");
                return;
            }

            if (loops[ctx.Guild.Id])
            {
                loops[ctx.Guild.Id] = false;
                await ctx.RespondAsync("Looping disabled!");
            }
            else
            {
                loops[ctx.Guild.Id] = true;
                await ctx.RespondAsync("Looping enabled!");
            }
        }

        [Command]
        [Aliases("ma")]
        [RequirePermissions(Permissions.MuteMembers)]
        public async Task MuteAll(CommandContext ctx)
        {
            foreach (var elem in ctx.Member.VoiceState.Channel.Users) await elem.SetMuteAsync(true);
            await ctx.RespondAsync("All members muted!");
        }

        [Command]
        [Aliases("uma")]
        [RequirePermissions(Permissions.MuteMembers)]
        public async Task UnMuteAll(CommandContext ctx)
        {
            foreach (var elem in ctx.Member.VoiceState.Channel.Users) await elem.SetMuteAsync(false);
            await ctx.RespondAsync("All members unmuted!");
        }

        [Command]
        [Aliases("tts")]
        public async Task TextToSpeech(CommandContext ctx, [RemainingText] string inputstring)
        {
            var msg = await ctx.RespondAsync("Processing audio...");
            var client = await new TextToSpeechClientBuilder
                {CredentialsPath = "Void Bot TTS-4af47ad6a178.json"}.BuildAsync();
            var input = new SynthesisInput
            {
                Text = inputstring
            };

            var voice = new VoiceSelectionParams
            {
                LanguageCode = "en-US",
                SsmlGender = SsmlVoiceGender.Neutral
            };

            var config = new AudioConfig
            {
                AudioEncoding = AudioEncoding.Mp3
            };

            var response = new SynthesizeSpeechResponse();

            try
            {
                response = client.SynthesizeSpeech(new SynthesizeSpeechRequest
                {
                    Input = input,
                    Voice = voice,
                    AudioConfig = config
                });
            }
            catch (Exception)
            {
                await msg.ModifyAsync("Error occurred in processing audio! (Was your request too long?)");

                return;
            }

            var time = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();

            using (Stream output = File.Create($"D:/Lavalink/Temp/{time}.mp3"))
            {
                response.AudioContent.WriteTo(output);
            }

            await Play(ctx, $"D:/Lavalink/Temp/{time}.mp3");
        }

        public async Task Conn_PlaybackFinished(LavalinkGuildConnection conn, TrackFinishEventArgs e)
        {
            await Task.Delay(2000);
            if (e.Track.Identifier.StartsWith("D:/")) File.Delete(e.Track.Identifier);
            if (e.Reason == TrackEndReason.Replaced || !e.Player.IsConnected) return;
            if (e.Reason == TrackEndReason.Finished || e.Reason == TrackEndReason.Stopped)
            {
                if (loops[e.Player.Guild.Id])
                {
                    await e.Player.PlayAsync(e.Track);
                }
                else
                {
                    if (loops.ContainsKey(e.Player.Guild.Id)) loops.Remove(e.Player.Guild.Id);
                }

                if (!queues[e.Player.Guild.Id].IsEmpty)
                {
                    queues[e.Player.Guild.Id].TryDequeue(out var track);
                    await e.Player.PlayAsync(track);
                    e.Player.PlaybackFinished += Conn_PlaybackFinished;
                    return;
                }
            }

            try
            {
                if (e.Player.CurrentState.CurrentTrack == null)
                {
                    queues.Remove(e.Player.Guild.Id);
                    await e.Player.DisconnectAsync();
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}