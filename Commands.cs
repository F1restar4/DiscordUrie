using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DiscordUrie_DSharpPlus.Attributes;
using Newtonsoft.Json;

namespace DiscordUrie_DSharpPlus
{
	public partial class Commands : BaseCommandModule
	{
		private DiscordUrie discordUrie { get; set; }

		public Commands(DiscordUrie du)
		{
			discordUrie = du;
		}

		public override async Task BeforeExecutionAsync(CommandContext ctx)
		{
			var util = new Util(discordUrie);
			if (await util.UserAuthHigh(ctx.Member)) return;
			if (discordUrie.LockedOutUsers.Any(xr => xr == ctx.Member))
				throw new DSharpPlus.CommandsNext.Exceptions.CommandNotFoundException(ctx.Command.Name);
		}

		[Command("Lock"), RequireAuth, Description("Disables all commands for a user.")]
		public async Task Lock(CommandContext ctx, DiscordMember Member)
		{
			if (discordUrie.LockedOutUsers.Any(xr => xr == Member))
			{
				await ctx.RespondAsync("They're already locked.");
				return;
			}
			
			discordUrie.LockedOutUsers.Add(Member);
			await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
		}

		[Command("Unlock"), RequireAuth, Description("Allows use of all commands for a user if they were previously disabled.")]
		public async Task Unlock(CommandContext ctx, DiscordMember Member)
		{
			if (!discordUrie.LockedOutUsers.Any(xr => xr == Member))
				return;
			
			discordUrie.LockedOutUsers.Remove(Member);
			await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
		}
	
		[Command("sudo"), RequireOwner, Hidden]
		public async Task Sudo(CommandContext ctx, DiscordUser User, [RemainingText]string Command)
		{
			var cmd = ctx.CommandsNext.FindCommand(Command, out var args);
			var fctx = ctx.CommandsNext.CreateFakeContext(User, ctx.Channel, Command, ctx.Prefix, cmd, args);
			await ctx.CommandsNext.ExecuteCommandAsync(fctx);
		}

		[Command("msg"), Description("Supplies information about a givin message")]
		public async Task msg(CommandContext ctx, DiscordMessage msg)
		{
			await ctx.RespondAsync($"```\n{JsonConvert.SerializeObject(msg, Formatting.Indented)}\n```");
		}
	}
}