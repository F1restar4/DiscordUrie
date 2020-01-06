using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Drawing;
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
		[Group("color"), Description("Coloring group")]
		public class ColoringStuffGroup : BaseCommandModule
		{
			private DiscordUrie discordUrie { get; set; }
			public ColoringStuffGroup(DiscordUrie du)
			{
				discordUrie = du;
			}

			public async Task<bool> RemoveColor(DiscordUser user, DiscordGuild server, DiscordChannel channel, bool silent = false)
			{
				IReadOnlyDictionary<ulong, DiscordRole> roles = server.Roles;

				if (!roles.Any(xr => xr.Value.Name == user.Id.ToString()))
				{
					if (silent) return false;
					await channel.SendMessageAsync("There was no color to remove!");
					return false;
				}

				DiscordRole cur = roles.First(xr => xr.Value.Name == user.Id.ToString()).Value;
				await cur.DeleteAsync("Color deletion");

				await channel.SendMessageAsync("Color removed successfully.");
				Console.WriteLine("User {0}'s color was removed.", user.Username);
				return true;
			}

			public async Task<bool> SetColor(DiscordMember user, DiscordGuild server, DiscordChannel channel, DiscordColor color)
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

				await channel.SendMessageAsync("Color set successfully.");

				Console.WriteLine("User {0}'s color was set to {1}", user.Username, color.R + ", " + color.G + ", " + color.B);
				return true;
			}

			public Task<DiscordColor> GetColor(DiscordMember user, DiscordGuild server)
			{
				IReadOnlyDictionary<ulong, DiscordRole> roleList = server.Roles;
				string UserId = user.Id.ToString();

				if (!roleList.Any(xr => xr.Value.Name == UserId)) return null;
				return Task.FromResult(roleList.First(xr => xr.Value.Name == UserId).Value.Color);
			}

			[GroupCommand(), ColorCommand]
			[Description("Equal to calling color me or color set in one command, mentioning yourself will just run color me and will not require elevated perms")]
			public async Task ExecuteGroupAsync(CommandContext ctx, [Description("The ID or mention of the target user")] DiscordMember user, [Description("The color to set to, can be in hex, rgb or keyword format (Must be a single string)")] string Color)
			{
				try
				{
					var util = new Util(discordUrie);
					if (user != ctx.Member && !await util.UserAuth(ctx.Member))
					{
						await ctx.RespondAsync("You don't have the correct permissions for this!");
						return;
					}

					if (Color == "off")
					{
						await RemoveColor(user, ctx.Guild, ctx.Channel);
						return;
					}

					System.Drawing.Color col = ColorTranslator.FromHtml(Color);
					DiscordColor RoleColor = new DiscordColor(col.R, col.G, col.B);

					await SetColor(user, ctx.Guild, ctx.Channel, RoleColor);
				}
				catch (Exception ex)
				{
					await ctx.RespondAsync($"Something went wrong! {ex.Message}");


					ctx.Client.DebugLogger.LogMessage(LogLevel.Error, "Discord Urie", $"Error in setting color, {ex.Message}", DateTime.Now);
				}
			}

			[Command("purge"), ColorCommand]
			public async Task PurgeInactiveColors(CommandContext ctx)
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
						ctx.Client.DebugLogger.LogMessage(LogLevel.Debug, "DiscordUrie", $"CPurge: {user.Username}", DateTime.Now);
						i++;
						continue;
					}

				}

				await ctx.RespondAsync($"Removed {i} unused color roles.");

			}

			[Command("set"), RequireAuth, ColorCommand]
			[Description("Sets a givin user's color `only works if you're super cool`")]
			public async Task SetTargetColor(CommandContext ctx, [Description("The ID or mention of the target user")] DiscordMember user, [Description("The color to set to, can be in hex, rgb or keyword format (Must be a single string)")]string Color)
			{
				try
				{
					if (Color == "off")
						await RemoveColor(user, ctx.Guild, ctx.Channel);
					else
					{
						System.Drawing.Color col = ColorTranslator.FromHtml(Color);
						DiscordColor RoleColor = new DiscordColor(col.R, col.G, col.B);
						await SetColor(user, ctx.Guild, ctx.Channel, RoleColor);
					}
				}
				catch (Exception ex)
				{
					await ctx.RespondAsync($"Something went wrong! {ex.Message}");
					ctx.Client.DebugLogger.LogMessage(LogLevel.Error, "Discord Urie", $"Error in setting color, {ex.Message}", DateTime.Now);
				}
			}

			[Command("me"), ColorCommand]
			[Description("Sets your color")]
			public async Task SetSelfColor(CommandContext ctx, [Description("The color to set to, can be in hex, rgb or keyword format (Must be a single string)")]string Color)
			{
				try
				{
					if (Color == "off")
						await RemoveColor(ctx.User, ctx.Guild, ctx.Channel);

					else
					{
						System.Drawing.Color col = ColorTranslator.FromHtml(Color);
						DiscordColor RoleColor = new DiscordColor(col.R, col.G, col.B);
						await SetColor(ctx.Member, ctx.Guild, ctx.Channel, RoleColor);
					}
				}
				catch (Exception ex)
				{
					await ctx.RespondAsync($"Something went wrong! {ex.Message}");
					ctx.Client.DebugLogger.LogMessage(LogLevel.Error, "Discord Urie", $"Error in setting color, {ex.Message}", DateTime.Now);
				}
			}

			[Command("get")]
			[Description("Displays a givin user's current color")]
			public async Task GetColor(CommandContext ctx, [Description("The user to get the color of")] DiscordMember User)
			{
				try
				{
					var GuildSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);
					DiscordColor? UserColor = await GetColor(User, ctx.Guild);
					if (UserColor != null)
						await ctx.RespondAsync($"{User.Username}'s current color is {UserColor} or {UserColor.Value.R}, {UserColor.Value.G}, {UserColor.Value.B}");
					else
						await ctx.RespondAsync($"{User.Username} does not have a color set.");
				}
				catch (Exception ex)
				{
					await ctx.RespondAsync($"Something went wrong! {ex.Message}");
					ctx.Client.DebugLogger.LogMessage(LogLevel.Error, "Discord Urie", $"Error getting user's color; {ex.Message}", DateTime.Now);
				}
			}
		
			[Command("block"), RequireAuth]
			public async Task BlockAdd(CommandContext ctx, [Description("The user to add to the black/whitelist")]DiscordMember Member)
			{
				var GuildSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);
				if (GuildSettings.ColorBlacklist.Any(xr => xr == Member.Id))
				{
					await ctx.RespondAsync("They're already in the list!");
					return;
				}
				GuildSettings.ColorBlacklist.Add(Member.Id);
				await GuildSettings.SaveGuild(discordUrie.SQLConn);
				await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
			}

			[Command("unblock"), RequireAuth]
			public async Task BlockRemove(CommandContext ctx, [Description("The user to remove from the black/whitelist")]DiscordMember Member)
			{
				var GuildSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);
				if (!GuildSettings.ColorBlacklist.Any(xr => xr == Member.Id))
				{
					await ctx.RespondAsync("They're not in the list!");
					return;
				}
				GuildSettings.ColorBlacklist.Remove(Member.Id);
				await GuildSettings.SaveGuild(discordUrie.SQLConn);
				await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
			}

			[Command("mode"), RequireAuth]
			public async Task BlockMode(CommandContext ctx, int Mode)
			{
				var GuildSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);
				var ListMode = (BlackListModeEnum)Mode;
				if (GuildSettings.ColorBlacklistMode == ListMode) return;
				GuildSettings.ColorBlacklistMode = ListMode;
				await GuildSettings.SaveGuild(discordUrie.SQLConn);
				await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
			}

			[Command("toggle"), RequireAuth]
			public async Task Toggle(CommandContext ctx)
			{
				var GuildSettings = await discordUrie.Config.FindGuildSettings(ctx.Guild);
				if (GuildSettings.ColorEnabled)
				{
					GuildSettings.ColorEnabled = false;
					await GuildSettings.SaveGuild(discordUrie.SQLConn);
					await ctx.RespondAsync("Color commands disabled.");
					return;
				}
				else
				{
					GuildSettings.ColorEnabled = true;
					await GuildSettings.SaveGuild(discordUrie.SQLConn);
					await ctx.RespondAsync("Color commands enabled.");
					return;
				}
			}
		}
	}
}