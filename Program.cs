﻿using System.Threading.Tasks;
using System.IO;
using System.Data.SQLite;

namespace DiscordUrie_DSharpPlus
{
	class Program
	{
		public static void Main(string[] args)
		{
			var prog = new Program();
			prog.MainAsync().GetAwaiter().GetResult();
		}

		public async Task MainAsync()
		{
			//Check for the database file and create it if it doesn't exist
			if (!File.Exists("DiscordUrieConfig.db"))
			{
				SQLiteConnection.CreateFile("DiscordUrieConfig.db");
			}
			if (!File.Exists("activity.json"))
			{
				File.Create("activity.json");
			}
			//Setup database connection
			var SQLConn = new SQLiteConnection("Data Source=DiscordUrieConfig.db;Version=3;");
			var Sett = new DiscordUrieSettings();
			await Sett.Createdb(SQLConn);

			//Load settings from database and create a new instance of the bot with it
			var DiscordUrie = new DiscordUrie(await Sett.LoadSettings(SQLConn), SQLConn, Sett);
			//Start.
			await DiscordUrie.StartAsync();
			await Task.Delay(-1);
		}
	}
}