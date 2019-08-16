using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.Interactivity;
namespace DiscordUrie_DSharpPlus
{
    public partial class Commands : BaseCommandModule
    {
        [Group("music"), Aliases("m")]
        public class Music : BaseCommandModule
        {
            public DiscordUrie discordUrie { get; set; }
            public List<GuildMusicData> musicData => this.discordUrie.MusicData;
            public Music(DiscordUrie du)
            {
                discordUrie = du;
            }

            async Task<LavalinkGuildConnection> Join(DiscordGuild guild, DiscordChannel channel, DiscordMember member)
            {
                if (discordUrie.LavalinkNode.GetConnection(guild) != null)
                {
                    await channel.SendMessageAsync("Already connected.");
                    return null;
                }
                if (member.VoiceState.Channel == null)
                {
                    await channel.SendMessageAsync("You need to be in a voice channel first.");
                    return null;
                }
                var conn = await discordUrie.LavalinkNode.ConnectAsync(member.VoiceState.Channel);
                conn.PlaybackFinished += PlaybackFinished;
                this.musicData.Add(new GuildMusicData(guild, conn.Channel, channel));
                return conn;
            }

            async Task Leave(DiscordGuild guild)
            {
                var connection = discordUrie.LavalinkNode.GetConnection(guild);
                var MusicData = this.musicData.First(xr => xr.GuildId == guild.Id);
                if (connection == null || !connection.IsConnected)
                {
                    await MusicData.UpdateChannel.SendMessageAsync("Not connected.");
                    return;
                }
                connection.Disconnect();
                this.musicData.Remove(MusicData);
            }

            async Task Play(DiscordGuild guild)
            {
                var connection = discordUrie.LavalinkNode.GetConnection(guild);
                var MusicData = this.musicData.First(xr => xr.GuildId == guild.Id);
                if (connection == null || !connection.IsConnected)
                    return;

                if (MusicData.Queue.Count <= 0)
                    await Leave(guild);
                
                MusicData.StartNext();
                connection.Play(MusicData.NowPlaying);
                await MusicData.UpdateChannel.SendMessageAsync($"Playing `{MusicData.NowPlaying.Title}`");
            }

            async Task PlaybackFinished(TrackFinishEventArgs e)
            {
                var MusicData = this.musicData.First(xr => xr.GuildId == e.Player.Guild.Id);
                if (MusicData.Queue.Count == 0)
                    await this.Leave(e.Player.Guild);

                await this.Play(e.Player.Guild);
            }

            [Command("join")]
            public async Task Join(CommandContext ctx)
            => await this.Join(ctx.Guild, ctx.Channel, ctx.Member);

            [Command("leave")]
            public async Task Leave(CommandContext ctx)
            => await this.Leave(ctx.Guild);

            [Command("search")]
            public async Task Search(CommandContext ctx, [RemainingText]string search)
            {
                var connection = discordUrie.LavalinkNode.GetConnection(ctx.Guild);
                if (connection == null) 
                    connection = await this.Join(ctx.Guild, ctx.Channel, ctx.Member);

                var MusicData = this.musicData.First(xr => xr.GuildId == ctx.Guild.Id);
                var tracks = await discordUrie.LavalinkNode.GetTracksAsync(search);
                var track = tracks.Tracks.First();
                MusicData.Enqueue(track);
                if (MusicData.NowPlaying.Title == null && MusicData.Queue.Count <= 1)
                    await this.Play(ctx.Guild);

                await ctx.RespondAsync($"Queued {track.Title}");
            }

            [Command("stop")]
            public async Task Stop(CommandContext ctx)
            {
                var connection = discordUrie.LavalinkNode.GetConnection(ctx.Guild);
                if (connection != null)
                    return;

                connection.Stop();
                await this.Leave(ctx.Guild);
                await ctx.RespondAsync("Stopped player.");
            }

            [Command("play")]
            public async Task Play(CommandContext ctx)
                => await this.Play(ctx.Guild);

            [Command("skip")]
            public async Task Skip(CommandContext ctx)
                => await this.Play(ctx.Guild);

            [Command("nowplaying"), Aliases("np")]
            public async Task NowPlaying(CommandContext ctx)
            {
                var GuildMusicData = this.musicData.First(xr => xr.GuildId == ctx.Guild.Id);
                var NP = GuildMusicData.NowPlaying;
                await ctx.RespondAsync($"Currently playing `{NP.Title}` at `{NP.Position}/{NP.Length}`");
            }

            [Command("remove")]
            public async Task Remove(CommandContext ctx, int index)
            {
                var GuildMusicData = this.musicData.First(xr => xr.GuildId == ctx.Guild.Id);
                GuildMusicData.RemoveQueue(index);
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
            }

            [Command("queue")]
            public async Task ShowQueue(CommandContext ctx)
            {
                var Interactivity = ctx.Client.GetInteractivity();
                var GuildMusicData = this.musicData.First(xr => xr.GuildId == ctx.Guild.Id);
                var Queue = GuildMusicData.Queue;
                var QueuePages = Queue.Select((s, i) => new {track = s, index = i})
                    .GroupBy(x => x.index / 10)
                    .Select(xr => new Page(embed: new DiscordEmbedBuilder().WithDescription($"Now playing: {GuildMusicData.NowPlaying.Title}\n\n{string.Join("\n", xr.Select(xg => $"`{xg.index}` {xg.track.Title}"))}").WithColor(new DiscordColor("#00ffff"))))
                    .ToArray();

                await Interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, QueuePages, timeoutoverride: TimeSpan.FromMinutes(2));
            }
        }
    }
}