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
using DSharpPlus.Interactivity.Extensions;

namespace DiscordUrie_DSharpPlus
{
	public partial class Commands : BaseCommandModule
	{
		[Group("music"), Aliases("m"), Description("Used to play music (or any youtube video).")]
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
				if (discordUrie.LavalinkNode.GetGuildConnection(guild) != null)
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
				if (this.musicData.Any(xr => xr.GuildId == guild.Id))
					this.musicData.RemoveAll(xr => xr.GuildId == guild.Id);
				this.musicData.Add(new GuildMusicData(guild, conn.Channel, channel));
				return conn;
			}

			async Task Leave(DiscordGuild guild)
			{
				var connection = discordUrie.LavalinkNode.GetGuildConnection(guild);
				var MusicData = this.musicData.First(xr => xr.GuildId == guild.Id);
				if (connection == null || !connection.IsConnected)
				{
					await MusicData.UpdateChannel.SendMessageAsync("Not connected.");
					return;
				}
				await connection.DisconnectAsync();
				this.musicData.Remove(MusicData);
			}

			async Task Play(DiscordGuild guild)
			{
				var connection = discordUrie.LavalinkNode.GetGuildConnection(guild);
				var MusicData = this.musicData.First(xr => xr.GuildId == guild.Id);
				if (connection == null || !connection.IsConnected)
					return;

				if (MusicData.Queue.Count <= 0)
					await Leave(guild);
				
				MusicData.StartNext();
				await connection.PlayAsync(MusicData.NowPlaying);
				await MusicData.UpdateChannel.SendMessageAsync($"Playing `{MusicData.NowPlaying.Title}`");
			}

			async Task PlaybackFinished(LavalinkGuildConnection conn, TrackFinishEventArgs e)
			{
				if (e.Reason != TrackEndReason.Finished) return;
				var MusicData = this.musicData.First(xr => xr.GuildId == conn.Guild.Id);
				if (MusicData.Queue.Count == 0)
					await this.Leave(e.Player.Guild);

				await this.Play(e.Player.Guild);
			}

			[Command("join"), Description("Joins your voice channel.")]
			public async Task Join(CommandContext ctx)
			=> await this.Join(ctx.Guild, ctx.Channel, ctx.Member);

			[Command("leave"), Description("Leaves the voice channel and clears the queue.")]
			public async Task Leave(CommandContext ctx)
			=> await this.Leave(ctx.Guild);

			[Command("search"), Description("Searches for any youtube video and queues it to be played.")]
			public async Task Search(CommandContext ctx, [RemainingText, Description("The video to search for")]string search)
			{
				var connection = discordUrie.LavalinkNode.GetGuildConnection(ctx.Guild);
				if (connection == null) 
					connection = await this.Join(ctx.Guild, ctx.Channel, ctx.Member);
				var MusicData = this.musicData.First(xr => xr.GuildId == ctx.Guild.Id);
				var tracks = await discordUrie.LavalinkNode.Rest.GetTracksAsync(search);
				if (tracks.Tracks.Count() == 0)
				{
					await ctx.RespondAsync("No matches found.");
					return;
				}
				LavalinkTrack track;
				var trackarray = tracks.Tracks.Take(5);
				var embed = new DiscordEmbedBuilder
				{
					Title = "Track selection",
					Color = new DiscordColor("#00ffff")
				};
				embed.AddField("Tracks", string.Join("\n", trackarray.Select((xr, index) => $"{index + 1}. {xr.Title}")));
				var Int = ctx.Client.GetInteractivity();
				await ctx.RespondAsync(embed: embed.Build());
				var Message = await Int.WaitForMessageAsync(xr => Convert.ToInt32(xr.Content) >= 1 || Convert.ToInt32(xr.Content) <= 5);
				if (Message.TimedOut)
					await ctx.RespondAsync("Response time elapsed.");
				track = trackarray.ElementAt(Convert.ToInt32(Message.Result.Content) - 1);
				MusicData.Enqueue(track);
				await ctx.RespondAsync($"Queued {track.Title}");
				if (MusicData.NowPlaying == null && MusicData.Queue.Count <= 1)
					await this.Play(ctx.Guild);

			}

			[Command("clear"), Description("Clear the queue.")]
			public async Task Clear(CommandContext ctx)
			{
				var connection = discordUrie.LavalinkNode.GetGuildConnection(ctx.Guild);
				var MusicData = this.musicData.FirstOrDefault(xr => xr.GuildId == ctx.Guild.Id);
				if (connection == null)
				{
					this.musicData.Remove(MusicData);
					await ctx.RespondAsync("Cleared the queue.");
					return;
				}

				await connection.StopAsync();
				if (MusicData == null)
					return;
				
				MusicData.ClearQueue();
				MusicData.ClearNP();
				await ctx.RespondAsync("Cleared the queue.");
			}

			[Command("stop"), Description("Same as leave.")]
			public async Task Stop(CommandContext ctx)
			{
				var connection = discordUrie.LavalinkNode.GetGuildConnection(ctx.Guild);
				if (connection != null)
					return;

				await connection.StopAsync();
				await this.Leave(ctx.Guild);
				await ctx.RespondAsync("Stopped player.");
			}

			[Command("skip"), Description("Skips the current song.")]
			public async Task Skip(CommandContext ctx)
			{
				var connection = discordUrie.LavalinkNode.GetGuildConnection(ctx.Guild);
				await connection.SeekAsync(connection.CurrentState.CurrentTrack.Length);
				await ctx.RespondAsync($"Skipped {connection.CurrentState.CurrentTrack.Title}");
			}

			[Command("nowplaying"), Aliases("np"), Description("Displays the currently playing track.")]
			public async Task NowPlaying(CommandContext ctx)
			{
				var GuildMusicData = this.musicData.First(xr => xr.GuildId == ctx.Guild.Id);
				var NP = GuildMusicData.NowPlaying;
				await ctx.RespondAsync($"Currently playing `{NP.Title}` at `{NP.Position}/{NP.Length}`");
			}

			[Command("remove"), Description("Removes the track at a specific position in the queue.")]
			public async Task Remove(CommandContext ctx, int index)
			{
				var GuildMusicData = this.musicData.First(xr => xr.GuildId == ctx.Guild.Id);
				GuildMusicData.RemoveQueue(index);
				await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
			}

			[Command("queue"), Description("Shows the queue.")]
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