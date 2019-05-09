using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using static DiscordUrie_DSharpPlus.DiscordUrieSettings;

namespace DiscordUrie_DSharpPlus
{
	public partial class Commands : BaseCommandModule
	{
		[Group("bans")]
		[Description("Chat bans `Unusable unless you're cool`")]
		[Aliases("b")]
		public class ChatBansGroup : BaseCommandModule
		{
			[Command("on")]
			[Description("Enables chat bans `only works if you're super cool`")]
			public async Task Enable(CommandContext ctx)
			{

				if (!Util.UserAuth(ctx.Member.Id, ctx.Guild))
				{
					DiscordMessage a = await ctx.RespondAsync("Incorrect permissions!");
					await Task.Delay(2070);
					await ctx.Message.DeleteAsync("Command auto deletion.");
					await a.DeleteAsync("Command auto deletion.");
					return;
				}

				DiscordUrieGuild GuildSettings = Entry.Settings.FindGuildSettings(ctx.Guild);

				if (GuildSettings.CBSettings.Enabled)
				{
					DiscordMessage woah = await ctx.RespondAsync("Chat bans are already enabled!");
					await Task.Delay(2070);
					await ctx.Message.DeleteAsync("Command auto deletion.");
					await woah.DeleteAsync("Command auto deletion.");
				}
				else
				{
					Entry.Settings.GuildSettings.Remove(GuildSettings);
					GuildSettings.CBSettings = new ChatBanSettings()
					{
						BannedIds = GuildSettings.CBSettings.BannedIds,
						Enabled = true,
					};
					Entry.Settings.GuildSettings.Add(GuildSettings);
					Entry.Settings.SaveSettings();

					DiscordMessage woaaah = await ctx.RespondAsync("Chat bans enabled...");
					await Task.Delay(2070);
					await ctx.Message.DeleteAsync("Command auto deletion.");
					await woaaah.DeleteAsync("Command auto deletion.");
				}

			}

			[Command("off")]
			[Description("Disables chat bans `only works if you're super cool`")]
			public async Task Disable(CommandContext ctx)
			{

				if (!Util.UserAuth(ctx.Member.Id, ctx.Guild))
				{
					DiscordMessage a = await ctx.RespondAsync("Incorrect permissions!");
					await Task.Delay(2070);
					await ctx.Message.DeleteAsync("Command auto deletion.");
					await a.DeleteAsync("Command auto deletion.");
					return;
				}
				DiscordUrieGuild GuildSettings = Entry.Settings.FindGuildSettings(ctx.Guild);

				if (GuildSettings.CBSettings.Enabled)
				{
					Entry.Settings.GuildSettings.Remove(GuildSettings);
					GuildSettings.CBSettings = new ChatBanSettings()
					{
						BannedIds = GuildSettings.CBSettings.BannedIds,
						Enabled = false,
					};
					Entry.Settings.GuildSettings.Add(GuildSettings);
					Entry.Settings.SaveSettings();

					DiscordMessage ImSoTired = await ctx.RespondAsync("Chat bans disabled...");
					await Task.Delay(2070);
					await ctx.Message.DeleteAsync("Command auto deletion.");
					await ImSoTired.DeleteAsync("Command auto deletion.");

				}
				else
				{
					DiscordMessage a = await ctx.RespondAsync("Incorrect permissions!");
					await Task.Delay(2070);
					await ctx.Message.DeleteAsync("Command auto deletion.");
					await a.DeleteAsync("Command auto deletion.");
				}
			}

