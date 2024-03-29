using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Data.SQLite;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.Net;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Firestar4.ScpListSharp.Entities;
using DSharpPlus.Entities.AuditLogs;

namespace DiscordUrie_DSharpPlus
{
	public class DiscordUrie
	{
		public DiscordClient Client { get; }
		public InteractivityExtension Interactivity { get; }
		public SlashCommandsExtension SlashCommandsExtension { get; }
		public LavalinkExtension Lavalink { get; }
		public LavalinkNodeConnection LavalinkNode { get; set; }
		public DiscordUrieBootConfig BootConfig { get; set; }
		public List<GuildMusicData> MusicData { get; }
		public DiscordUrieConfig Config { get; set; }
		public DateTime StartTime { get; set; }
		public DateTime SocketStart { get; set; }
		public SQLiteConnection SQLConn { get; }
		public DiscordUrieSettings SettingsInstance { get; }
		public List<DiscordMember> LockedOutUsers { get; set; }
		public List<SCPServer> CachedServerInfo = new List<SCPServer>();
		public DateTime CacheTimestamp = DateTime.Now;

		public DiscordUrie(DiscordUrieConfig cfg, SQLiteConnection connection, DiscordUrieSettings sett)
		{
			SettingsInstance = sett;
			SQLConn = connection;
			this.StartTime = DateTime.Now;
			this.Config = cfg;
			this.MusicData = new List<GuildMusicData>();
			this.LockedOutUsers = new List<DiscordMember>();
			BootConfig = DiscordUrieBootSettings.GetBootConfig();

			//Initial client setup
			this.Client = new DiscordClient(new DiscordConfiguration
			{
				Token = BootConfig.BotToken,
				MinimumLogLevel = LogLevel.Debug,
				Intents = DiscordIntents.All,
			});

			//Client events setup
			this.Client.SessionCreated += this.Client_Ready;
			this.Client.ClientErrored += this.ErrorHandler;
			this.Client.GuildMemberRemoved += this.UserLeaveGuild;
			this.Client.GuildMemberAdded += this.UserJoinGuild;
			this.Client.GuildAvailable += this.GuildAvailable;
			this.Client.GuildDeleted += this.GuildDeleted;
			this.Client.SocketOpened += async (client, e) =>
			{
				await Task.Yield();
				this.SocketStart = DateTime.Now;
			};

			//Build dependancies for injection
			var depend = new ServiceCollection()
				.AddSingleton(this)
				.BuildServiceProvider();

			this.Lavalink = Client.UseLavalink();
			this.SlashCommandsExtension = Client.UseSlashCommands(new SlashCommandsConfiguration
			{
				Services = depend,
			});
			this.SlashCommandsExtension.RegisterCommands<Commands>();
			this.Interactivity = Client.UseInteractivity(new InteractivityConfiguration());
		}

		public async Task StartAsync()
		{
			await this.Client.ConnectAsync();
		}

		private Task ErrorHandler(DiscordClient client, ClientErrorEventArgs e)
		{
			client.Logger.Log(LogLevel.Error, $"{e.Exception.GetType()} in the event {e.EventName}. {e.Exception.Message}");
			return Task.CompletedTask;
		}

		//Add configuration data for new guilds to the database.
		private async Task GuildAvailable(DiscordClient client, GuildCreateEventArgs e)
		{
			if (!this.Config.GuildSettings.Any(xr => xr.Id == e.Guild.Id))
				await this.Config.AddGuild(e.Guild);
		}

		//Remove configuration data from guilds the bot is no longer apart of
		private async Task GuildDeleted(DiscordClient client, GuildDeleteEventArgs e)
		{
			if (!e.Unavailable)
			{
				await this.Config.RemoveGuild(e.Guild.Id);
				client.Logger.Log(LogLevel.Information, $"Removed from guild: {e.Guild.Name}");
			}
		}

