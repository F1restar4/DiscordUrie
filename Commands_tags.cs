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

namespace DiscordUrie_DSharpPlus
{
    public partial class Commands : BaseCommandModule
    {

        [Group("tag"), Description("I dunno some gay shit")]
        public class Tag : BaseCommandModule
        {
            private DiscordUrie discordUrie { get; set; }
            public Tag(DiscordUrie du)
            {
                discordUrie = du;
            }

            [GroupCommand()]
            public async Task GroupExecute(CommandContext ctx, [RemainingText] string tag)
            {
                var GuildSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);

                tag = Regex.Escape(tag).ToLower();

                if (GuildSettings.Tags.Any(xr => xr.Tag.ToLower() == tag))
                {
                    await ctx.RespondAsync(Regex.Unescape(GuildSettings.Tags.First(xr => xr.Tag.ToLower() == tag).Output));
                    return;
                }

                var Contained = GuildSettings.Tags.FindAll(xr => xr.Tag.ToLower().Contains(tag));
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
                    var GuildSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);
                
                    tag = Regex.Escape(tag);
                    Output = Regex.Escape(Output);
                    if (GuildSettings.Tags.Any(xr => xr.Tag.ToLower() == tag.ToLower()))
                    {
                        await ctx.RespondAsync("This tag already exists!");
                        return;
                    }
                
                    discordUrie.Config.GuildSettings.Remove(GuildSettings);

                    GuildSettings.Tags.Add(new DiscordUrieSettings.DiscordUrieTag
                    {
                        Tag = tag,
                        Output = Output,
                        Owner = ctx.Member.Id
                    });
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
                var GuildSettings = await this.discordUrie.Config.FindGuildSettings(ctx.Guild).ConfigureAwait(false);
                if (!GuildSettings.Tags.Any(xr => xr.Tag.ToLower() == tag))
                {
                    await ctx.RespondAsync("Tag doesn't exist!").ConfigureAwait(false);
                    return;
                }
                var Target = GuildSettings.Tags.First(xr => xr.Tag.ToLower() == tag);
                var util = new Util(this.discordUrie);
                if (Target.Owner != ctx.Member.Id && !await util.UserAuth(ctx.Member))
                {
                    await ctx.RespondAsync("You do not own this tag and do not have the permissions to edit this.").ConfigureAwait(false);
                    return;
                }
                this.discordUrie.Config.GuildSettings.Remove(GuildSettings);
                GuildSettings.Tags.Remove(Target);
                Target.Output = output;
                GuildSettings.Tags.Add(Target);
                this.discordUrie.Config.GuildSettings.Add(GuildSettings);
                await GuildSettings.SaveGuild(this.discordUrie.SQLConn).ConfigureAwait(false);
                await ctx.RespondAsync("Tag edited!").ConfigureAwait(false);
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
                    DiscordUrieSettings.DiscordUrieTag Target = GuildSettings.Tags.First(xr => xr.Tag.ToLower() == tag);
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
                List<DiscordUrieSettings.DiscordUrieTag> tags = await discordUrie.Config.GetTags(ctx.Guild);
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
        }

    }
}