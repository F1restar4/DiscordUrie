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
using static DiscordUrie_DSharpPlus.DiscordUrieSettings;

namespace DiscordUrie_DSharpPlus
{
    public partial class Commands : BaseCommandModule
    {

        [Group("tag"), Description("I dunno some gay shit")]
        public class Tag : BaseCommandModule
        {

            [GroupCommand()]
            public async Task GroupExecute(CommandContext ctx, [RemainingText] string tag)
            {
                DiscordUrieGuild GuildSettings = await Entry.Settings.FindGuildSettings(ctx.Guild);

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

            [Command("add"), Description("Add a tag")]
            public async Task TagAdd(CommandContext ctx, string tag, [RemainingText] string Output)
            {
                try
                {
                    DiscordUrieGuild GuildSettings = await Entry.Settings.FindGuildSettings(ctx.Guild);
                
                    tag = Regex.Escape(tag);
                    Output = Regex.Escape(Output);
                    if (GuildSettings.Tags.Any(xr => xr.Tag.ToLower() == tag.ToLower()))
                    {
                        await ctx.RespondAsync("This tag already exists!");
                        return;
                    }
                
                    Entry.Settings.GuildSettings.Remove(GuildSettings);

                    GuildSettings.Tags.Add(new DiscordUrieTag
                    {
                        Tag = tag,
                        Output = Output,
                        Owner = ctx.Member.Id
                    });
                    Entry.Settings.GuildSettings.Add(GuildSettings);
                    await Entry.Settings.SaveSettings(Entry.SQLConn);
                    await ctx.RespondAsync("Tag created!");
                }
                catch(Exception ex)
                {
                    await ctx.RespondAsync($"You did something wrong. {ex.GetType()} : {ex.Message}");
                }
            }

            [Command("remove"), Description("Remove a tag")]
            public async Task TagRemove(CommandContext ctx, [RemainingText] string tag)
            {
                try
                {
                    tag = Regex.Escape(tag).ToLower();
                    DiscordUrieGuild GuildSettings = await Entry.Settings.FindGuildSettings(ctx.Guild);


                    if(!GuildSettings.Tags.Any(xr => xr.Tag.ToLower() == tag))
                    {
                        await ctx.RespondAsync("Tag doesn't exist!");
                        return;
                    }
                    DiscordUrieTag Target = GuildSettings.Tags.First(xr => xr.Tag.ToLower() == tag);
                    if(Target.Owner != ctx.Member.Id && !await Util.UserAuth(ctx.Member.Id, ctx.Guild))
                    {
                        await ctx.RespondAsync("You do not have the permissions to do this!");
                        return;
                    }
                    Entry.Settings.GuildSettings.Remove(GuildSettings);
                    GuildSettings.Tags.Remove(Target);
                    Entry.Settings.GuildSettings.Add(GuildSettings);
                    await Entry.Settings.SaveSettings(Entry.SQLConn);
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
                List<DiscordUrieTag> tags = await Entry.Settings.GetTags(ctx.Guild);
                IEnumerable<string> TagKeys = tags.Select(xr => xr.Tag);

                string EditedTags = String.Join("\n", TagKeys);


                await intex.SendPaginatedMessageAsync(ctx.Channel, ctx.User, intex.GeneratePagesInEmbed(EditedTags, SplitType.Line));
            }

        }

    }
}