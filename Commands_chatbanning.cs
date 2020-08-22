using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DiscordUrie_DSharpPlus.Attributes;

namespace DiscordUrie_DSharpPlus
{
	public partial class Commands : BaseCommandModule
	{
		[Group("bans"), Aliases("b"), Description("Chat bans"), RequireAuth]
		public class ChatBansGroup : BaseCommandModule
		{
			private DiscordUrie discordUrie { get; set; }
			public ChatBansGroup(DiscordUrie du)
			{
				discordUrie = du;
			}

			[Command("on"), Description("Enables chat bans")]
			public async Task Enable(CommandContext ctx)
			{
				var GuildSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);

				if (GuildSettings.BansEnabled)
				{
					DiscordMessage woah = await ctx.RespondAsync("Chat bans are already enabled!");
					await Task.Delay(2070);
					await ctx.Message.DeleteAsync("Command auto deletion.");
					await woah.DeleteAsync("Command auto deletion.");
				}
				else
				{
					discordUrie.Config.GuildSettings.Remove(GuildSettings);
					GuildSettings.BansEnabled = true;
					discordUrie.Config.GuildSettings.Add(GuildSettings);
					await GuildSettings.SaveGuild(discordUrie.SQLConn);

					DiscordMessage woaaah = await ctx.RespondAsync("Chat bans enabled...");
					await Task.Delay(2070);
					await ctx.Message.DeleteAsync("Command auto deletion.");
					await woaaah.DeleteAsync("Command auto deletion.");
				}
			}

			[Command("off"), Description("Disables chat bans")]
			public async Task Disable(CommandContext ctx)
			{
				var GuildSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);

				if (GuildSettings.BansEnabled)
				{
					discordUrie.Config.GuildSettings.Remove(GuildSettings);
					GuildSettings.BansEnabled = false;

					discordUrie.Config.GuildSettings.Add(GuildSettings);
					await GuildSettings.SaveGuild(discordUrie.SQLConn);

					DiscordMessage ImSoTired = await ctx.RespondAsync("Chat bans disabled...");
					await Task.Delay(2070);
					await ctx.Message.DeleteAsync("Command auto deletion.");
					await ImSoTired.DeleteAsync("Command auto deletion.");

				}
				else
				{
					DiscordMessage a = await ctx.RespondAsync("Chat bans are already disabled!");
					await Task.Delay(2070);
					await ctx.Message.DeleteAsync("Command auto deletion.");
					await a.DeleteAsync("Command auto deletion.");
				}
			}

			[Command("add"), Description("Adds a user to the ban list")]
			public async Task AddBan(CommandContext ctx, [Description("The user id to add")] DiscordMember user)
			{
				var util = new Util(discordUrie);
				bool success = await util.AddBan(ctx.Client, user.Id, ctx.Guild);

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

			[Command("remove"), Description("Removes a user from the ban list")]
			public async Task RemoveBan(CommandContext ctx, [Description("The user id to remove")] DiscordMember user)
			{
				var util = new Util(discordUrie);
				bool success = await util.RemoveBan(ctx.Client, user.Id, ctx.Guild);

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

			[Command("check"), Description("Displays all the currently chat banned users")]
			public async Task BanCheck(CommandContext ctx)
			{
				List<ulong> idlist = await discordUrie.Config.GetChatBanIdList(ctx.Guild);
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