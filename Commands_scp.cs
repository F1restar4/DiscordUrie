using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity.EventHandling;
using Firestar4.ScpListSharp;
using Firestar4.ScpListSharp.Entities;
using Firestar4.ScpBanInfo;
using DiscordUrie_DSharpPlus.Attributes;

namespace DiscordUrie_DSharpPlus
{
	public partial class Commands : ApplicationCommandModule
	{
		[SlashCommand("scp", "Gets Packing Peanut's server info")]
		public async Task Scp(InteractionContext ctx)
		{
			SCPServer TargetServer;
			DiscordInteractionResponseBuilder MessageBuilder = new DiscordInteractionResponseBuilder();
			try
			{
				var ServerList = await Rest.GetOwnServersAsync(discordUrie.BootConfig.ScpID, discordUrie.BootConfig.ScpKey, Players: true, Info: true, Version: true, Online: true);
				discordUrie.CachedServerInfo = ServerList;
				TargetServer = ServerList.First();
			}
			catch (Exception ex)
			{
				if (discordUrie.CachedServerInfo.Count == 0)
				{
					await ctx.CreateResponseAsync(ex.Message);
					return;
				}
				TargetServer = discordUrie.CachedServerInfo.First();
				MessageBuilder.WithContent("Rate limited, displaying cached info.");
			}

			var FixedInfo = Regex.Replace(TargetServer.Info, "<[^>]+>", "");
			DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
			builder.Title = FixedInfo;
			builder.WithColor(new DiscordColor("#00ffff"));
			builder.AddField("Online", TargetServer.Online.ToString());
			builder.AddField("Players", TargetServer.Players);
			builder.AddField("Friendly fire", TargetServer.FF.ToString());
			builder.AddField("Version", TargetServer.Version);
			builder.AddField("Modded", TargetServer.Modded.ToString());
			MessageBuilder.AddEmbed(builder.Build());
			await ctx.CreateResponseAsync(MessageBuilder);

		}

		[SlashCommandGroup("scpbans", "Search for a ban on the scp server"), RequireAuth]
		public class Scpbans : ApplicationCommandModule
		{
			public DiscordUrie discordUrie { get; set; }
			public Scpbans(DiscordUrie du)
			{
				discordUrie = du;
			}

			[SlashCommand("get", "Search for a ban on the scp server")]
			public async Task ScpBans(InteractionContext ctx, [Option("search", "The string to search by. Searches for the name, id, or ban reason.")] string search, [Option("DisplayAdminInfo", "Whether or not to display the admin's info.")] bool DisplayAdminInfo = true)
			{
				await ctx.DeferAsync();
				var data = await ScpBanInfo.GetData();
				string lower = search.ToLower();
				var pop = data.FindAll(xr => lower == xr.Target.Name.ToLower() || search == xr.Target.ID.ToString() || lower == xr.Reason.ToLower());
				if (pop.Count == 0)
				{
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("No matches found."));
					return;
				}
				var peep = pop.First();
				var reason = peep.Reason;

				if (String.IsNullOrEmpty(reason))
					reason = "No ban reason givin.";

				DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
				builder.Title = $"Ban info for: {peep.Target.Name}";
				builder.Description = $"ID: {peep.Target.ID.ToString()}";
				builder.AddField("Reason", reason);
				if (DisplayAdminInfo)
					builder.AddField("Admin name", peep.AdminName);
				builder.AddField("Ban time", peep.BanTime.ToShortDateString());
				builder.AddField("Unban time", peep.UnbanTime.ToShortDateString());
				builder.AddField("Ban duration", (peep.BanTime - peep.UnbanTime).ToString("%d' day(s), '%h' hour(s), '%m' minutes, '%s' second(s)'"));
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(builder));
			}

			[SlashCommand("list", "List all bans")]
			public async Task ListBans(InteractionContext ctx)
			{
				var data = await ScpBanInfo.GetData();
				InteractivityExtension intex = ctx.Client.GetInteractivity();
				if (data.Count <= 0) return;
				IEnumerable<string> TagKeys = data.Select(xr => $"{xr.Target.Name} | {xr.Target.ID}");

				string EditedTags = String.Join("\n", TagKeys);
				await intex.SendPaginatedResponseAsync(ctx.Interaction, false, ctx.User, intex.GeneratePagesInEmbed(EditedTags, DSharpPlus.Interactivity.Enums.SplitType.Line));
			}
		}
	}
}