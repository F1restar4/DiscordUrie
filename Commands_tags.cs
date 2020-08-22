using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity;
using DiscordUrie_DSharpPlus.Attributes;

namespace DiscordUrie_DSharpPlus
{
	public partial class Commands : BaseCommandModule
	{

		[Group("tag"), Description("Allows a user to store a message for later recall")]
		public class Tag : BaseCommandModule
		{
			private DiscordUrie discordUrie { get; set; }
			public Tag(DiscordUrie du)
			{
				discordUrie = du;
			}

			[GroupCommand(), Description("Recalls a tag using it's name.")]
			public async Task GroupExecute(CommandContext ctx, [RemainingText] string tag)
			{
				var GuildSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);

				tag = Regex.Escape(tag).ToLower();

				if (GuildSettings.Tags.Any(xr => xr.Tag.ToLower() == tag))
				{
					await ctx.RespondAsync(Regex.Unescape(GuildSettings.Tags.First(xr => xr.Tag.ToLower() == tag).Output));
					return;
				}

				List<DiscordUrieTag> Contained = GuildSettings.Tags.FindAll(xr => xr.Tag.ToLower().Contains(tag));
				if (Contained.Count <= 0)
				{
					await ctx.RespondAsync("Tag not found.");
					return;
				}

				string Out = "Tag not found. Did you mean:\n";

				foreach(var cur in Contained)
				{
					Out += cur.Tag + "\n";
				}

				await ctx.RespondAsync(Out);
			}

			[Command("add"), Description("Add a tag"), Aliases("create")]
			public async Task TagAdd(CommandContext ctx, string tag, [RemainingText] string Output)
			{
				try
				{
					if (Output == null)
					{
						await ctx.RespondAsync("You're missing a parameter dingus.");
						return;
					}

					var GuildSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);
				
					tag = Regex.Escape(tag);
					Output = Regex.Escape(Output);
					if (GuildSettings.Tags.Any(xr => xr.Tag.ToLower() == tag.ToLower()))
					{
						await ctx.RespondAsync("This tag already exists!");
						return;
					}
				
					discordUrie.Config.GuildSettings.Remove(GuildSettings);
					GuildSettings.Tags.Add(new DiscordUrieTag(tag, Output, ctx.Member.Id));
					discordUrie.Config.GuildSettings.Add(GuildSettings);
					await GuildSettings.SaveGuild(discordUrie.SQLConn);
					await ctx.RespondAsync("Tag created!");
				}
				catch(Exception ex)
				{
					await ctx.RespondAsync($"You did something wrong. {ex.GetType()} : {ex.Message}");
				}
			}

			[Command("edit"), Description("Edit a tag")]
			public async Task TagEdit(CommandContext ctx, string tag, [RemainingText] string output)
			{
				tag = Regex.Escape(tag).ToLower();
				var GuildSettings = await this.discordUrie.Config.FindGuildSettings(ctx.Guild);
				if (!GuildSettings.Tags.Any(xr => xr.Tag.ToLower() == tag))
				{
					await ctx.RespondAsync("Tag doesn't exist!");
					return;
				}
				var Target = GuildSettings.Tags.First(xr => xr.Tag.ToLower() == tag);
				var util = new Util(this.discordUrie);
				if (Target.Owner != ctx.Member.Id && !await util.UserAuth(ctx.Member))
				{
					await ctx.RespondAsync("You do not own this tag and do not have the permissions to edit this.");
					return;
				}
				this.discordUrie.Config.GuildSettings.Remove(GuildSettings);
				GuildSettings.Tags.Remove(Target);
				Target.Output = output;
				GuildSettings.Tags.Add(Target);
				this.discordUrie.Config.GuildSettings.Add(GuildSettings);
				await GuildSettings.SaveGuild(this.discordUrie.SQLConn);
				await ctx.RespondAsync("Tag edited!");
			}

