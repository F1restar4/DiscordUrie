﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.SQLite;
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
		public static SQLiteConnection SQLConn;
		public static string[] CmdPrefix = { "/" };
		public static DateTime StartTime = DateTime.Now;

		public static void Main(string[] args)
		{

			var prog = new Entry();
			prog.Start().GetAwaiter().GetResult();
		}

		public async Task Start()
		{
			if (!File.Exists("DiscordUrieConfig.db"))
			{
				SQLiteConnection.CreateFile("DiscordUrieConfig.db");
			}
			if (!File.Exists("activity.json"))
			{
				File.Create("activity.json");
			}

			SQLConn = new SQLiteConnection("Data Source=DiscordUrieConfig.db;Version=3;");
			await SQLConn.OpenAsync();
			await DiscordUrieSettings.Createdb(SQLConn);

			string token = null;
			string steamKey = null;


			if (!File.Exists("token.txt"))
			{
				Console.Write("Token file not found. Please input a Discord bot token: ");
				token = Console.ReadLine();

				await File.WriteAllTextAsync("token.txt", token);
				Console.Clear();
			}
			else
			{
				if (token == null)
				{
					token = await File.ReadAllTextAsync("token.txt");
				}
			}

			if (!File.Exists("steamkey.txt"))
			{
				Console.Write("Input a steam api key: ");
				steamKey = Console.ReadLine();
				await File.WriteAllTextAsync("steamkey.txt", steamKey);
				Console.Clear();
			}
			else
			{
				if (steamKey == null)
				{
					steamKey = await File.ReadAllTextAsync("steamKey.txt");
				}
			}


			InitSettings = await DiscordUrieSettings.LoadSettings(SQLConn);

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
			Client.GuildAvailable += Events.GuildAvailable;
			Client.GuildDeleted += Events.GuildDeleted;

			Client.MessageCreated += async e =>
			{
				if (!e.Author.IsBot)
				{
					await Events.ChatBansEventCall(e);
				}
			};

			await Client.ConnectAsync();

			SInterface = new SteamUser(steamKey);
			SStore = new SteamStore();
			SPlayerService = new PlayerService(steamKey);
			await Task.Delay(-1);
			SQLConn.Close();
		}
	}
}