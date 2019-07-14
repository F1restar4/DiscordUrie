using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace DiscordUrie_DSharpPlus
{

    namespace Attributes
    {

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
        class ColorCommand : CheckBaseAttribute
        {
            public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
            {
                if (await Util.UserAuthHigh(ctx.Member.Id, ctx.Guild))
                    return true;

                var GuildSettings = await Entry.Settings.FindGuildSettings(ctx.Guild);
                if (!GuildSettings.ColorEnabled)
                {
                    await ctx.RespondAsync("This is disabled on this guild.");
                    return false;
                }

				switch (GuildSettings.ColorBlacklistMode)
				{
				    case DiscordUrieSettings.BlackListModeEnum.Blacklist:
					    if (GuildSettings.ColorBlacklist.Any(xr => xr == ctx.Member.Id))
					    {
						    await ctx.RespondAsync("You are blacklisted from this command and cannot use it.");
						    return false;
					    }
					    break;

			        case DiscordUrieSettings.BlackListModeEnum.Whitelist:
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
                return await Util.UserAuth(ctx.Member.Id, ctx.Guild);
            }

        }

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
        class RequireAuthHigh : CheckBaseAttribute
        {
            public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
            {
                return await Util.UserAuthHigh(ctx.Member.Id, ctx.Guild);
            }
        }
    }

}