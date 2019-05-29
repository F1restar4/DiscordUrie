﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using SteamWebAPI2.Interfaces;
using Steam.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using static DiscordUrie_DSharpPlus.DiscordUrieSettings;

namespace DiscordUrie_DSharpPlus
{

	public partial class Commands : BaseCommandModule
	{

		[Command("uptime")]
		public async Task UptimeAsync(CommandContext ctx)
		{
			await ctx.RespondAsync($"Program uptime: {await DateTime.Now.Subtract(Entry.StartTime).ToDuration()}");
		}

		[Command("lookup")]
		[Description("Looks up info about a user")]
		public async Task LookupUserAsync(CommandContext ctx, [Description("The user to lookup, can be an ID")]DiscordUser InputUser = null)
		{
			if (InputUser == null)
			{
				InputUser = ctx.User;
			}

			DiscordMember member;

			try
			{
				member = await ctx.Guild.GetMemberAsync(InputUser.Id);
			}
			catch
			{
				member = null;
			}


			DiscordEmbedBuilder EBuilder = new DiscordEmbedBuilder()
			.WithAuthor($"{InputUser.Username}#{InputUser.Discriminator}")
			.WithImageUrl(InputUser.GetAvatarUrl(ImageFormat.Png, 64))
			.WithTimestamp(DateTime.Now)
			.AddField("\u200b", InputUser.Mention);
			if (member != null)
			{
				EBuilder.AddField("In current guild", "true");
			}
			else
			{
				EBuilder.AddField("In current guild", "false");
			}

			await ctx.RespondAsync(embed: EBuilder.Build());

		}

		[Group("spam")]
		[Description("Spam remove group `Unusable unless you're cool`")]
		[Aliases("s")]
		public class SpamStuffGroup : BaseCommandModule
		{

			[Command("add")]
			public async Task SpamAddAsync(CommandContext ctx, string Yes, int Count)
			{
				if (await Util.UserAuth(ctx.Member.Id, ctx.Guild))
				{
					await ctx.Message.DeleteAsync();

					for (int i = 0; i < Count; i++)
					{
						await ctx.RespondAsync(Yes);
						await Task.Delay(1000);
					}
				}
			}

			[Command("remove")]
			[Description("Mass remove messages from a channel using the key as a search term `only works if you're super cool`")]
			public async Task SpamRemoveAsync(CommandContext ctx, [Description("The key to search for.")] string key, [Description("A specific user to search for, this param is optional")] DiscordMember ByUser = null)
			{

				if (!await Util.UserAuth(ctx.Member.Id, ctx.Guild))
				{
					await ctx.RespondAsync("You're not super special cool enough to use this command");
					return;
				}

				try
				{
					IReadOnlyList<DiscordMessage> Messages = await ctx.Channel.GetMessagesAsync();
					DiscordMessage InitRemoveMessage;
					key = key.ToLower();

					bool ByUserInput = false;


					InitRemoveMessage = await ctx.RespondAsync("Checking messages...");


					ByUserInput |= ByUser != null;
					IEnumerable<DiscordMessage> FilteredMessages;

					if (ByUserInput)
						FilteredMessages = Messages.Where(xr => xr.Content.ToLower().Contains(key) && xr.Author.Id == ByUser.Id && xr != ctx.Message);
					else
						FilteredMessages = Messages.Where(xr => xr.Content.ToLower().Contains(key) && xr != ctx.Message);

					await ctx.Channel.DeleteMessagesAsync(FilteredMessages, "Spam Removal Command Deletion");

					ctx.Client.DebugLogger.LogMessage(LogLevel.Info, "Discord Urie", $"Removed {FilteredMessages.Count()} matching messages.", DateTime.Now);

					await InitRemoveMessage.ModifyAsync($"Removed {FilteredMessages.Count()} matching messages.");
					await Task.Delay(3000);
					await InitRemoveMessage.DeleteAsync("Command auto deletion.");

					await ctx.Message.DeleteAsync();
				}
				catch (Exception ex)
				{
					DiscordMessage ErrorMessage = await ctx.RespondAsync($"Something went wrong: {ex.Message}");
					ctx.Client.DebugLogger.LogMessage(LogLevel.Error, "Dicord Urie", $"Error in /s remove; {ex.Message}", DateTime.Now);

					await Task.Delay(5000);

					await ErrorMessage.DeleteAsync("Command auto deletion.");
					await ctx.Message.DeleteAsync("Command auto deletion.");

				}

			}
		}


		[Group("steam")]
		[Description("Steam related commands")]
		[Hidden]
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


			[Command("profileinfo")]
			[Aliases("profinf", "calc")]
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

		[Group("settings")]
		[Description("Various bot related settings")]
		[Hidden]
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

			[Command("list")]
			[Description("Lists settings")]
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

			[Command("update")]
			[Description("Updates settings")]
			[Hidden]
			public async Task UpdateSettingsAsync(CommandContext ctx)
			{
				DiscordUrieConfig? Settings = await LoadSettings(Entry.SQLConn);
				if (Settings != null)
				{
					Entry.Settings = Settings.Value;

					DiscordMessage Message = await ctx.RespondAsync("Settings updated...");
					await ctx.Client.UpdateStatusAsync(Entry.Settings.StartupActivity);
					await Message.DeleteAsync();
					await ctx.Message.DeleteAsync();
				}
			}

