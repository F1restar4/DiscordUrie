using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Drawing;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DiscordUrie_DSharpPlus.Attributes;
using Microsoft.Extensions.Logging;

namespace DiscordUrie_DSharpPlus
{
	public partial class Commands : ApplicationCommandModule
	{
		[SlashCommandGroup("color", "Allows users to modify their displayed color as desired.")]
		public class ColoringStuffGroup : ApplicationCommandModule
		{
			private DiscordUrie discordUrie { get; set; }
			public ColoringStuffGroup(DiscordUrie du)
			{
				discordUrie = du;
			}

			public async Task RemoveColor(DiscordUser user, DiscordGuild server, DiscordChannel channel)
			{
				IReadOnlyDictionary<ulong, DiscordRole> roles = server.Roles;

				if (!roles.Any(xr => xr.Value.Name == user.Id.ToString()))
				{
					await channel.SendMessageAsync("There was no color to remove!");
					return;
				}

				DiscordRole cur = roles.First(xr => xr.Value.Name == user.Id.ToString()).Value;
				await cur.DeleteAsync("Color deletion");

				Console.WriteLine("User {0}'s color was removed.", user.Username);
				return;
			}

			public async Task SetColor(DiscordMember user, DiscordGuild server, DiscordChannel channel, DiscordColor color)
			{
				IReadOnlyDictionary<ulong, DiscordRole> roles = server.Roles;
				DiscordRole existingRole = null;
				string UserId = user.Id.ToString();

				if (roles.Any(xr => xr.Value.Name == UserId))
				{
					existingRole = roles.First(xr => xr.Value.Name == UserId).Value;
					await existingRole.ModifyAsync(color: color, reason: "Coloring role edit");
				}
				else
				{
					DiscordRole CreatedRole = await server.CreateRoleAsync(user.Id.ToString(), Permissions.None, color, null, null, "Coloring role creation");

					await user.GrantRoleAsync(CreatedRole, "Coloring role assignment");
				}

				Console.WriteLine("User {0}'s color was set to {1}", user.Username, color.R + ", " + color.G + ", " + color.B);
				return;
			}

			public Task<DiscordColor> GetColor(DiscordMember user, DiscordGuild server)
			{
				IReadOnlyDictionary<ulong, DiscordRole> roleList = server.Roles;
				string UserId = user.Id.ToString();

				if (!roleList.Any(xr => xr.Value.Name == UserId)) return null;
				return Task.FromResult(roleList.First(xr => xr.Value.Name == UserId).Value.Color);
			}

			[SlashCommand("set", "Sets the target's color, you're allowed to set your own color usually"), ColorCommand]
			public async Task ExecuteGroupAsync(InteractionContext ctx, [Option("TargetUser", "The ID or mention of the target user")] DiscordUser user, [Option("Color", "The color to set to, can be in hex, rgb or keyword format (Must be a single string)")] string Color)
			{
				try
				{
					var util = new Util(discordUrie);
					if (user != ctx.Member && !await util.UserAuth(ctx.Member))
					{
						await ctx.CreateResponseAsync("You don't have the correct permissions for this!");
						return;
					}

					if (Color == "off")
					{
						await RemoveColor(user, ctx.Guild, ctx.Channel);
						await ctx.CreateResponseAsync("Color removed.");
						return;
					}

					System.Drawing.Color col = ColorTranslator.FromHtml(Color);
					DiscordColor RoleColor = new DiscordColor(col.R, col.G, col.B);

					await SetColor((DiscordMember)user, ctx.Guild, ctx.Channel, RoleColor);
					await ctx.CreateResponseAsync("Color set successfully");
				}
				catch (Exception ex)
				{
					await ctx.CreateResponseAsync($"Something went wrong! {ex.Message}");
					ctx.Client.Logger.Log(LogLevel.Error, ex, "Error in setting color");
				}
			}

			[SlashCommand("purge", "Removes unused color roles"), ColorCommand]
			public async Task PurgeInactiveColors(InteractionContext ctx)
			{
				int i = 0;
				foreach (DiscordRole cur in ctx.Guild.Roles.Values)
				{
					bool Success = ulong.TryParse(cur.Name, out ulong ID);
					if (!Success)
						continue;

					DiscordUser user = null;

					try
					{
						user = await ctx.Client.GetUserAsync(ID);
						if (user == null)
							continue;
					}
					catch
					{
						continue;
					}

					try
					{
						DiscordMember member = await ctx.Guild.GetMemberAsync(ID);
						if (member != null)
							continue;
					}
					catch
					{
						await cur.DeleteAsync("Color role purge.");
						i++;
						continue;
					}

				}

				await ctx.CreateResponseAsync($"Removed {i} unused color roles.");

			}

