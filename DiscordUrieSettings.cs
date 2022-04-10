using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using DSharpPlus;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Data.SQLite;

namespace DiscordUrie_DSharpPlus
{
	public class DiscordUrieSettings
	{

		public async Task<int> Createdb(SQLiteConnection conn)
		{
			await conn.OpenAsync();
			var command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS config (Id UNSIGNED INTEGER PRIMARY KEY," +
											"ColorEnabled INTEGER, ColorLocked INTEGER, ColorBlacklistMode INTEGER, ColorBlacklist TEXT," +
											"Admins TEXT, Tags TEXT, NotificationChannel INTEGER, AutoRole INTEGER);", conn);
			var pee = await command.ExecuteNonQueryAsync();
			conn.Close();
			return pee;
		}

		public async Task<DiscordUrieConfig> CreateAllDefaultSettings(BaseDiscordClient client, SQLiteConnection SQLConn)
		{
			DiscordUrieConfig OutputConfig = new DiscordUrieConfig(this, SQLConn)
			{

				StartupActivity = new DiscordActivity("the voices in my head", ActivityType.ListeningTo),

				GuildSettings = await CreateGuildDefaultSettings(client.Guilds.Values)
			};

			return OutputConfig;
		}

		public Task<DiscordUrieGuild> CreateGuildDefaultSettings(DiscordGuild Guild)
			=> Task.FromResult(new DiscordUrieGuild(Guild.Id));


		public async Task<List<DiscordUrieGuild>> CreateGuildDefaultSettings(IEnumerable<DiscordGuild> GuildList)
		{
			List<DiscordUrieGuild> OutputGuildList = new List<DiscordUrieGuild>();

			foreach (DiscordGuild cur in GuildList)
			{
				OutputGuildList.Add(await CreateGuildDefaultSettings(cur));
			}

			return OutputGuildList;
		}

		public async Task<DiscordUrieConfig> LoadSettings(SQLiteConnection conn)
		{
			await conn.OpenAsync();
			JsonSerializerSettings serializerSettings = new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore
			};

