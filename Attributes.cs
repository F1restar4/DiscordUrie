using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace DiscordUrie_DSharpPlus
{

    namespace Attributes
    {
        
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