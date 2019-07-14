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
	}
}