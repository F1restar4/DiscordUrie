using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;

namespace DiscordUrie_DSharpPlus
{
	public static class ext
	{
		public static Task<string> Truncate(this string value, int maxLength)
		{
			if (string.IsNullOrEmpty(value)) return Task.FromResult(value);
			return Task.FromResult(value.Length <= maxLength ? value : value.Substring(0, maxLength));
		}

		public static async Task<DiscordBan> GetBanAsync(this DiscordGuild guild, DiscordUser user)
		{
			var GuildBans = await guild.GetBansAsync();
			return GuildBans.FirstOrDefault(xr => xr.User == user);
		}

		public static Task<string> ToDuration(this TimeSpan span)
		{
			if (span.TotalDays >= 1)
				return Task.FromResult(span.ToString(@"dd\:hh\:mm\:ss"));

            return Task.FromResult(span.ToString(@"hh\:mm\:ss"));

		}
	}

	public class Util
	{
		private DiscordUrie discordUrie { get; set; }
		
		public Util(DiscordUrie du)
		{
			this.discordUrie = du;
		}

		public Task<bool> UserAuthHigh(DiscordMember Member)
		{
			if (Member.IsOwner)
				return Task.FromResult(true);

			ulong[] SuperDuperCoolPeople = { 105076116942811136, 161658438869450752, 197757126611959808 };

			if (SuperDuperCoolPeople.Any(xr => xr == Member.Id))
				return Task.FromResult(true);

			return Task.FromResult(false);
		}

		public async Task<bool> UserAuth(DiscordMember Member)
		{

			if (await UserAuthHigh(Member))
				return true;

			if ((Member.PermissionsIn(Member.Guild.GetDefaultChannel()) & Permissions.Administrator) == Permissions.Administrator)
				return true;

			List<ulong> ServerAdmins = new List<ulong>();
			DiscordUrieSettings.DiscordUrieGuild GuildSettings = await discordUrie.Config.FindGuildSettings(Member.Guild);

			ServerAdmins.AddRange(GuildSettings.Admins);

			if (ServerAdmins.Any(xr => xr == Member.Id))
				return true;

			return false;

		}

		internal async Task<bool> RemoveBan(DiscordClient Client, ulong id, DiscordGuild Guild)
		{
			try
			{

				List<ulong> BannedIds = await discordUrie.Config.GetChatBanIdList(Guild);

				bool removed = BannedIds.Remove(id);


				if (removed)
				{
					DiscordUrieSettings.DiscordUrieGuild GuildSettings = await discordUrie.Config.FindGuildSettings(Guild);
					discordUrie.Config.GuildSettings.Remove(GuildSettings);
					GuildSettings.BannedIds = BannedIds;
					discordUrie.Config.GuildSettings.Add(GuildSettings);
					await GuildSettings.SaveGuild(discordUrie.SQLConn);
					return true;
				}
				else
				{
					return false;
				}
			}
			catch (Exception exc)
			{
				Client.DebugLogger.LogMessage(LogLevel.Error, "Discord Urie", $"Error removing chat ban. {exc.Message}", DateTime.Now);
				throw exc;
			}
		}

		internal async Task<bool> AddBan(DiscordClient Client, ulong id, DiscordGuild Guild)
		{
			try
			{
				List<ulong> BannedIds = await discordUrie.Config.GetChatBanIdList(Guild);

				if (BannedIds.Any(xr => xr == id))
					return false;

				BannedIds.Add(id);
				DiscordUrieSettings.DiscordUrieGuild GuildSettings = await discordUrie.Config.FindGuildSettings(Guild);
				discordUrie.Config.GuildSettings.Remove(GuildSettings);
				GuildSettings.BannedIds = BannedIds;
				discordUrie.Config.GuildSettings.Add(GuildSettings);
				await GuildSettings.SaveGuild(discordUrie.SQLConn);

				return true;

			}
			catch (Exception exc)
			{
				Client.DebugLogger.LogMessage(LogLevel.Error, "Discord Urie", "Error adding ban to list.", DateTime.Now, exc);
				throw exc;
			}

		}
	}
}