			[Command("add")]
			[Description("Adds a user to the ban list `only works if you're super cool`")]
			public async Task AddBan(CommandContext ctx, [Description("The user id to add")] DiscordMember user)
			{

				if (!Util.UserAuth(ctx.Member.Id, ctx.Guild))
				{
					DiscordMessage a = await ctx.RespondAsync("Incorrect permissions!");
					await Task.Delay(2070);
					await ctx.Message.DeleteAsync("Command auto deletion.");
					await a.DeleteAsync("Command auto deletion.");
					return;
				}

				bool success = Util.AddBan(ctx.Client, user.Id, ctx.Guild, out Exception ex);

				if (ex != null)
				{
					DiscordMessage OhThanksYes = await ctx.RespondAsync($"Something went wrong! {ex.Message}");
					await Task.Delay(2070);
					await ctx.Message.DeleteAsync("Command auto deletion.");
					await OhThanksYes.DeleteAsync("Command auto deletion.");
				}


				if (success == true)
				{
					DiscordMessage IWishIWasDeadThanks = await ctx.RespondAsync("Id added sucessfully.");
					await Task.Delay(2070);
					await ctx.Message.DeleteAsync("Command auto deletion.");
					await IWishIWasDeadThanks.DeleteAsync("Command auto deletion.");
				}
				else
				{
					DiscordMessage IReallyWishIWasDeadThankYou = await ctx.RespondAsync("Id already in list...");
					await Task.Delay(2070);
					await ctx.Message.DeleteAsync("Command auto deletion.");
					await IReallyWishIWasDeadThankYou.DeleteAsync("Command auto deletion.");
				}

			}

			[Command("remove")]
			[Description("Removes a user from the ban list `only works if you're super cool`")]
			public async Task RemoveBan(CommandContext ctx, [Description("The user id to remove")] DiscordMember user)
			{
				if (!Util.UserAuth(ctx.Member.Id, ctx.Guild))
				{
					DiscordMessage a = await ctx.RespondAsync("Incorrect permissions!");
					await Task.Delay(2070);
					await ctx.Message.DeleteAsync("Command auto deletion.");
					await a.DeleteAsync("Command auto deletion.");
					return;
				}


				bool success = Util.RemoveBan(ctx.Client, user.Id, ctx.Guild, out Exception ex);
				if (ex != null)
				{
					DiscordMessage OhThanksYes = await ctx.RespondAsync($"Something went wrong! {ex.Message}");
					await Task.Delay(2070);
					await ctx.Message.DeleteAsync("Command auto deletion.");
					await OhThanksYes.DeleteAsync("Command auto deletion.");
					return;
				}

				if (success == true)
				{
					DiscordMessage IWishIWasDeadThanks = await ctx.RespondAsync("Id removed sucessfully.");
					await Task.Delay(2070);
					await ctx.Message.DeleteAsync("Command auto deletion.");
					await IWishIWasDeadThanks.DeleteAsync("Command auto deletion.");
				}
				else
				{
					DiscordMessage IReallyWishIWasDeadThankYou = await ctx.RespondAsync("Id not in list...");
					await Task.Delay(2070);
					await ctx.Message.DeleteAsync("Command auto deletion.");
					await IReallyWishIWasDeadThankYou.DeleteAsync("Command auto deletion.");
				}

			}

			[Command("check")]
			[Description("Displays all the currently chat banned users")]
			public async Task BanCheck(CommandContext ctx)
			{
				List<ulong> idlist = Entry.Settings.GetChatBanIdList(ctx.Guild);
				string CurMessage = "The users that are currently in the chat bans list are: ";
				int IHateMyLife = 0;

				foreach (ulong cur in idlist)
				{

					DiscordUser CurUser = await ctx.Guild.GetMemberAsync(cur);


					if (CurUser != null)
					{
						if (IHateMyLife == 0)
						{
							CurMessage += $"`{CurUser.Username}#{CurUser.Discriminator}`";
						}
						else
						{
							CurMessage += $", `{CurUser.Username}#{CurUser.Discriminator}`";
						}
					}
					else
					{
						if (IHateMyLife == 0)
						{
							CurMessage += $"`{cur}`";
						}
						else
						{
							CurMessage += $", `{cur}`";
						}
					}

					IHateMyLife++;
				}

				if (CurMessage == "The users that are currently in the chat bans list are: ")
				{
					await ctx.RespondAsync("No users are currently chat banned.");
				}
				else
				{
					await ctx.RespondAsync(CurMessage);
				}
			}
		}

	}

}