			[Command("remove"), Description("Remove a tag")]
			public async Task TagRemove(CommandContext ctx, [RemainingText] string tag)
			{
				try
				{
					tag = Regex.Escape(tag).ToLower();
					var GuildSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);


					if(!GuildSettings.Tags.Any(xr => xr.Tag.ToLower() == tag))
					{
						await ctx.RespondAsync("Tag doesn't exist!");
						return;
					}
					DiscordUrieTag Target = GuildSettings.Tags.First(xr => xr.Tag.ToLower() == tag);
					var util = new Util(discordUrie);
					if(Target.Owner != ctx.Member.Id && !await util.UserAuth(ctx.Member))
					{
						await ctx.RespondAsync("You do not have the permissions to do this!");
						return;
					}
					discordUrie.Config.GuildSettings.Remove(GuildSettings);
					GuildSettings.Tags.Remove(Target);
					discordUrie.Config.GuildSettings.Add(GuildSettings);
					await GuildSettings.SaveGuild(discordUrie.SQLConn);
					await ctx.RespondAsync("Tag removed!");
				}
				catch(Exception ex)
				{
					await ctx.RespondAsync($"You did something wrong. {ex.GetType()} : {ex.Message}");
				}
			}

			[Command("list"), Description("List all tags")]
			public async Task TagList(CommandContext ctx)
			{
				InteractivityExtension intex = ctx.Client.GetInteractivity();
				List<DiscordUrieTag> tags = await discordUrie.Config.GetTags(ctx.Guild);
				if (tags.Count <= 0) return;
				IEnumerable<string> TagKeys = tags.Select(xr => xr.Tag);

				string EditedTags = String.Join("\n", TagKeys);


				await intex.SendPaginatedMessageAsync(ctx.Channel, ctx.User, intex.GeneratePagesInEmbed(EditedTags, SplitType.Line));
			}

			[Command("info"), Description("Gives information about a tag")]
			public async Task TagInfo(CommandContext ctx, [RemainingText] string tag)
			{
				var GuildSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);
				tag = Regex.Escape(tag).ToLower();
				var Tag = GuildSettings.Tags.First(xr => xr.Tag.ToLower() == tag);

				if (Tag == null)
				{
					await ctx.RespondAsync("Invalid tag");
					return;
				}

				DiscordUser TagOwner = await ctx.Client.GetUserAsync(Tag.Owner);
				DiscordEmbedBuilder EBuilder = new DiscordEmbedBuilder();
				EBuilder.WithAuthor($"{TagOwner.Username}#{TagOwner.Discriminator}", iconUrl: TagOwner.GetAvatarUrl(ImageFormat.Png));
				EBuilder.AddField("Tag Name", Tag.Tag);
				EBuilder.AddField("Owner Id", Tag.Owner.ToString());
				await ctx.RespondAsync(embed: EBuilder.Build());
			}

			[Command("import"), RequireAuth, Description("Imports tags from another guild")]
			public async Task Import(CommandContext ctx, [Description("Target guild.")] DiscordGuild guild )
			{
				if (guild == ctx.Guild)
				{
					await ctx.RespondAsync("Same guild, loser.");
				}

				var MergingSettings = await discordUrie.Config.FindGuildSettings(guild);
				if (MergingSettings.Tags.Count <= 0)
				{
					await ctx.RespondAsync("Target guild has no tags.");
					return;
				}
				var TargetSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);

				var DifferingTags = MergingSettings.Tags.FindAll(xr => !TargetSettings.Tags.Any(xa => xa.Tag == xr.Tag));
				discordUrie.Config.GuildSettings.Remove(TargetSettings);
				TargetSettings.Tags.AddRange(DifferingTags);
				discordUrie.Config.GuildSettings.Add(TargetSettings);
				await TargetSettings.SaveGuild(discordUrie.SQLConn);
				await ctx.RespondAsync("Tags imported.");
			}

			[Command("wipe"), RequireAuthHigh, Description("Wipes all of the current guild's tags.")]
			public async Task Wipe(CommandContext ctx)
			{
				var inter = ctx.Client.GetInteractivity();
				await ctx.RespondAsync("Wipe all tags in this guild? This cannot be undone. (y/n)");
				var result = await inter.WaitForMessageAsync(xr => xr.Author == ctx.User && xr.Content.ToLower() == "y" || xr.Content.ToLower() == "n");
				if (result.TimedOut)
				{
					await ctx.RespondAsync("Timed out.");
					return;
				}
				if (result.Result.Content.ToLower() != "y")
				{
					await ctx.RespondAsync("Aborted.");
					return;
				}

				var GuildSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);
				discordUrie.Config.GuildSettings.Remove(GuildSettings);
				GuildSettings.Tags = new List<DiscordUrieTag>();
				discordUrie.Config.GuildSettings.Add(GuildSettings);
				await GuildSettings.SaveGuild(discordUrie.SQLConn);
				await ctx.RespondAsync("Tags wiped.");
			}
		}

	}
}