using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

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

			ulong[] SuperDuperCoolPeople = { 105076116942811136 };

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
			DiscordUrieGuild GuildSettings = await discordUrie.Config.FindGuildSettings(Member.Guild);

			ServerAdmins.AddRange(GuildSettings.Admins);

			if (ServerAdmins.Any(xr => xr == Member.Id))
				return true;

			return false;

		}
	}
}