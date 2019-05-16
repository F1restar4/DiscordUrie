using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static DiscordUrie_DSharpPlus.Entry;
using DSharpPlus;
using DSharpPlus.Entities;

namespace DiscordUrie_DSharpPlus
{

	public static class Util
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
            if (span.TotalHours >= 1)
                return Task.FromResult(span.ToString(@"h\:mm\:ss"));
			return Task.FromResult(span.ToString(@"m\:ss"));
		}

		public static Task<bool> UserAuthHigh(ulong inputID, DiscordGuild Guild)
		{
			if (Guild.Owner.Id == inputID)
				return Task.FromResult(true);

			ulong[] SuperDuperCoolPeople = { 105076116942811136, 161658438869450752, 197757126611959808 };

			if (SuperDuperCoolPeople.Any(xr => xr == inputID))
				return Task.FromResult(true);

			return Task.FromResult(false);
		}

		public static async Task<bool> UserAuth(ulong inputId, DiscordGuild Guild)
		{

			if (await UserAuthHigh(inputId, Guild))
				return true;

			List<ulong> ServerAdmins = new List<ulong>();
			DiscordUrieSettings.DiscordUrieGuild GuildSettings = await Settings.FindGuildSettings(Guild);

			ServerAdmins.AddRange(GuildSettings.ASettings.Admins);

			if (ServerAdmins.Any(xr => xr == inputId))
				return true;

			return false;

		}

		internal static async Task<bool> RemoveBan(DiscordClient Client, ulong id, DiscordGuild Guild)
		{
			try
			{

				List<ulong> BannedIds = await Settings.GetChatBanIdList(Guild);

				bool removed = BannedIds.Remove(id);


				if (removed)
				{
					DiscordUrieSettings.DiscordUrieGuild GuildSettings = await Settings.FindGuildSettings(Guild);
					Settings.GuildSettings.Remove(GuildSettings);
					GuildSettings.CBSettings = new DiscordUrieSettings.ChatBanSettings()
					{
						Enabled = GuildSettings.CBSettings.Enabled,
						BannedIds = BannedIds,
					};
					Settings.GuildSettings.Add(GuildSettings);
					await Settings.SaveSettings();
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

		internal static async Task<bool> AddBan(DiscordClient Client, ulong id, DiscordGuild Guild)
		{
			try
			{
				List<ulong> BannedIds = await Settings.GetChatBanIdList(Guild);

				if (BannedIds.Any(xr => xr == id))
					return false;

				BannedIds.Add(id);
				DiscordUrieSettings.DiscordUrieGuild GuildSettings = await Settings.FindGuildSettings(Guild);
				Settings.GuildSettings.Remove(GuildSettings);
				GuildSettings.CBSettings = new DiscordUrieSettings.ChatBanSettings()
				{
					Enabled = GuildSettings.CBSettings.Enabled,
					BannedIds = BannedIds,
				};
				Settings.GuildSettings.Add(GuildSettings);
				await Settings.SaveSettings();

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