			var command = new SQLiteCommand("SELECT * FROM config", conn);
			var reader = await command.ExecuteReaderAsync();
			DiscordUrieConfig OutSettings = new DiscordUrieConfig(this, conn)
			{
				StartupActivity = JsonConvert.DeserializeObject<DiscordActivity>(File.ReadAllText("activity.json")),
				GuildSettings = new List<DiscordUrieGuild>()
			};
			while(await reader.ReadAsync())
			{
				OutSettings.GuildSettings.Add(new DiscordUrieGuild(Convert.ToUInt64(reader["id"]))
				{
					ColorEnabled  = Convert.ToBoolean(reader["ColorEnabled"]),
					ColorLocked = Convert.ToBoolean(reader["ColorLocked"]),
					ColorBlacklistMode = (BlackListModeEnum)Convert.ToInt32(reader["ColorBlacklistMode"]),
					ColorBlacklist = JsonConvert.DeserializeObject<List<ulong>>((string)reader["ColorBlacklist"]),
					Tags = JsonConvert.DeserializeObject<List<DiscordUrieTag>>((string)reader["Tags"]),
					NotificationChannel = Convert.ToUInt64(reader["NotificationChannel"]),
					AutoRole = Convert.ToUInt64(reader["AutoRole"])
				});
			}
			conn.Close();
			return OutSettings;
		}

	}

	public class DiscordUrieConfig
	{
		public DiscordUrieConfig(DiscordUrieSettings Settings, SQLiteConnection SQLConn,  List<DiscordUrieGuild> Guilds = null, DiscordActivity Activity = null)
		{
			this.SettingsInstance = Settings;
			this.SQLConn = SQLConn;
			this.GuildSettings = Guilds;
			this.StartupActivity = Activity;
		}

		public Task<bool> IsEmpty()
		{
			if (GuildSettings == null || StartupActivity == null)
			{
				return Task.FromResult(true);
			}
			return Task.FromResult(false);
		}

		public async Task<int> SaveSettings(SQLiteConnection conn)
		{
			await File.WriteAllTextAsync("activity.json",JsonConvert.SerializeObject(this.StartupActivity));
			int affected = 0;
			foreach(DiscordUrieGuild cur in GuildSettings)
			{
				affected += await cur.SaveGuild(conn);
			}
			return affected;
		}

		public async Task<int> RemoveGuild(ulong guildid)
		{
			bool result = GuildSettings.Remove(GuildSettings.First(xr => xr.Id == guildid));
			await this.SQLConn.OpenAsync();
			var command = new SQLiteCommand($"DELETE FROM config WHERE id = {guildid}", this.SQLConn);
			var Out = await command.ExecuteNonQueryAsync();
			this.SQLConn.Close();
			return Out;
		}

		public async Task<bool> AddGuild(DiscordGuild guild)
		{
			if (GuildSettings.Any(xr => xr.Id == guild.Id))
				return false;

			var Settings = await this.SettingsInstance.CreateGuildDefaultSettings(guild);
			GuildSettings.Add(Settings);
			await Settings.SaveGuild(this.SQLConn);
			return true;
		}

		public async Task<DiscordUrieGuild> FindGuildSettings(DiscordGuild SearchForGuild)
		{
			if (GuildSettings.Any(xr => xr.Id == SearchForGuild.Id))
			{
				return GuildSettings.First(xr => xr.Id == SearchForGuild.Id);
			}
			else
			{
				DiscordUrieGuild NewDefaultServer = await this.SettingsInstance.CreateGuildDefaultSettings(SearchForGuild);
				GuildSettings.Add(NewDefaultServer);
				await NewDefaultServer.SaveGuild(this.SQLConn);
				return NewDefaultServer;
			}
		}
		public async Task<List<DiscordUrieTag>> GetTags(DiscordGuild guild)
		{
			DiscordUrieGuild GuildSettings = await FindGuildSettings(guild);
			return GuildSettings.Tags;
		}

		public DiscordActivity StartupActivity { get; set; }
		public List<DiscordUrieGuild> GuildSettings { get; set; }
		public DiscordUrieSettings SettingsInstance { get; }
		public SQLiteConnection SQLConn { get; }
	}

	public enum BlackListModeEnum
	{
		Off = 0,
		Blacklist = 1,
		Whitelist = 2
	}

	public class DiscordUrieTag
	{
		public DiscordUrieTag(string Tag, string Output, ulong Owner)
		{
			this.Tag = Tag;
			this.Output = Output;
			this.Owner = Owner;
		}
		public string Tag {get; set;}
		public string Output {get; set;}
		public ulong Owner {get; set;}
	}

	public class DiscordUrieGuild
	{
		public DiscordUrieGuild(ulong Id)
		{
			this.Id = Id;
			this.ColorEnabled = true;
			this.ColorLocked = false;
			this.ColorBlacklistMode = BlackListModeEnum.Off;
			this.ColorBlacklist = new List<ulong>();
			this.Admins = new List<ulong>();
			this.Tags = new List<DiscordUrieTag>();
			this.NotificationChannel = 1;
			this.AutoRole = 0;
		}
		public async Task<int> SaveGuild(SQLiteConnection conn)
		{
			await conn.OpenAsync();
			JsonSerializerSettings serializerSettings = new JsonSerializerSettings
			{
				Formatting = Formatting.None,
				NullValueHandling = NullValueHandling.Ignore
			};
			var command = new SQLiteCommand($"INSERT OR REPLACE INTO config VALUES(@Id, " +
			"@ColorEnabled, @ColorLocked, @ColorBlacklistMode, @ColorBlacklist, " +
			"@Admins, @Tags, @NotificationChannel, @AutoRole)", conn);
			command.Parameters.AddWithValue("@Id", Id);
			command.Parameters.AddWithValue("@ColorEnabled", ColorEnabled);
			command.Parameters.AddWithValue("@ColorLocked", ColorLocked);
			command.Parameters.AddWithValue("@ColorBlacklistMode", ColorBlacklistMode);
			command.Parameters.AddWithValue("@ColorBlacklist", JsonConvert.SerializeObject(ColorBlacklist, serializerSettings));
			command.Parameters.AddWithValue("@Admins", JsonConvert.SerializeObject(Admins, serializerSettings));
			command.Parameters.AddWithValue("@Tags", JsonConvert.SerializeObject(Tags, serializerSettings));
			command.Parameters.AddWithValue("@NotificationChannel", NotificationChannel);
			command.Parameters.AddWithValue("@AutoRole", AutoRole);
			var peeagain = await command.ExecuteNonQueryAsync();
			conn.Close();
			return peeagain;
		}
		public ulong Id {get; set;}
		public bool ColorEnabled {get; set;}
		public bool ColorLocked {get; set;}
		public BlackListModeEnum ColorBlacklistMode {get; set;}
		public List<ulong> ColorBlacklist {get; set;}
		public List<ulong> Admins {get; set;}
		public List<DiscordUrieTag> Tags {get; set;}
		public ulong NotificationChannel {get; set;}
		public ulong AutoRole {get; set;}
	}
}