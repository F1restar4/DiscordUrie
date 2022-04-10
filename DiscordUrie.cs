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
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DiscordUrie_DSharpPlus
{
	public class DiscordUrie
	{
		public DiscordClient Client { get; }
		public CommandsNextExtension CNext { get; }
		public InteractivityExtension Interactivity { get; }
		public LavalinkExtension Lavalink { get; }
		public LavalinkNodeConnection LavalinkNode { get; set; }
		public string LavaPass { get; }
		public List<GuildMusicData> MusicData { get; }
		public DiscordUrieConfig Config { get; set; }
		public DateTime StartTime { get; set; }
		public DateTime SocketStart { get; set; }
		public string[] CmdPrefix = { "/" };
		public SQLiteConnection SQLConn { get; }
		public DiscordUrieSettings SettingsInstance { get; }
		public List<DiscordMember> LockedOutUsers { get; set; }
		public int SCPID { get; set; }
		public string SCPKey { get; set; }
		public List<Firestar4.ScpListSharp.Entities.SCPServer> CachedServerInfo = new List<Firestar4.ScpListSharp.Entities.SCPServer>();

		public DiscordUrie(DiscordUrieConfig cfg, SQLiteConnection connection, DiscordUrieSettings sett)
		{
			SettingsInstance = sett;
			SQLConn = connection;
			this.StartTime = DateTime.Now;
			this.Config = cfg;
			this.MusicData = new List<GuildMusicData>();
			this.LockedOutUsers = new List<DiscordMember>();
			string token;

			//Check for a saved token
			if (!File.Exists("token.txt"))
			{
				Console.Write("Token file not found. Please input a Discord bot token: ");
				token = Console.ReadLine();

				File.WriteAllText("token.txt", token);
				Console.Clear();
			}
			else
			{
				token = File.ReadAllText("token.txt");
			}

			//Check for a saved LavaLink server password
			//Maybe I should consolidate this, the token and the scplist info to cleanup these files.
			if (!File.Exists("lavapass.txt"))
			{
				Console.Write("Input the lavalink server password: ");
				this.LavaPass = Console.ReadLine();
				File.WriteAllText("lavapass.txt", this.LavaPass);
				Console.Clear();
			}
			else
			{
				this.LavaPass = File.ReadAllText("lavapass.txt");
			}

			//Check for saved ScpList info
			if (!File.Exists("ScpInfo.txt"))
			{
				Console.Write("Input your SCP account ID");
				this.SCPID = Convert.ToInt32(Console.ReadLine());
				Console.Write("Input your SCP server api key: ");
				this.SCPKey = Console.ReadLine();
				string[] data = {this.SCPID.ToString(), this.SCPKey};
				File.WriteAllLines("ScpInfo.txt", data);
			}
			else
			{
				var data = File.ReadAllLines("ScpInfo.txt");
				this.SCPID = Convert.ToInt32(data[0]);
				this.SCPKey = data[1];
			}
			
			//Initial client setup
			this.Client = new DiscordClient(new DiscordConfiguration
			{
				Token = token,
				MinimumLogLevel = LogLevel.Information,
				Intents = DiscordIntents.All,
			});

			//Client events setup
			this.Client.Ready += this.Client_Ready;
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

			//Final client setup
			this.CNext = Client.UseCommandsNext(new CommandsNextConfiguration
			{
				CaseSensitive = false,
				StringPrefixes = CmdPrefix,
				EnableDefaultHelp = true,
				EnableDms = false,
				Services = depend
			});

			this.Lavalink = Client.UseLavalink();
			this.CNext.RegisterCommands(Assembly.GetExecutingAssembly());
			this.Interactivity = Client.UseInteractivity(new InteractivityConfiguration
			{
				AckPaginationButtons = true,
			});
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

		//Finished downloading guild information
		private async Task Client_Ready(DiscordClient client, ReadyEventArgs e)
		{	
			//Setup lavalink
			var LavaConfig = new LavalinkConfiguration
			{
					RestEndpoint = new ConnectionEndpoint { Hostname = "localhost", Port = 2333 },
					SocketEndpoint = new ConnectionEndpoint { Hostname = "localhost", Port = 2333 },
					Password = this.LavaPass
			};
			this.LavalinkNode = await this.Lavalink.ConnectAsync(LavaConfig);
			
			//Check if global config is empty, this shouldn't happen normally
			if (await this.Config.IsEmpty())
			{
				//Populate the config with default settings for all guilds.
				List<DiscordGuild> Yes = new List<DiscordGuild>();
				Yes.AddRange(client.Guilds.Values);
				this.Config = await this.SettingsInstance.CreateAllDefaultSettings(client, this.SQLConn);
				await this.Config.SaveSettings(SQLConn);
			}

			await client.UpdateStatusAsync(this.Config.StartupActivity, UserStatus.Online);
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
			switch(GuildSettings.NotificationChannel)
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
			var L = await e.Guild.GetAuditLogsAsync(1, action_type: AuditLogActionType.Kick);
			DiscordAuditLogKickEntry LastKick = (DiscordAuditLogKickEntry)L.FirstOrDefault();
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