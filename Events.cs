using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using static DiscordUrie_DSharpPlus.Entry;

namespace DiscordUrie_DSharpPlus
{
	class Events
	{

		public static async Task ChatBansEventCall(MessageCreateEventArgs e)
		{
			if (e.Channel.IsPrivate || e.Message.Author.IsBot)
				return;

			DiscordUrieSettings.DiscordUrieGuild GuildSettings = await Settings.FindGuildSettings(e.Guild);

			if (GuildSettings.BansEnabled)
			{
				ulong id = e.Author.Id;

				if (GuildSettings.BannedIds.Any(xr => xr == id))
					await e.Message.DeleteAsync("Chat ban deletion");
			}
		}


		public static Task ErrorHandler(ClientErrorEventArgs e)
		{
			e.Client.DebugLogger.LogMessage(LogLevel.Error, "Discord Urie", $"{e.Exception.GetType()} in the event {e.EventName}. {e.Exception.Message}", DateTime.Now);
			return Task.CompletedTask;
		}

		public static async Task GuildAvailable(GuildCreateEventArgs e)
		{
			if (!Settings.GuildSettings.Any(xr => xr.Id == e.Guild.Id))
				 await Settings.AddGuild(e.Guild);
		}

		public static async Task GuildDeleted(GuildDeleteEventArgs e)
		{
			if (!e.Unavailable)
			{
				await Settings.RemoveGuild(e.Guild.Id);
				e.Client.DebugLogger.LogMessage(LogLevel.Info, "DicordUrie", $"Removed from guild: {e.Guild.Name}", DateTime.Now);
			}
		}

		public static async Task Client_Ready(ReadyEventArgs e)
		{
			if (await Settings.IsEmpty())
			{

				List<DiscordGuild> Yes = new List<DiscordGuild>();

				Yes.AddRange(e.Client.Guilds.Values);

				Settings = await DiscordUrieSettings.CreateAllDefaultSettings(e.Client);
				await Settings.SaveSettings(SQLConn);

			}

			await e.Client.UpdateStatusAsync(Settings.StartupActivity, UserStatus.Online);
			e.Client.DebugLogger.LogMessage(LogLevel.Info, "Discord Urie", "Connected successfully", DateTime.Now);
		}

		public static async Task UserLeaveGuild(GuildMemberRemoveEventArgs e)
		{
			if (e.Member.IsCurrent) return;
			
			DiscordBan UserBan = await e.Guild.GetBanAsync(e.Member);

			await Commands.ColoringStuffGroup.MethodShit.RemoveColor(e.Member, e.Guild, e.Guild.GetDefaultChannel(), true);
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