		private Task LavalinkGuildConnectionRemoved(LavalinkGuildConnection con, GuildConnectionRemovedEventArgs args)
		{
			var connectedGuilds = con.Node.ConnectedGuilds;
			foreach (var cur in this.MusicData)
			{
				if (!connectedGuilds.Any(xr => xr.Value.Guild.Id == cur.GuildId))
				{
					this.MusicData.Remove(cur);
					return Task.CompletedTask;
				}
			}
			return Task.CompletedTask;
		}

		//Finished downloading guild information
		private async Task Client_Ready(DiscordClient client, SessionReadyEventArgs e)
		{
			//Setup lavalink
			var LavaConfig = new LavalinkConfiguration
			{
				RestEndpoint = new ConnectionEndpoint { Hostname = "localhost", Port = 2333 },
				SocketEndpoint = new ConnectionEndpoint { Hostname = "localhost", Port = 2333 },
				Password = this.BootConfig.LavalinkPassword
			};
			if (this.BootConfig.MusicEnabled)
			{
				this.LavalinkNode = await this.Lavalink.ConnectAsync(LavaConfig);
				this.LavalinkNode.GuildConnectionRemoved += LavalinkGuildConnectionRemoved;
			}

			//Check if global config is empty, this shouldn't happen normally
			if (await this.Config.IsEmpty())
			{
				//Populate the config with default settings for all guilds.
				List<DiscordGuild> Yes = new List<DiscordGuild>();
				Yes.AddRange(client.Guilds.Values);
				this.Config = await this.SettingsInstance.CreateAllDefaultSettings(client, this.SQLConn);
				await this.Config.SaveSettings(SQLConn);
			}

			await client.UpdateStatusAsync(this.BootConfig.StartupActivity, UserStatus.Online);
			client.Logger.Log(LogLevel.Information, "Connected successfully");
		}

		//Automatically assign specified roles to users when they join
		private async Task UserJoinGuild(DiscordClient client, GuildMemberAddEventArgs e)
		{
			if (e.Member.IsCurrent) return;

			var GuildSettings = await this.Config.FindGuildSettings(e.Guild);
			if (GuildSettings.AutoRole == 0) return;

			var role = e.Guild.GetRole(GuildSettings.AutoRole);
			await e.Member.GrantRoleAsync(role, "Auto role");
		}

		//Announce users leaving the guild if enabled
		private async Task UserLeaveGuild(DiscordClient client, GuildMemberRemoveEventArgs e)
		{
			if (e.Member.IsCurrent) return;

			var GuildSettings = await this.Config.FindGuildSettings(e.Guild);
			DiscordChannel channel;
			//Check notification settings
			switch (GuildSettings.NotificationChannel)
			{
				case 0:
					return;

				case 1:
					channel = e.Guild.GetDefaultChannel();
					break;

				default:
					channel = e.Guild.GetChannel(GuildSettings.NotificationChannel);
					break;
			}
			if (channel == null)
				return;

			//Check if the user was banned

			try
			{
				DiscordBan UserBan = await e.Guild.GetBanAsync(e.Member);
				await channel.SendMessageAsync($"{e.Member.Mention} ({e.Member.Username}#{e.Member.Discriminator}) was banned from the guild with the reason `{UserBan.Reason}`");
				return;
			}
			catch
			{

			}

			//Check if the user was kicked
			//The only way to determine this is through the audit logs which can make this inconsistent
			List<DiscordAuditLogEntry> bruh = new List<DiscordAuditLogEntry>();
			await foreach (var i in e.Guild.GetAuditLogsAsync(1, actionType: DiscordAuditLogActionType.Kick))
			{
				bruh.Add(i);
			}
			var LastKick = (DiscordAuditLogKickEntry)bruh.FirstOrDefault();
			if (LastKick != null && LastKick.Target == e.Member)
			{
				await channel.SendMessageAsync($"{e.Member.Mention} ({e.Member.Username}#{e.Member.Discriminator}) was kicked from the guild with the reason `{LastKick.Reason}`");
				return;
			}

			//The user left on their own
			await channel.SendMessageAsync($"{e.Member.Mention} ({e.Member.Username}#{e.Member.Discriminator}) left the guild.");

		}
	}
}