using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
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
		[Group("color"), Description("Coloring group")]
		public class ColoringStuffGroup : BaseCommandModule
		{

			public class MethodShit
			{
				public static async Task<bool> RemoveColor(DiscordUser user, DiscordGuild server, DiscordChannel channel, bool Override = false)
				{
					DiscordUrieGuild GuildSettings = await Entry.Settings.FindGuildSettings(server);

					if (!Override)
					{
						if (!GuildSettings.ColorEnabled)
						{
							await channel.SendMessageAsync("This feature is disabled on this server.");
							return false;
						}

						switch (GuildSettings.ColorBlacklistMode)
						{
							case BlackListModeEnum.Blacklist:
								if (GuildSettings.ColorBlacklist.Any(xr => xr == user.Id))
								{
									await channel.SendMessageAsync("You are blacklisted from this command and cannot use it.");
									return false;
								}
								break;

							case BlackListModeEnum.Whitelist:
								if (!GuildSettings.ColorBlacklist.Any(xr => xr == user.Id))
								{
									await channel.SendMessageAsync("You have not been whitelisted for this command and cannot use it.");
									return false;
								}
								break;
						}
					}

					IReadOnlyDictionary<ulong, DiscordRole> roles = server.Roles;

					if (!roles.Any(xr => xr.Value.Name == user.Id.ToString()))
					{
						if (Override) return false;
						await channel.SendMessageAsync("There was no color to remove!");
						return false;
					}

					DiscordRole cur = roles.First(xr => xr.Value.Name == user.Id.ToString()).Value;
					await cur.DeleteAsync("Color deletion");

					await channel.SendMessageAsync("Color removed successfully.");
					Console.WriteLine("User {0}'s color was removed.", user.Username);
					return true;
				}

				public static async Task<bool> SetColor(DiscordMember user, DiscordGuild server, DiscordChannel channel, DiscordColor color)
				{
					DiscordUrieGuild GuildSettings = await Entry.Settings.FindGuildSettings(server);

					if (!GuildSettings.ColorEnabled)
					{
						await channel.SendMessageAsync("This feature is disabled on this server.");
						return false;
					}

					switch (GuildSettings.ColorBlacklistMode)
					{
						case BlackListModeEnum.Blacklist:
							if (GuildSettings.ColorBlacklist.Any(xr => xr == user.Id))
							{
								await channel.SendMessageAsync("You are blacklisted from this command and cannot use it.");
								return false;
							}
							break;

						case BlackListModeEnum.Whitelist:
							if (!GuildSettings.ColorBlacklist.Any(xr => xr == user.Id))
							{
								await channel.SendMessageAsync("You have not been whitelisted for this command and cannot use it.");
								return false;
							}
							break;
					}

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
						DiscordRole CreatedRole = await server.CreateRoleAsync(user.Id.ToString(), null, color, null, null, "Coloring role creation");

						await user.GrantRoleAsync(CreatedRole, "Coloring role assignment");
					}


					await channel.SendMessageAsync("Color set successfully.");

					Console.WriteLine("User {0}'s color was set to {1}", user.Username, color.R + ", " + color.G + ", " + color.B);
					return true;


				}

				public static async Task<DiscordColor?> GetColor(DiscordMember user, DiscordGuild server)
				{

					DiscordUrieGuild GuildSettings = await Entry.Settings.FindGuildSettings(server);

					if (!GuildSettings.ColorEnabled)
						return null;



					IReadOnlyDictionary<ulong, DiscordRole> roleList = server.Roles;
					string UserId = user.Id.ToString();

					if (!roleList.Any(xr => xr.Value.Name == UserId)) return null;
					return roleList.First(xr => xr.Value.Name == UserId).Value.Color;


				}
			}

			[GroupCommand()]
			[Description("Equal to calling color me or color set in one command, mentioning yourself will just run color me and will not require elevated perms")]
			public async Task ExecuteGroupAsync(CommandContext ctx, [Description("The ID or mention of the target user")] DiscordMember user, [Description("The color to set to, can be in hex, rgb or keyword format (Must be a single string)")] string Color)
			{
				try
				{

					if (user != ctx.Member && !await Util.UserAuth(ctx.Member.Id, ctx.Guild))
					{
						await ctx.RespondAsync("You don't have the correct permissions for this!");
						return;
					}

					if (Color == "off")
					{
						await MethodShit.RemoveColor(user, ctx.Guild, ctx.Channel);
						return;
					}

					System.Drawing.Color col = System.Drawing.ColorTranslator.FromHtml(Color);
					DiscordColor RoleColor = new DiscordColor(col.R, col.G, col.B);

					await MethodShit.SetColor(user, ctx.Guild, ctx.Channel, RoleColor);
				}
				catch (Exception ex)
				{
					await ctx.RespondAsync($"Something went wrong! {ex.Message}");


					ctx.Client.DebugLogger.LogMessage(LogLevel.Error, "Discord Urie", $"Error in setting color, {ex.Message}", DateTime.Now);
				}
			}


			[Command("purge")]
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

			[Command("set")]
			[Description("Sets a givin user's color `only works if you're super cool`")]
			public async Task SetTargetColor(CommandContext ctx, [Description("The ID or mention of the target user")] DiscordMember user, [Description("The color to set to, can be in hex, rgb or keyword format (Must be a single string)")]string Color)
			{
				if (await Util.UserAuth(ctx.User.Id, ctx.Guild))
				{
					try
					{


						if (Color == "off")
							await MethodShit.RemoveColor(user, ctx.Guild, ctx.Channel);

						else
						{
							System.Drawing.Color col = System.Drawing.ColorTranslator.FromHtml(Color);
							DiscordColor RoleColor = new DiscordColor(col.R, col.G, col.B);

							await MethodShit.SetColor(user, ctx.Guild, ctx.Channel, RoleColor);
						}
					}
					catch (Exception ex)
					{
						await ctx.RespondAsync($"Something went wrong! {ex.Message}");


						ctx.Client.DebugLogger.LogMessage(LogLevel.Error, "Discord Urie", $"Error in setting color, {ex.Message}", DateTime.Now);

					}
				}
			}

			[Command("me")]
			[Description("Sets your color")]
			public async Task SetSelfColor(CommandContext ctx, [Description("The color to set to, can be in hex, rgb or keyword format (Must be a single string)")]string Color)
			{
				try
				{

					if (Color == "off")
						await MethodShit.RemoveColor(ctx.User, ctx.Guild, ctx.Channel);

					else
					{

						System.Drawing.Color col = System.Drawing.ColorTranslator.FromHtml(Color);
						DiscordColor RoleColor = new DiscordColor(col.R, col.G, col.B);
						await MethodShit.SetColor(ctx.Member, ctx.Guild, ctx.Channel, RoleColor);

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

					DiscordUrieGuild GuildSettings = await Entry.Settings.FindGuildSettings(ctx.Guild);

					if (!GuildSettings.ColorEnabled)
					{
						await ctx.RespondAsync("This feature is disabled on this server.");
						return;
					}


					DiscordColor? UserColor = await MethodShit.GetColor(User, ctx.Guild);

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
		}
	}
}