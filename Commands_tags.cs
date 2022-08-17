using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DiscordUrie_DSharpPlus.Attributes;

namespace DiscordUrie_DSharpPlus
{
	public partial class Commands : ApplicationCommandModule
	{

		[SlashCommandGroup("tag", "Allows a user to store a message for later recall")]
		public class Tag : ApplicationCommandModule
		{
			private DiscordUrie discordUrie { get; set; }
			public Tag(DiscordUrie du)
			{
				discordUrie = du;
			}

			[SlashCommand("get", "Recalls a tag using it's name.")]
			public async Task GroupExecute(InteractionContext ctx, [Option("tag", "The name of the tag")] string tag)
			{
				await ctx.DeferAsync();
				var GuildSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);

				tag = Regex.Escape(tag).ToLower();

				if (GuildSettings.Tags.Any(xr => xr.Tag.ToLower() == tag))
				{
					var builder = new DiscordWebhookBuilder().WithContent(Regex.Unescape(GuildSettings.Tags.First(xr => xr.Tag.ToLower() == tag).Output));
					await ctx.EditResponseAsync(builder);
					return;
				}

				List<DiscordUrieTag> Contained = GuildSettings.Tags.FindAll(xr => xr.Tag.ToLower().Contains(tag));
				if (Contained.Count <= 0)
				{
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Tag not found."));
					return;
				}

				string Out = "Tag not found. Did you mean:\n";

				foreach (var cur in Contained)
				{
					Out += cur.Tag + "\n";
				}

				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(Out));
			}

			[SlashCommand("Create", "Create a tag")]
			public async Task TagCreate(InteractionContext ctx, [Option("tag", "The name of the tag")] string tag, [Option("output", "The output of the tag")] string Output)
				=> await TagAdd(ctx, tag, Output);

