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

		public static string Truncate(this string value, int maxLength)
		{
			if (string.IsNullOrEmpty(value)) return value;
			return value.Length <= maxLength ? value : value.Substring(0, maxLength);
		}

		public static async Task<DiscordBan> GetBanAsync(this DiscordGuild guild, DiscordUser user)
		{
			var GuildBans = await guild.GetBansAsync();
			return GuildBans.FirstOrDefault(xr => xr.User == user);
		}

		public static string ToDuration(this TimeSpan span)
		{
            if (span.TotalHours >= 1)
                return span.ToString(@"h\:mm\:ss");
			return span.ToString(@"m\:ss");
		}

		public static bool UserAuthHigh(ulong inputID, DiscordGuild Guild)
		{
			if (Guild.Owner.Id == inputID)
				return true;

			ulong[] SuperDuperCoolPeople = { 105076116942811136, 161658438869450752, 197757126611959808 };

			if (SuperDuperCoolPeople.Any(xr => xr == inputID))
				return true;

			return false;


		}

		public static bool UserAuth(ulong inputId, DiscordGuild Guild)
		{

			if (UserAuthHigh(inputId, Guild))
				return true;

			List<ulong> ServerAdmins = new List<ulong>();
			DiscordUrieSettings.DiscordUrieGuild GuildSettings = Settings.FindGuildSettings(Guild);

			ServerAdmins.AddRange(GuildSettings.ASettings.Admins);

			if (ServerAdmins.Any(xr => xr == inputId))
				return true;

			return false;

		}

		internal static bool RemoveBan(DiscordClient Client, ulong id, DiscordGuild Guild, out Exception ex)
		{
			try
			{

				List<ulong> BannedIds = Settings.GetChatBanIdList(Guild);

				bool removed = BannedIds.Remove(id);


				if (removed)
				{
					DiscordUrieSettings.DiscordUrieGuild GuildSettings = Settings.FindGuildSettings(Guild);
					Settings.GuildSettings.Remove(GuildSettings);
					GuildSettings.CBSettings = new DiscordUrieSettings.ChatBanSettings()
					{
						Enabled = GuildSettings.CBSettings.Enabled,
						BannedIds = BannedIds,
					};
					Settings.GuildSettings.Add(GuildSettings);
					Settings.SaveSettings();
					ex = null;
					return true;
				}
				else
				{
					ex = null;
					return false;
				}
			}
			catch (Exception exc)
			{
				Client.DebugLogger.LogMessage(LogLevel.Error, "Discord Urie", $"Error removing chat ban. {exc.Message}", DateTime.Now);
				ex = exc;
				return false;
			}
		}

		internal static bool AddBan(DiscordClient Client, ulong id, DiscordGuild Guild, out Exception ex)
		{
			try
			{
				List<ulong> BannedIds = Settings.GetChatBanIdList(Guild);

				if (BannedIds.Any(xr => xr == id))
				{
					ex = null;
					return false;
				}

				BannedIds.Add(id);
				DiscordUrieSettings.DiscordUrieGuild GuildSettings = Settings.FindGuildSettings(Guild);
				Settings.GuildSettings.Remove(GuildSettings);
				GuildSettings.CBSettings = new DiscordUrieSettings.ChatBanSettings()
				{
					Enabled = GuildSettings.CBSettings.Enabled,
					BannedIds = BannedIds,
				};
				Settings.GuildSettings.Add(GuildSettings);
				Settings.SaveSettings();

				ex = null;
				return true;

			}
			catch (Exception exc)
			{
				Client.DebugLogger.LogMessage(LogLevel.Error, "Discord Urie", $"Error adding ban to list. {exc.Message}", DateTime.Now);
				ex = exc;
				return false;
			}

		}
	}
}