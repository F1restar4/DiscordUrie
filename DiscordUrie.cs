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
using SteamWebAPI2.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordUrie_DSharpPlus
{
    public class DiscordUrie
    {

        public DiscordClient Client { get; }
        public CommandsNextExtension CNext { get; }
        public InteractivityExtension Interactivity { get; }
        public DiscordUrieSettings.DiscordUrieConfig Config { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime SocketStart { get; set; }
        public string[] CmdPrefix = { "/" };
        public SteamUser SInterface { get; }
		public SteamStore SStore { get; }
		public PlayerService SPlayerService { get; }
        public SQLiteConnection SQLConn { get; }
        public DiscordUrieSettings SettingsInstance { get; }

        public DiscordUrie(DiscordUrieSettings.DiscordUrieConfig cfg)
        {
            SettingsInstance = new DiscordUrieSettings();

            SQLConn = new SQLiteConnection("Data Source=DiscordUrieConfig.db;Version=3;");

			this.StartTime = DateTime.Now;
            this.Config = cfg;
            string token;
            string steamKey;

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

			if (!File.Exists("steamkey.txt"))
			{
				Console.Write("Input a steam api key: ");
				steamKey = Console.ReadLine();
				File.WriteAllText("steamkey.txt", steamKey);
				Console.Clear();
			}
			else
			{
				steamKey = File.ReadAllText("steamkey.txt");
			}
            
            this.Client = new DiscordClient(new DiscordConfiguration
            {
				Token = token,
				UseInternalLogHandler = true,
            });

            this.Client.Ready += this.Client_Ready;
			this.Client.ClientErrored += this.ErrorHandler;
			this.Client.GuildMemberRemoved += this.UserLeaveGuild;
			this.Client.GuildAvailable += this.GuildAvailable;
			this.Client.GuildDeleted += this.GuildDeleted;
			this.Client.SocketOpened += async () =>
			{
				await Task.Yield();
				this.SocketStart = DateTime.Now;
			};


			this.Client.MessageCreated += async e =>
			{
				if (!e.Author.IsBot)
				{
					await this.ChatBansEventCall(e);
				}
			};

            var depend = new ServiceCollection()
				.AddSingleton(this)
				.BuildServiceProvider();

			this.CNext = Client.UseCommandsNext(new CommandsNextConfiguration
			{
				CaseSensitive = false,
				StringPrefixes = CmdPrefix,
				EnableDefaultHelp = true,
				EnableDms = false,
				Services = depend
			});

            this.CNext.RegisterCommands(Assembly.GetExecutingAssembly());
        
			this.Interactivity = Client.UseInteractivity(new InteractivityConfiguration());

            this.SInterface = new SteamUser(steamKey);
			this.SStore = new SteamStore();
			this.SPlayerService = new PlayerService(steamKey);

        }

        public async Task StartAsync()
        {
            await this.Client.ConnectAsync();
        }
	
		private async Task ChatBansEventCall(MessageCreateEventArgs e)
		{
			if (e.Channel.IsPrivate || e.Message.Author.IsBot)
				return;

			DiscordUrieSettings.DiscordUrieGuild GuildSettings = await this.Config.FindGuildSettings(e.Guild);

			if (GuildSettings.BansEnabled)
			{
				ulong id = e.Author.Id;

				if (GuildSettings.BannedIds.Any(xr => xr == id))
					await e.Message.DeleteAsync("Chat ban deletion");
			}
		}

		private Task ErrorHandler(ClientErrorEventArgs e)
		{
			e.Client.DebugLogger.LogMessage(LogLevel.Error, "Discord Urie", $"{e.Exception.GetType()} in the event {e.EventName}. {e.Exception.Message}", DateTime.Now);
			return Task.CompletedTask;
		}

		private async Task GuildAvailable(GuildCreateEventArgs e)
		{
			if (!this.Config.GuildSettings.Any(xr => xr.Id == e.Guild.Id))
				 await this.Config.AddGuild(e.Guild);
		}

		private async Task GuildDeleted(GuildDeleteEventArgs e)
		{
			if (!e.Unavailable)
			{
				await this.Config.RemoveGuild(e.Guild.Id);
				e.Client.DebugLogger.LogMessage(LogLevel.Info, "DicordUrie", $"Removed from guild: {e.Guild.Name}", DateTime.Now);
			}
		}

		private async Task Client_Ready(ReadyEventArgs e)
		{	

			if (await this.Config.IsEmpty())
			{
				List<DiscordGuild> Yes = new List<DiscordGuild>();

				Yes.AddRange(e.Client.Guilds.Values);

				this.Config = await this.SettingsInstance.CreateAllDefaultSettings(e.Client, this.SQLConn);
				await this.Config.SaveSettings(SQLConn);
			}

			await e.Client.UpdateStatusAsync(this.Config.StartupActivity, UserStatus.Online);
			e.Client.DebugLogger.LogMessage(LogLevel.Info, "Discord Urie", "Connected successfully", DateTime.Now);
		}

		private async Task UserLeaveGuild(GuildMemberRemoveEventArgs e)
		{
			if (e.Member.IsCurrent) return;
			
			DiscordBan UserBan = await e.Guild.GetBanAsync(e.Member);

			if (UserBan != null)
			{
				await e.Guild.GetDefaultChannel().SendMessageAsync($"{e.Member.Mention} ({e.Member.Username}#{e.Member.Discriminator}) was banned from the discord with the reason `{UserBan.Reason}`");
				return;
			}

			var L = await e.Guild.GetAuditLogsAsync(1, action_type: AuditLogActionType.Kick);
			DiscordAuditLogKickEntry LastKick = (DiscordAuditLogKickEntry)L.FirstOrDefault();
			if (LastKick != null && LastKick.Target == e.Member)
			{
				await e.Guild.GetDefaultChannel().SendMessageAsync($"{e.Member.Mention} ({e.Member.Username}#{e.Member.Discriminator}) was kicked from the discord with the reason `{LastKick.Reason}`");
				return;
			}



			await e.Guild.GetDefaultChannel().SendMessageAsync($"{e.Member.Mention} ({e.Member.Username}#{e.Member.Discriminator}) left the discord.");

		}
    }
}