			[SlashCommand("me", "Sets your color"), ColorCommand]
			public async Task SetSelfColor(InteractionContext ctx, [Option("color", "The color to set to, can be in hex, rgb or keyword format (Must be a single string)")] string Color)
			{
				try
				{
					if (Color == "off")
					{
						await RemoveColor(ctx.User, ctx.Guild, ctx.Channel);
						await ctx.CreateResponseAsync("Color removed.");
					}
					else
					{
						System.Drawing.Color col = ColorTranslator.FromHtml(Color);
						DiscordColor RoleColor = new DiscordColor(col.R, col.G, col.B);
						await SetColor(ctx.Member, ctx.Guild, ctx.Channel, RoleColor);
						await ctx.CreateResponseAsync("Color set successfully.");
					}
				}
				catch (Exception ex)
				{
					await ctx.CreateResponseAsync($"Something went wrong! {ex.Message}");
					ctx.Client.Logger.Log(LogLevel.Error, $"[Discord Urie] Error in setting color, {ex.Message}");
				}
			}

			[SlashCommand("get", "Displays a givin user's current color")]
			public async Task GetColor(InteractionContext ctx, [Option("User", "The user to get the color of")] DiscordUser User)
			{
				try
				{
					var GuildSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);
					DiscordColor? UserColor = await GetColor((DiscordMember)User, ctx.Guild);
					if (UserColor != null)
						await ctx.CreateResponseAsync($"{User.Username}'s current color is {UserColor} or {UserColor.Value.R}, {UserColor.Value.G}, {UserColor.Value.B}");
					else
						await ctx.CreateResponseAsync($"{User.Username} does not have a color set.");
				}
				catch (Exception ex)
				{
					await ctx.CreateResponseAsync($"Something went wrong! {ex.Message}");
					ctx.Client.Logger.Log(LogLevel.Error, $"Error getting user's color; {ex.Message}");
				}
			}

			[SlashCommand("block", "Adds a user to the color commmand black/whitelist"), RequireAuth]
			public async Task BlockAdd(InteractionContext ctx, [Option("User", "The user to add to the black/whitelist")] DiscordUser Member)
			{
				var GuildSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);
				if (GuildSettings.ColorBlacklist.Any(xr => xr == Member.Id))
				{
					await ctx.CreateResponseAsync("They're already in the list!");
					return;
				}
				GuildSettings.ColorBlacklist.Add(Member.Id);
				await GuildSettings.SaveGuild(discordUrie.SQLConn);
				await ctx.CreateResponseAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
			}

			[SlashCommand("unblock", "Removes a user from the color command black/whitelist"), RequireAuth]
			public async Task BlockRemove(InteractionContext ctx, [Option("User", "The user to remove from the black/whitelist")] DiscordUser Member)
			{
				var GuildSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);
				if (!GuildSettings.ColorBlacklist.Any(xr => xr == Member.Id))
				{
					await ctx.CreateResponseAsync("They're not in the list!");
					return;
				}
				GuildSettings.ColorBlacklist.Remove(Member.Id);
				await GuildSettings.SaveGuild(discordUrie.SQLConn);
				await ctx.CreateResponseAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
			}

			[SlashCommand("mode", "Switches the blacklist mode between off, blacklist and whitelist 0-2"), RequireAuth]
			public async Task BlockMode(InteractionContext ctx, [Option("Mode", "Blacklist mode")] BlackListModeEnum Mode)
			{
				var GuildSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);
				var ListMode = (BlackListModeEnum)Mode;
				if (GuildSettings.ColorBlacklistMode == ListMode) return;
				GuildSettings.ColorBlacklistMode = ListMode;
				await GuildSettings.SaveGuild(discordUrie.SQLConn);
				await ctx.CreateResponseAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
			}

			[SlashCommand("toggle", "Enables and disables use of any color commands"), RequireAuth]
			public async Task Toggle(InteractionContext ctx)
			{
				var GuildSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);
				if (GuildSettings.ColorEnabled)
				{
					GuildSettings.ColorEnabled = false;
					await GuildSettings.SaveGuild(discordUrie.SQLConn);
					await ctx.CreateResponseAsync("Color commands disabled.");
					return;
				}
				else
				{
					GuildSettings.ColorEnabled = true;
					await GuildSettings.SaveGuild(discordUrie.SQLConn);
					await ctx.CreateResponseAsync("Color commands enabled.");
					return;
				}
			}
		}
	}
}