using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordUrie_DSharpPlus
{

	namespace Attributes
	{

		[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
		class MusicCommand : SlashCheckBaseAttribute
		{
			public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
			{
				var du = ctx.Services.GetService<DiscordUrie>();
				if (!du.BootConfig.MusicEnabled)
				{
					await ctx.CreateResponseAsync("Music module is disabled by my developer. If you believe this is a mistake please contact her.", ephemeral: true);
					return false;
				}
				return true;
			}
		}

		[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
		class ColorCommand : SlashCheckBaseAttribute
		{
			public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
			{
				var du = ctx.Services.GetService<DiscordUrie>();
				var util = new Util(du);
				if (await util.UserAuthHigh(ctx.Member))
					return true;

				var GuildSettings = await du.Config.FindGuildSettings(ctx.Guild);
				if (!GuildSettings.ColorEnabled)
				{
					await ctx.CreateResponseAsync("This is disabled on this guild.");
					return false;
				}

				switch (GuildSettings.ColorBlacklistMode)
				{
					case BlackListModeEnum.Blacklist:
						if (GuildSettings.ColorBlacklist.Any(xr => xr == ctx.Member.Id))
						{
							await ctx.CreateResponseAsync("You are blacklisted from this command and cannot use it.");
							return false;
						}
						break;

					case BlackListModeEnum.Whitelist:
						if (!GuildSettings.ColorBlacklist.Any(xr => xr == ctx.Member.Id))
						{
							await ctx.CreateResponseAsync("You have not been whitelisted for this command and cannot use it.");
							return false;
						}
						break;
					default:
						return true;
				}

				return true;

			}
		}

		[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
		class RequireAuth : SlashCheckBaseAttribute
		{
			public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
			{
				var du = ctx.Services.GetService<DiscordUrie>();
				var util = new Util(du);
				return await util.UserAuth(ctx.Member);
			}

		}

		[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
		class RequireAuthHigh : SlashCheckBaseAttribute
		{
			public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
			{
				var du = ctx.Services.GetService<DiscordUrie>();
				var util = new Util(du);
				return await util.UserAuthHigh(ctx.Member);
			}
		}
	}

}