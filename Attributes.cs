using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordUrie_DSharpPlus
{

    namespace Attributes
    {

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
        class ColorCommand : CheckBaseAttribute
        {
            public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
            {
                var du = ctx.Services.GetService<DiscordUrie>();
                var util = new Util(du);
                if (await util.UserAuthHigh(ctx.Member))
                    return true;

                var GuildSettings = await du.Config.FindGuildSettings(ctx.Guild);
                if (!GuildSettings.ColorEnabled)
                {
                    await ctx.RespondAsync("This is disabled on this guild.");
                    return false;
                }

				switch (GuildSettings.ColorBlacklistMode)
				{
				    case BlackListModeEnum.Blacklist:
					    if (GuildSettings.ColorBlacklist.Any(xr => xr == ctx.Member.Id))
					    {
						    await ctx.RespondAsync("You are blacklisted from this command and cannot use it.");
						    return false;
					    }
					    break;

			        case BlackListModeEnum.Whitelist:
						if (!GuildSettings.ColorBlacklist.Any(xr => xr == ctx.Member.Id))
						{
							await ctx.RespondAsync("You have not been whitelisted for this command and cannot use it.");
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
        class RequireAuth : CheckBaseAttribute
        {
            public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
            {
                var du = ctx.Services.GetService<DiscordUrie>();
                var util = new Util(du);
                return await util.UserAuth(ctx.Member);
            }

        }

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
        class RequireAuthHigh : CheckBaseAttribute
        {
            public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
            {
                var du = ctx.Services.GetService<DiscordUrie>();
                var util = new Util(du);
                return await util.UserAuthHigh(ctx.Member);
            }
        }
    }

}