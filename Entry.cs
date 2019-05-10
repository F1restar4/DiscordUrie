﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Interactivity;
using SteamWebAPI2.Interfaces;

namespace DiscordUrie_DSharpPlus
{
	class Entry
	{

		public DiscordClient Client { get; set; }
		public CommandsNextExtension cmds;
		public InteractivityExtension IntExtension;
		public static DiscordUrieSettings.DiscordUrieConfig? InitSettings;
		public static DiscordUrieSettings.DiscordUrieConfig Settings;
		public static SteamUser SInterface;
		public static SteamStore SStore;
		public static PlayerService SPlayerService;
		public static string[] CmdPrefix = { "/" };

		public static void Main(string[] args)
		{

			var prog = new Entry();
			prog.Start().GetAwaiter().GetResult();
		}

		public async Task Start()
		{
			string token = null;

			if (!File.Exists("config.json"))
			{
				File.Create("config.json");

			}

			if (!File.Exists("token.txt"))
			{
				Console.Write("Token file not found. Please input a Discord bot token: ");
				token = Console.ReadLine();

				File.WriteAllText("token.txt", token);
			}
			else
			{
				if (token == null)
				{
					token = File.ReadAllText("token.txt");
				}
			}

			Console.Clear();

			InitSettings = DiscordUrieSettings.LoadSettings();

			if (InitSettings != null)
				Settings = InitSettings.Value;


			DiscordConfiguration config = new DiscordConfiguration
			{
				Token = token,
				UseInternalLogHandler = true,
			};

			Client = new DiscordClient(config);

			cmds = Client.UseCommandsNext(new CommandsNextConfiguration
			{
				CaseSensitive = false,
				StringPrefixes = CmdPrefix,
				EnableDefaultHelp = true,
				EnableDms = false,

			});

			IntExtension = Client.UseInteractivity(new InteractivityConfiguration());

			cmds.RegisterCommands<Commands>();
			Client.Ready += Events.Client_Ready;
			Client.ClientErrored += Events.ErrorHandler;
			Client.GuildMemberRemoved += Events.UserLeaveGuild;

			Client.MessageCreated += async e =>
			{
				if (!e.Author.IsBot)
				{
					await Events.ChatBansEventCall(e);
				}
			};

			await Client.ConnectAsync();

			string SKey = "CB1A5ADCAE06C134617D39DAAAD0AF79";
			SInterface = new SteamUser(SKey);
			SStore = new SteamStore();
			SPlayerService = new PlayerService(SKey);
			await Task.Delay(-1);
		}
	}
}