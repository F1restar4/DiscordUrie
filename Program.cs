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
			if (!File.Exists("DiscordUrieConfig.db"))
			{
				SQLiteConnection.CreateFile("DiscordUrieConfig.db");
			}
			if (!File.Exists("activity.json"))
			{
				File.Create("activity.json");
			}
			var SQLConn = new SQLiteConnection("Data Source=DiscordUrieConfig.db;Version=3;");
			var Sett = new DiscordUrieSettings();
			await Sett.Createdb(SQLConn);

			var DiscordUrie = new DiscordUrie(await Sett.LoadSettings(SQLConn));
			await DiscordUrie.StartAsync();
			await Task.Delay(-1);
		}
	}
}