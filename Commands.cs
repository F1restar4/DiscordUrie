﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using SteamWebAPI2.Interfaces;
using Steam.Models;
using Microsoft.CodeAnalysis;
using static DiscordUrie_DSharpPlus.DiscordUrieSettings;
using DiscordUrie_DSharpPlus.Attributes;

namespace DiscordUrie_DSharpPlus
{

	public partial class Commands : BaseCommandModule
	{
		
		[Group("steam"), Hidden, Description("Steam related commands")]
		public class SteamCommands : BaseCommandModule
		{
			class OtherHelpfullMethods
			{
				public static Task<ulong?> Convert32IdTo64(string ThirtyTwoBitId)
				{
					string[] SplitInput = ThirtyTwoBitId.Split(':');

					string Woah = null;
					string haoW = null;

					if (SplitInput[0] == "STEAM_0")
					{
						Woah = SplitInput[1];
						haoW = SplitInput[2];
					}

					if (!string.IsNullOrWhiteSpace(Woah) || !string.IsNullOrWhiteSpace(haoW))
					{
						ulong Converted = Convert.ToUInt64(haoW) * 2;
						Converted += 76561197960265728;
						Converted += Convert.ToUInt64(Woah);

						return Task.FromResult<ulong?>(Converted);
					}

					return Task.FromResult<ulong?>(null);
				}
			}


			[Command("profileinfo"), Aliases("profinf", "calc")]
			public async Task ProfileInfoAsync(CommandContext ctx, string Userid)
			{
				DiscordMessage LoadingMessage = await ctx.RespondAsync("Retrieving info...");
				ulong? ConvertedId = await OtherHelpfullMethods.Convert32IdTo64(Userid);

				if (ConvertedId == null)
				{
					try
					{
						ConvertedId = Convert.ToUInt64(Userid);
					}
					catch
					{
						var b = await Entry.SInterface.ResolveVanityUrlAsync(Userid);
						ConvertedId = b.Data;

					}
				}

				var Response = await Entry.SInterface.GetPlayerSummaryAsync(ConvertedId.Value);
				var Data = Response.Data;


				var Games = await Entry.SPlayerService.GetOwnedGamesAsync(Data.SteamId, true);
				var Games2 = Games.Data;



				var LevelResponse = await Entry.SPlayerService.GetSteamLevelAsync(Data.SteamId);
				uint PlayerLevel = LevelResponse.Data.Value;
				string PlayerLevelStr = PlayerLevel.ToString();


				if (PlayerLevelStr.Length > 2)
				{
					await PlayerLevelStr.Truncate(2);
				}

				uint.TryParse(PlayerLevelStr, out uint CutPlayerLevel);
				DiscordColor color;

				switch (CutPlayerLevel)
				{
					case uint n when (n >= 0 && n < 10):
						color = new DiscordColor("#9b9b9b");
						break;

					case uint n when (n >= 10 && n < 20):
						color = new DiscordColor("#c02942");
						break;

					case uint n when (n >= 20 && n < 30):
						color = new DiscordColor("#d95b43");
						break;

					case uint n when (n >= 30 && n < 40):
						color = new DiscordColor("#fecc23");
						break;

					case uint n when (n >= 40 && n < 50):
						color = new DiscordColor("#467a3c");
						break;

					case uint n when (n >= 50 && n < 60):
						color = new DiscordColor("#4e8ddb");
						break;

					case uint n when (n >= 60 && n < 70):
						color = new DiscordColor("#7652c9");
						break;

					case uint n when (n >= 70 && n < 80):
						color = new DiscordColor("#c252c9");
						break;

					case uint n when (n >= 80 && n < 90):
						color = new DiscordColor("#542437");
						break;

					case uint n when (n >= 90 && n <= 99):
						color = new DiscordColor("#997c52");
						break;

					default:
						color = new DiscordColor("#9b9b9b");
						break;

				}

				DiscordEmbedBuilder EmbedBuilder = new DiscordEmbedBuilder
				{
					Color = color,
					ThumbnailUrl = Data.AvatarMediumUrl,
				};

				/*foreach (Steam.Models.SteamCommunity.OwnedGameModel cur in Games2.OwnedGames)
				{
					var woah = await Entry.SStore.GetStoreAppDetailsAsync(cur.AppId);

				}*/


				EmbedBuilder.WithAuthor(Data.Nickname, Data.ProfileUrl);
				EmbedBuilder.AddField("\u200b", $"Level: {PlayerLevel}");



				await LoadingMessage.ModifyAsync(null, EmbedBuilder.Build());

			}
		}

		[Group("settings"), Hidden, Description("Various bot related settings")]
		public class Settings : BaseCommandModule
		{
			[GroupCommand]
			public async Task ExecuteGroupAsync(CommandContext ctx)
			{
				DiscordEmbedBuilder EBuilder = new DiscordEmbedBuilder()
				{
					Color = new DiscordColor("007fff"),
					Title = "Settings",
					Description = $"This is a list of all of my settings for this server.\nRun `settings [setting/setting sub group]` to get details about specific settings.",
				};

				EBuilder.AddField("colorsettings", "enabled\nblacklistmode\nblacklist add\nblacklist remove", true);
				EBuilder.AddField("Other settings", "admins", true);

				await ctx.RespondAsync(null, false, EBuilder.Build());
			}

