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

			DiscordUrieSettings.DiscordUrieGuild GuildSettings = Settings.FindGuildSettings(e.Guild);

			if (!e.Message.Author.IsBot && GuildSettings.CBSettings.Enabled)
			{

				ulong id = e.Author.Id;
				List<ulong> bans = Settings.GetChatBanIdList(e.Guild);

				if (bans.Any(xr => xr == id))
					await e.Message.DeleteAsync("Chat ban deletion");

			}
		}


		public static Task ErrorHandler(ClientErrorEventArgs e)
		{
			e.Client.DebugLogger.LogMessage(LogLevel.Error, "Discord Urie", $"Error in the event {e.EventName}. {e.Exception.Message}", DateTime.Now);
			return Task.CompletedTask;
		}

		public static async Task Client_Ready(ReadyEventArgs e)
		{
			if (Settings.IsEmpty())
			{

				List<DiscordGuild> Yes = new List<DiscordGuild>();

				Yes.AddRange(e.Client.Guilds.Values);

				Settings = DiscordUrieSettings.CreateAllDefaultSettings(e.Client);
				Settings.SaveSettings();

			}

			await e.Client.UpdateStatusAsync(Settings.StartupActivity, UserStatus.Online);
			e.Client.DebugLogger.LogMessage(LogLevel.Info, "Discord Urie", "Connected successfully", DateTime.Now);
		}

		public static async Task UserLeaveGuild(GuildMemberRemoveEventArgs e)
		{
			DiscordBan UserBan = await e.Guild.GetBanAsync(e.Member);

			await Commands.ColoringStuffGroup.MethodShit.RemoveColor(e.Member, e.Guild, e.Guild.GetDefaultChannel(), true);
			if (UserBan != null)
			{
				await e.Guild.GetDefaultChannel().SendMessageAsync($"{e.Member.Mention} ({e.Member.Username}#{e.Member.Discriminator}) was banned from the discord with the reason `{UserBan.Reason}`");
				return;
			}

			var L = await e.Guild.GetAuditLogsAsync(1, action_type: AuditLogActionType.Kick);
			DiscordAuditLogKickEntry LastKick = (DiscordAuditLogKickEntry)L.FirstOrDefault();
			if (LastKick.Target == e.Member)
			{
				await e.Guild.GetDefaultChannel().SendMessageAsync($"{e.Member.Mention} ({e.Member.Username}#{e.Member.Discriminator}) was kicked from the discord with the reason `{LastKick.Reason}`");
				return;
			}



			await e.Guild.GetDefaultChannel().SendMessageAsync($"{e.Member.Mention} ({e.Member.Username}#{e.Member.Discriminator}) left the discord.");

		}


	}
}