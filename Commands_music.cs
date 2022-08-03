using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DiscordUrie_DSharpPlus.Attributes;

namespace DiscordUrie_DSharpPlus
{
	public partial class Commands : ApplicationCommandModule
	{
		[SlashCommandGroup("music", "Used to play music (or any youtube video)."), MusicCommand]
		public class Music : ApplicationCommandModule
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
				{
					await this.Leave(e.Player.Guild);
					return;
				}

				await this.Play(e.Player.Guild);
			}

			[SlashCommand("join", "Joins your voice channel.")]
			public async Task Join(InteractionContext ctx)
			{
				await this.Join(ctx.Guild, ctx.Channel, ctx.Member);
				await ctx.CreateResponseAsync("Joined.");
			}

			[SlashCommand("leave", "Leaves the voice channel and clears the queue.")]
			public async Task Leave(InteractionContext ctx)
			{
				await this.Leave(ctx.Guild);
				await ctx.CreateResponseAsync("Left and cleared queue.");
			}

			public async Task<LavalinkGuildConnection> GetOrCreateConnectionAsync(DiscordGuild Guild, DiscordChannel Channel, DiscordMember Member)
			{
					var connection = discordUrie.LavalinkNode.GetGuildConnection(Guild);
					if (connection == null) 
						connection = await this.Join(Guild, Channel, Member);
					return connection;
			}

			[SlashCommand("searchurl", "Searches for a specific youtube video via link and queues it to be played.")]
			public async Task SearchUrl(InteractionContext ctx, [Option("ulr", "The url to play")]string search)
			{
				var success = Uri.TryCreate(search, UriKind.RelativeOrAbsolute, out var url);
				if (!success)
				{
					await ctx.CreateResponseAsync("Url invalid.");
					return;
				}
				var connection = await this.GetOrCreateConnectionAsync(ctx.Guild, ctx.Channel, ctx.Member);
				var MusicData = this.musicData.First(xr => xr.GuildId == ctx.Guild.Id);

				var tracks = await discordUrie.LavalinkNode.Rest.GetTracksAsync(url);
				var track = tracks.Tracks.First();
				MusicData.Enqueue(track);
				await ctx.CreateResponseAsync($"Queued {track.Title}");
				if (MusicData.NowPlaying == null && MusicData.Queue.Count <= 1)
					await this.Play(ctx.Guild);
			}

			[SlashCommand("play", "Searches for any youtube video and queues it to be played.")]
			public async Task SearchPlay(InteractionContext ctx, [Option("search", "The video to search for")]string search)
			=> await Search(ctx, search);

			[SlashCommand("search", "Searches for any youtube video and queues it to be played.")]
			public async Task Search(InteractionContext ctx, [Option("search", "The video to search for")]string search)
			{
				var tracks = await discordUrie.LavalinkNode.Rest.GetTracksAsync(search);
				if (tracks.Tracks.Count() == 0)
				{
					await ctx.CreateResponseAsync("No matches found.");
					return;
				}
				await ctx.DeferAsync();

				var connection = await this.GetOrCreateConnectionAsync(ctx.Guild, ctx.Channel, ctx.Member);
				var MusicData = this.musicData.First(xr => xr.GuildId == ctx.Guild.Id);
				var trackarray = tracks.Tracks.Take(5);

				var embed = new DiscordEmbedBuilder
				{
					Title = "Track selection",
					Color = new DiscordColor("#00ffff")
				};
				embed.AddField("Tracks", string.Join("\n", trackarray.Select((xr, index) => $"{index + 1}. {xr.Title}")));
				var Int = ctx.Client.GetInteractivity();
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
				var Message = await Int.WaitForMessageAsync(xr => xr.Author == ctx.User &&(Convert.ToInt32(xr.Content) >= 1 || Convert.ToInt32(xr.Content) <= 5));
				
				if (Message.TimedOut)
				{
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Response time elapsed."));
					if (MusicData.NowPlaying == null && MusicData.Queue.Count == 0)
						await this.Leave(ctx.Guild);
						return;
				}

				var track = trackarray.ElementAt(Convert.ToInt32(Message.Result.Content) - 1);
				MusicData.Enqueue(track);
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Queued {track.Title}"));
				if (MusicData.NowPlaying == null && MusicData.Queue.Count <= 1)
					await this.Play(ctx.Guild);

			}

			[SlashCommand("clear", "Clear the queue.")]
			public async Task Clear(InteractionContext ctx)
			{
				var connection = discordUrie.LavalinkNode.GetGuildConnection(ctx.Guild);
				var MusicData = this.musicData.FirstOrDefault(xr => xr.GuildId == ctx.Guild.Id);
				if (connection == null)
				{
					this.musicData.Remove(MusicData);
					await ctx.CreateResponseAsync("Cleared the queue.");
					return;
				}

				await connection.StopAsync();
				if (MusicData == null)
					return;
				
				MusicData.ClearQueue();
				MusicData.ClearNP();
				await ctx.CreateResponseAsync("Cleared the queue.");
			}

			[SlashCommand("stop", "Same as leave.")]
			public async Task Stop(InteractionContext ctx)
			{
				var connection = discordUrie.LavalinkNode.GetGuildConnection(ctx.Guild);
				if (connection == null)
					return;

				await connection.StopAsync();
				await this.Leave(ctx.Guild);
				await ctx.CreateResponseAsync("Stopped player.");
			}

			[SlashCommand("skip", "Skips the current song.")]
			public async Task Skip(InteractionContext ctx)
			{
				var connection = discordUrie.LavalinkNode.GetGuildConnection(ctx.Guild);
				await connection.SeekAsync(connection.CurrentState.CurrentTrack.Length);
				await ctx.CreateResponseAsync($"Skipped {connection.CurrentState.CurrentTrack.Title}");
			}

			[SlashCommand("nowplaying", "Displays the currently playing track.")]
			public async Task NowPlaying(InteractionContext ctx)
			{
				var GuildMusicData = this.musicData.First(xr => xr.GuildId == ctx.Guild.Id);
				var NP = GuildMusicData.NowPlaying;
				await ctx.CreateResponseAsync($"Currently playing `{NP.Title}` that is {NP.Length} long`");
			}

			[SlashCommand("remove", "Removes the track at a specific position in the queue.")]
			public async Task Remove(InteractionContext ctx, [Option("index", "The index of the entry to remove")] long index)
			{
				var GuildMusicData = this.musicData.First(xr => xr.GuildId == ctx.Guild.Id);
				GuildMusicData.RemoveQueue(Convert.ToInt32(index));
				await ctx.CreateResponseAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
			}

			[SlashCommand("queue", "Shows the queue.")]
			public async Task ShowQueue(InteractionContext ctx)
			{
				var Interactivity = ctx.Client.GetInteractivity();
				var GuildMusicData = this.musicData.First(xr => xr.GuildId == ctx.Guild.Id);
				var Queue = GuildMusicData.Queue;
				var QueuePages = Queue.Select((s, i) => new {track = s, index = i})
					.GroupBy(x => x.index / 10)
					.Select(xr => new Page(embed: new DiscordEmbedBuilder().WithDescription($"Now playing: {GuildMusicData.NowPlaying.Title}\n\n{string.Join("\n", xr.Select(xg => $"`{xg.index}` {xg.track.Title}"))}").WithColor(new DiscordColor("#00ffff"))))
					.ToArray();
				await ctx.CreateResponseAsync("Displaying list", ephemeral: true);
				await Interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, QueuePages, new DSharpPlus.Interactivity.EventHandling.PaginationButtons(), timeoutoverride: TimeSpan.FromSeconds(12));
			}
		}
	}
}