			[SlashCommand("add", "Add a tag")]
			public async Task TagAdd(InteractionContext ctx, [Option("tag", "The name of the tag")] string tag, [Option("output", "The output of the tag")] string Output)
			{
				try
				{
					if (Output == null)
					{
						await ctx.CreateResponseAsync("You're missing a parameter dingus.");
						return;
					}

					var GuildSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);

					tag = Regex.Escape(tag);
					Output = Regex.Escape(Output);
					if (GuildSettings.Tags.Any(xr => xr.Tag.ToLower() == tag.ToLower()))
					{
						await ctx.CreateResponseAsync("This tag already exists!");
						return;
					}

					discordUrie.Config.GuildSettings.Remove(GuildSettings);
					GuildSettings.Tags.Add(new DiscordUrieTag(tag, Output, ctx.Member.Id));
					discordUrie.Config.GuildSettings.Add(GuildSettings);
					await GuildSettings.SaveGuild(discordUrie.SQLConn);
					await ctx.CreateResponseAsync("Tag created!");
				}
				catch (Exception ex)
				{
					await ctx.CreateResponseAsync($"You did something wrong. {ex.GetType()} : {ex.Message}");
				}
			}

			[SlashCommand("edit", "Edit a tag")]
			public async Task TagEdit(InteractionContext ctx, [Option("tag", "The name of the tag")] string tag, [Option("output", "The output of the tag")] string output)
			{
				tag = Regex.Escape(tag).ToLower();
				var GuildSettings = await this.discordUrie.Config.FindGuildSettings(ctx.Guild);
				if (!GuildSettings.Tags.Any(xr => xr.Tag.ToLower() == tag))
				{
					await ctx.CreateResponseAsync("Tag doesn't exist!");
					return;
				}
				var Target = GuildSettings.Tags.First(xr => xr.Tag.ToLower() == tag);
				var util = new Util(this.discordUrie);
				if (Target.Owner != ctx.Member.Id && !await util.UserAuth(ctx.Member))
				{
					await ctx.CreateResponseAsync("You do not own this tag and do not have the permissions to edit this.");
					return;
				}
				this.discordUrie.Config.GuildSettings.Remove(GuildSettings);
				GuildSettings.Tags.Remove(Target);
				Target.Output = output;
				GuildSettings.Tags.Add(Target);
				this.discordUrie.Config.GuildSettings.Add(GuildSettings);
				await GuildSettings.SaveGuild(this.discordUrie.SQLConn);
				await ctx.CreateResponseAsync("Tag edited!");
			}

			[SlashCommand("remove", "Remove a tag")]
			public async Task TagRemove(InteractionContext ctx, [Option("tag", "The tag to delete")] string tag)
			{
				try
				{
					tag = Regex.Escape(tag).ToLower();
					var GuildSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);


					if (!GuildSettings.Tags.Any(xr => xr.Tag.ToLower() == tag))
					{
						await ctx.CreateResponseAsync("Tag doesn't exist!");
						return;
					}
					DiscordUrieTag Target = GuildSettings.Tags.First(xr => xr.Tag.ToLower() == tag);
					var util = new Util(discordUrie);
					if (Target.Owner != ctx.Member.Id && !await util.UserAuth(ctx.Member))
					{
						await ctx.CreateResponseAsync("You do not have the permissions to do this!");
						return;
					}
					discordUrie.Config.GuildSettings.Remove(GuildSettings);
					GuildSettings.Tags.Remove(Target);
					discordUrie.Config.GuildSettings.Add(GuildSettings);
					await GuildSettings.SaveGuild(discordUrie.SQLConn);
					await ctx.CreateResponseAsync("Tag removed!");
				}
				catch (Exception ex)
				{
					await ctx.CreateResponseAsync($"You did something wrong. {ex.GetType()} : {ex.Message}");
				}
			}

			[SlashCommand("list", "List all tags")]
			public async Task TagList(InteractionContext ctx)
			{
				InteractivityExtension intex = ctx.Client.GetInteractivity();
				List<DiscordUrieTag> tags = await discordUrie.Config.GetTags(ctx.Guild);
				if (tags.Count <= 0) return;
				IEnumerable<string> TagKeys = tags.Select(xr => xr.Tag);

				string EditedTags = String.Join("\n", TagKeys);
				await intex.SendPaginatedResponseAsync(ctx.Interaction, false, ctx.User, intex.GeneratePagesInEmbed(EditedTags, DSharpPlus.Interactivity.Enums.SplitType.Line));
			}

			[SlashCommand("info", "Gives information about a tag")]
			public async Task TagInfo(InteractionContext ctx, [Option("tag", "The name of the tag")] string tag)
			{
				var GuildSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);
				tag = Regex.Escape(tag).ToLower();
				var Tag = GuildSettings.Tags.First(xr => xr.Tag.ToLower() == tag);

				if (Tag == null)
				{
					await ctx.CreateResponseAsync("Invalid tag");
					return;
				}

				DiscordUser TagOwner = await ctx.Client.GetUserAsync(Tag.Owner);
				DiscordEmbedBuilder EBuilder = new DiscordEmbedBuilder();
				EBuilder.WithAuthor($"{TagOwner.Username}#{TagOwner.Discriminator}", iconUrl: TagOwner.GetAvatarUrl(ImageFormat.Png));
				EBuilder.AddField("Tag Name", Tag.Tag);
				EBuilder.AddField("Owner Id", Tag.Owner.ToString());
				await ctx.CreateResponseAsync(embed: EBuilder.Build());
			}

			[SlashCommand("import", "Imports tags from another guild"), RequireAuth]
			public async Task Import(InteractionContext ctx, [Option("guild", "Target guild.")] string guild)
			{
				await ctx.DeferAsync();
				var TargetGuild = await ctx.Client.GetGuildAsync(Convert.ToUInt64(guild));
				if (TargetGuild == null)
				{
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Invalid guild"));
					return;
				}
				if (TargetGuild == ctx.Guild)
				{
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Same guild, loser."));
					return;
				}

				var MergingSettings = await discordUrie.Config.FindGuildSettings(TargetGuild);
				if (MergingSettings.Tags.Count <= 0)
				{
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Target guild has no tags."));
					return;
				}
				var TargetSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);

				var DifferingTags = MergingSettings.Tags.FindAll(xr => !TargetSettings.Tags.Any(xa => xa.Tag == xr.Tag));
				if (DifferingTags.Count == 0)
				{
					await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("There's no new tags to add."));
					return;
				}
				discordUrie.Config.GuildSettings.Remove(TargetSettings);
				TargetSettings.Tags.AddRange(DifferingTags);
				discordUrie.Config.GuildSettings.Add(TargetSettings);
				await TargetSettings.SaveGuild(discordUrie.SQLConn);
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Tags imported."));
			}

			[SlashCommand("wipe", "Wipes all of the current guild's tags."), RequireAuthHigh]
			public async Task Wipe(InteractionContext ctx)
			{
				var inter = ctx.Client.GetInteractivity();
				await ctx.CreateResponseAsync("Wipe all tags in this guild? This cannot be undone. (y/n)");
				var result = await inter.WaitForMessageAsync(xr => xr.Author == ctx.User && xr.Content.ToLower() == "y" || xr.Content.ToLower() == "n");
				if (result.TimedOut)
				{
					await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Timed out."));
					return;
				}
				if (result.Result.Content.ToLower() != "y")
				{
					await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Aborted."));
					return;
				}

				var GuildSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);
				discordUrie.Config.GuildSettings.Remove(GuildSettings);
				GuildSettings.Tags = new List<DiscordUrieTag>();
				discordUrie.Config.GuildSettings.Add(GuildSettings);
				await GuildSettings.SaveGuild(discordUrie.SQLConn);
				await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Tags wiped."));
			}
		}

	}
}