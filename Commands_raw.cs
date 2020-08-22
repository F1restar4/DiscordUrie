using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Newtonsoft.Json;

namespace DiscordUrie_DSharpPlus
{
	public partial class Commands : BaseCommandModule
	{
		[Command("raw"), Description("Supplies information about a givin object")]
		public async Task Raw(CommandContext ctx, DiscordMessage msg)
		{
			await ctx.RespondAsync($"```\n{JsonConvert.SerializeObject(msg, Formatting.Indented)}\n```");
		}

		[Command("raw")]
		public async Task Raw (CommandContext ctx, DiscordMember member)
		{
			await ctx.RespondAsync($"```\n{JsonConvert.SerializeObject(member, Formatting.Indented)}\n```");
		}

		[Command("raw")]
		public async Task Raw (CommandContext ctx, DiscordChannel channel)
		{
			await ctx.RespondAsync($"```\n{JsonConvert.SerializeObject(channel, Formatting.Indented)}\n```");
		}

		[Command("raw")]
		public async Task Raw (CommandContext ctx, DiscordRole role)
		{
			await ctx.RespondAsync($"```\n{JsonConvert.SerializeObject(role, Formatting.Indented)}\n```");
		}

		[Command("raw")]
		public async Task Raw(CommandContext ctx, DiscordGuild guild)
		{
			await ctx.RespondAsync($"```\n{JsonConvert.SerializeObject(guild, Formatting.Indented)}\n```");
		}
	}
}