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
using ScpListSharp.Entities;
using ScpListSharp;
using System.Text.RegularExpressions;
using Firestar4.ScpBanInfo;

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

		[Command("scp")]
		public async Task Scp(CommandContext ctx)
		{
			List<SCPServer> ServerList;
			try
			{
				ServerList = await Rest.GetOwnServersAsync(discordUrie.SCPID, discordUrie.SCPKey, Players: true, Info: true, Version: true, Online: true);
			}
			catch (Exception ex)
			{
				await ctx.RespondAsync(ex.Message);
				return;
			}
			var TargetServer = ServerList.First();
			var FixedInfo = Regex.Replace(TargetServer.Info, "<[^>]+>", "");
			DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
			builder.Title = FixedInfo;
			builder.WithColor(new DiscordColor("#00ffff"));
			builder.AddField("Online", TargetServer.Online.ToString());
			builder.AddField("Players", TargetServer.Players);
			builder.AddField("Friendly fire", TargetServer.FF.ToString());
			builder.AddField("Version", TargetServer.Version);
			builder.AddField("Modded", TargetServer.Modded.ToString());
			await ctx.RespondAsync(embed: builder.Build());

		}

		[Command("scpbans"), Description("Search for a ban on the scp server"), RequireAuth]
		public async Task ScpBans(CommandContext ctx, [Description("The string to search by. Searches for the name, id, or ban reason."), RemainingText]string search)
		{
			var data = await ScpBanInfo.GetData();
			string lower = search.ToLower();
			var pop = data.FindAll(xr => lower == xr.Target.Name.ToLower() || search == xr.Target.ID.ToString() || lower == xr.Reason.ToLower());
			if (pop.Count == 0)
			{
				await ctx.RespondAsync("No matches found.");
				return;
			}
			var peep = pop.First();

			DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
			builder.Title = $"Ban info for: {peep.Target.Name}";
			builder.Description = $"ID: {peep.Target.ID.ToString()}";
			builder.AddField("Reason", peep.Reason);
			builder.AddField("Admin name", peep.AdminName);
			builder.AddField("Ban time", peep.BanTime.ToShortDateString());
			builder.AddField("Unban time", peep.UnbanTime.ToShortDateString());
			builder.AddField("Ban duration", (peep.BanTime - peep.UnbanTime).ToString("%d' day(s), '%h' hour(s), '%m' minutes, '%s' second(s)'"));
			await ctx.RespondAsync(builder);

		}
	}
}