			[Group("colorsettings")]
			[Aliases("color")]
			[Description("Settings related to the coloring commands")]
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

				[Command("enabled")]
				[Aliases("enable")]
				[Description("Setting to determine if coloring is enabled on this server")]
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

		[Command("setmsg")]
		[Hidden]
		public async Task SetGameMsgAsync(CommandContext ctx, string name, ActivityType type)
		{
			if (!await Util.UserAuth(ctx.Member.Id, ctx.Guild))
				return;
			await ctx.Client.UpdateStatusAsync(new DiscordActivity(name, type), UserStatus.Online);
			await ctx.Message.DeleteAsync(reason: "Command auto deletion.");
		}

		[Command("shutdown")]
		[Hidden]
		public async Task ShutdownAsync(CommandContext ctx)
		{
			if (!await Util.UserAuthHigh(ctx.Member.Id, ctx.Guild))
				return;
			DiscordMessage HelpThanks = await ctx.RespondAsync("Shutting down...");
			await Task.Delay(3000);
			await ctx.Message.DeleteAsync("Command auto deletion.");
			await HelpThanks.DeleteAsync("Command auto deletion.");
			await ctx.Client.DisconnectAsync();
			Entry.SQLConn.Close();
			Environment.Exit(0);

		}

		public class globals
		{
			public CommandContext ctx;
			public DiscordUrieConfig settings;
		}

		[Command("eval"), Description("Evaluates a snippet of C# code, in context."), Hidden, RequireOwner]
       	public async Task EvaluateAsync(CommandContext ctx, [RemainingText, Description("Code to evaluate.")] string code)
        {
            var cs1 = code.IndexOf("```") + 3;
            cs1 = code.IndexOf('\n', cs1) + 1;
            var cs2 = code.LastIndexOf("```");

            if (cs1 == -1 || cs2 == -1)
               	throw new ArgumentException("You need to wrap the code into a code block.", nameof(code));

            code = code.Substring(cs1, cs2 - cs1);

            var embed = new DiscordEmbedBuilder
            {
                Title = "Evaluating...",
                Color = new DiscordColor(0xD091B2)
            };
            var msg = await ctx.RespondAsync("", embed: embed.Build()).ConfigureAwait(false);

            var globals = new globals
			{
				ctx = ctx,
				settings = Entry.Settings
			};
            var sopts = ScriptOptions.Default
                .WithImports("System", "System.Collections.Generic", "System.Diagnostics", "System.Linq", "System.Net.Http", "System.Net.Http.Headers", "System.Reflection", "System.Text", 
                             "System.Threading.Tasks", "DSharpPlus", "DSharpPlus.CommandsNext", "DSharpPlus.Entities", "DSharpPlus.EventArgs", "DSharpPlus.Exceptions")
                .WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)));
            
            var sw1 = Stopwatch.StartNew();
            var cs = CSharpScript.Create(code, sopts, typeof(globals));
            var csc = cs.Compile();
            sw1.Stop();
            
            if (csc.Any(xd => xd.Severity == DiagnosticSeverity.Error))
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = "Compilation failed",
                    Description = string.Concat("Compilation failed after ", sw1.ElapsedMilliseconds.ToString("#,##0"), "ms with ", csc.Length.ToString("#,##0"), " errors."),
                    Color = new DiscordColor(255,0,0)
                };
                foreach (var xd in csc.Take(3))
                {
                    var ls = xd.Location.GetLineSpan();
                    embed.AddField(string.Concat("Error at ", ls.StartLinePosition.Line.ToString("#,##0"), ", ", ls.StartLinePosition.Character.ToString("#,##0")), Formatter.InlineCode(xd.GetMessage()), false);
                }
                if (csc.Length > 3)
                {
                    embed.AddField("Some errors ommited", string.Concat((csc.Length - 3).ToString("#,##0"), " more errors not displayed"), false);
                }
                await msg.ModifyAsync(embed: embed.Build()).ConfigureAwait(false);
                return;
            }

            Exception rex = null;
            ScriptState<object> css = null;
            var sw2 = Stopwatch.StartNew();
            try
            {
                css = await cs.RunAsync(globals).ConfigureAwait(false);
                rex = css.Exception;
            }
            catch (Exception ex)
            {
                rex = ex;
            }
            sw2.Stop();

            if (rex != null)
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = "Execution failed",
                    Description = string.Concat("Execution failed after ", sw2.ElapsedMilliseconds.ToString("#,##0"), "ms with `", rex.GetType(), ": ", rex.Message, "`."),
                    Color = new DiscordColor(255,0,0),
                };
                await msg.ModifyAsync(embed: embed.Build()).ConfigureAwait(false);
                return;
            }

            // execution succeeded
            embed = new DiscordEmbedBuilder
            {
                Title = "Evaluation successful",
                Color = new DiscordColor(0,255,0),
            };

            embed.AddField("Result", css.ReturnValue != null ? css.ReturnValue.ToString() : "No value returned", false)
                .AddField("Compilation time", string.Concat(sw1.ElapsedMilliseconds.ToString("#,##0"), "ms"), true)
                .AddField("Execution time", string.Concat(sw2.ElapsedMilliseconds.ToString("#,##0"), "ms"), true);

            if (css.ReturnValue != null)
                embed.AddField("Return type", css.ReturnValue.GetType().ToString(), true);

            await msg.ModifyAsync(embed: embed.Build()).ConfigureAwait(false);
		}

	}
}