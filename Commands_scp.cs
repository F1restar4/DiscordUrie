using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity.EventHandling;
using ScpListSharp;
using ScpListSharp.Entities;
using Firestar4.ScpBanInfo;
using DiscordUrie_DSharpPlus.Attributes;

namespace DiscordUrie_DSharpPlus
{
    public partial class Commands : BaseCommandModule
    {
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

        [Group("scpbans"), Description("Search for a ban on the scp server"), RequireAuth]
        public class Scpbans : BaseCommandModule
        {
            private DiscordUrie discordUrie { get; set; }
			public Scpbans (DiscordUrie du)
			{
				discordUrie = du;
			}

            [GroupCommand(), Description("Search for a ban on the scp server")]
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

			[Command("list"), Description("List all bans")]
			public async Task ListBans(CommandContext ctx)
			{
				var data = await ScpBanInfo.GetData();
				InteractivityExtension intex = ctx.Client.GetInteractivity();
				if (data.Count <= 0) return;
				IEnumerable<string> TagKeys = data.Select(xr => $"{xr.Target.Name} | {xr.Target.ID}");

				string EditedTags = String.Join("\n", TagKeys);


				await intex.SendPaginatedMessageAsync(ctx.Channel, ctx.User, intex.GeneratePagesInEmbed(EditedTags, DSharpPlus.Interactivity.Enums.SplitType.Line), timeoutoverride: TimeSpan.FromSeconds(12));
			}

        }

        
		
    }
}