			[Command("list"), Description("Lists settings")]
			public async Task ListSettingsAsync(CommandContext ctx)
			{
				DiscordEmbedBuilder EBuilder = new DiscordEmbedBuilder()
				{
					Color = new DiscordColor("007fff"),
					Title = "Settings",
					Description = "These are the current settings for this guild.\n----------------------------------"
				};

				DiscordUrieGuild GuildSettings = await Entry.Settings.FindGuildSettings(ctx.Guild);

				string CurMessage = "";

				foreach (ulong cur in GuildSettings.ColorBlacklist)
				{
					CurMessage += $"{cur} ";
				}

				if (CurMessage == "")
				{
					CurMessage = "`NONE`";
				}

				EBuilder.AddField("Server Id", GuildSettings.Id.ToString());
				EBuilder.AddField("Color Settings", $"Enabled: {GuildSettings.ColorEnabled}\n" +
								$"Locked: {GuildSettings.ColorLocked}\n" +
								$"Blacklist Mode: {GuildSettings.ColorBlacklistMode}\n" +
								$"Blacklist: {CurMessage}\n");

				List<ulong> idlist = await Entry.Settings.GetChatBanIdList(ctx.Guild);
				int yes = 0;
				CurMessage = "";
				foreach (ulong cur in idlist)
				{
					DiscordMember CurUser = await ctx.Guild.GetMemberAsync(cur);


					if (CurUser != null)
					{
						if (yes == 0)
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
						if (yes == 0)
						{
							CurMessage += $"`{cur}`";
						}
						else
						{
							CurMessage += $", `{cur}`";
						}
					}

					yes++;
				}

				if (CurMessage == "")
				{
					CurMessage = "`NONE`";
				}

				EBuilder.AddField("Chat Ban Settings", $"Enabled: {GuildSettings.BansEnabled}\n" +
								$"Banned Users: {CurMessage}");

				await ctx.RespondAsync(null, false, EBuilder.Build());
			}

			[Command("update"), Description("Updates settings")]
			public async Task UpdateSettingsAsync(CommandContext ctx)
			{
				DiscordUrieConfig Settings = await LoadSettings(Entry.SQLConn);
				Entry.Settings = Settings;

				DiscordMessage Message = await ctx.RespondAsync("Settings updated...");
				await ctx.Client.UpdateStatusAsync(Entry.Settings.StartupActivity);
				await Message.DeleteAsync();
				await ctx.Message.DeleteAsync();
			}

			[Group("colorsettings"), Aliases("color"), Description("Settings related to the coloring commands")]
			public class ColorSettings : BaseCommandModule
			{
				[GroupCommand()]
				public async Task ExecuteGroupAsync(CommandContext ctx)
				{
					DiscordEmbedBuilder EBuilder = new DiscordEmbedBuilder()
					{
						Color = new DiscordColor("007fff"),
						Title = "Color settings",
						Description = $"This is a list of all of my color related settings for this server.\nRun `settings colorsettings [setting]` to get details about specific settings.",
					};

					EBuilder.AddField("Settings", "enabled\nblacklistmode\nblacklist add\nblacklist remove", true);

					await ctx.RespondAsync(null, false, EBuilder.Build());
				}

				[Command("enabled"), RequireAuth, Aliases("enable"), Description("Setting to determine if coloring is enabled on this server")]
				public async Task EnabledAsync(CommandContext ctx, bool Enabled, [Description("Set to disable changing this setting unless you're the server owner [only works if you are the owner (or are cool enough)]")] bool? Lock = null)
				{
					if (!await Util.UserAuth(ctx.Member.Id, ctx.Guild))
					{
						await ctx.RespondAsync("Incorrect permissions to edit setting.");
						return;
					}

					DiscordUrieGuild GuildSettings = await Entry.Settings.FindGuildSettings(ctx.Guild);
					bool locked = GuildSettings.ColorLocked;
					bool LockChanged = false;

					if (GuildSettings.ColorLocked && !await Util.UserAuth(ctx.Member.Id, ctx.Guild))
					{
						await ctx.RespondAsync("This setting is locked. You do not have the correct permissions to edit it...");
					}
					else
					{
						if (Enabled == GuildSettings.ColorEnabled)
						{
							await ctx.RespondAsync($"This setting is already set to {Enabled}");
						}
						if (Lock.HasValue && await Util.UserAuth(ctx.Member.Id, ctx.Guild))
						{
							if (Lock != locked)
							{
								locked = Lock.Value;
								LockChanged = true;
							}
						}

						Entry.Settings.GuildSettings.Remove(GuildSettings);
						GuildSettings.ColorEnabled = Enabled;
						GuildSettings.ColorLocked = locked;

						Entry.Settings.GuildSettings.Add(GuildSettings);
						await GuildSettings.SaveGuild(Entry.SQLConn);
						if (Enabled)
						{
							if (LockChanged)
							{
								if (locked)
								{
									await ctx.RespondAsync("Color commands have been enabled and locked, users without elevated permissions cannot change this setting until it is unlocked.");
								}
								else
								{
									await ctx.RespondAsync("Color commands have been enabled and unlocked, users with medium permissions can now change this setting.");
								}
							}
							else
							{
								await ctx.RespondAsync("Color commands have been enabled.");
							}
						}
						else
						{
							if (LockChanged)
							{
								if (locked)
								{
									await ctx.RespondAsync("Color commands have been disabled and locked, users without elevated permissions cannot change this setting util it is unlocked.");
								}
								else
								{
									await ctx.RespondAsync("Color commands have been disabled and unlocked, users with medium permissions can now change this setting.");
								}
							}
							else
							{
								await ctx.RespondAsync("Color commands have been disabled.");
							}
						}
					}


				}
			}
		}
	}
}