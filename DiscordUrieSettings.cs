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

		public static async Task<int> Createdb(SQLiteConnection conn)
		{
			var command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS config (Id UNSIGNED INTEGER PRIMARY KEY," +
											"ColorEnabled INTEGER, ColorLocked INTEGER, ColorBlacklistMode INTEGER, ColorBlacklist TEXT," +
											"BansEnabled INTEGER, BannedIds TEXT, Admins TEXT, Tags TEXT);", conn);
			return await command.ExecuteNonQueryAsync();
		}

		public static async Task<DiscordUrieConfig> CreateAllDefaultSettings(BaseDiscordClient client)
		{
			DiscordUrieConfig OutputConfig = new DiscordUrieConfig
			{

				StartupActivity = new DiscordActivity("the voices in my head", ActivityType.ListeningTo),

				GuildSettings = await CreateGuildDefaultSettings(client.Guilds.Values)
			};


			return OutputConfig;
		}

		public static Task<DiscordUrieGuild> CreateGuildDefaultSettings(DiscordGuild Guild)
		{
			return Task.FromResult(new DiscordUrieGuild()
			{
				Id = Guild.Id,
				ColorEnabled = true,
				ColorLocked = false,
				ColorBlacklistMode = BlackListModeEnum.Off,
				ColorBlacklist = new List<ulong>(),
				BansEnabled = false,
				BannedIds = new List<ulong>(),
				Admins = new List<ulong>(),
				Tags = new List<DiscordUrieTag>()
			});
		}

		public static async Task<List<DiscordUrieGuild>> CreateGuildDefaultSettings(IEnumerable<DiscordGuild> GuildList)
		{
			List<DiscordUrieGuild> OutputGuildList = new List<DiscordUrieGuild>();

			foreach (DiscordGuild cur in GuildList)
			{
				OutputGuildList.Add(await CreateGuildDefaultSettings(cur));
			}


			return OutputGuildList;
		}

		public static async Task<DiscordUrieConfig> LoadSettings(SQLiteConnection conn)
		{
			JsonSerializerSettings serializerSettings = new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore
			};

			var command = new SQLiteCommand("SELECT * FROM config", conn);
			var reader = await command.ExecuteReaderAsync();
			DiscordUrieConfig OutSettings = new DiscordUrieConfig
			{
				StartupActivity = JsonConvert.DeserializeObject<DiscordActivity>(File.ReadAllText("activity.json")),
				GuildSettings = new List<DiscordUrieGuild>()
			};
			while(await reader.ReadAsync())
			{
				OutSettings.GuildSettings.Add(new DiscordUrieGuild
				{
					Id = Convert.ToUInt64(reader["Id"]),
					ColorEnabled  = Convert.ToBoolean(reader["ColorEnabled"]),
					ColorLocked = Convert.ToBoolean(reader["ColorLocked"]),
					ColorBlacklistMode = (BlackListModeEnum)Convert.ToInt32(reader["ColorBlacklistMode"]),
					ColorBlacklist = JsonConvert.DeserializeObject<List<ulong>>((string)reader["ColorBlacklist"]),
					BansEnabled = Convert.ToBoolean(reader["BansEnabled"]),
					BannedIds = JsonConvert.DeserializeObject<List<ulong>>((string)reader["BannedIds"]),
					Tags = JsonConvert.DeserializeObject<List<DiscordUrieTag>>((string)reader["Tags"])
				});
			}
			return OutSettings;
		}

		public class DiscordUrieConfig
		{
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
				await File.WriteAllTextAsync("activity.json",JsonConvert.SerializeObject(Entry.Settings.StartupActivity));
				int affected = 0;
				foreach(DiscordUrieGuild cur in GuildSettings)
				{
					affected += await cur.SaveGuild(conn);
				}
				return affected;
			}

			public async Task<bool> RemoveGuild(ulong guildid)
			{
				bool result = GuildSettings.Remove(GuildSettings.First(xr => xr.Id == guildid));
				await SaveSettings(Entry.SQLConn);
				return result;
			}

			public async Task<bool> AddGuild(DiscordGuild guild)
			{
				if (GuildSettings.Any(xr => xr.Id == guild.Id))
					return false;

				var Settings = await CreateGuildDefaultSettings(guild);
				GuildSettings.Add(Settings);
				await Settings.SaveGuild(Entry.SQLConn);
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
					DiscordUrieGuild NewDefaultServer = await CreateGuildDefaultSettings(SearchForGuild);


					GuildSettings.Add(NewDefaultServer);
					await NewDefaultServer.SaveGuild(Entry.SQLConn);
					return NewDefaultServer;
				}
			}

			public async Task<List<ulong>> GetChatBanIdList(DiscordGuild Guild)
			{

				DiscordUrieSettings.DiscordUrieGuild GuildSettings = await FindGuildSettings(Guild);

				return GuildSettings.BannedIds;
			}

			public async Task<List<DiscordUrieTag>> GetTags(DiscordGuild guild)
			{
				DiscordUrieGuild GuildSettings = await FindGuildSettings(guild);
				return GuildSettings.Tags;
			}

			public DiscordActivity StartupActivity { get; set; }

			public List<DiscordUrieGuild> GuildSettings { get; set; }
		}

		public enum BlackListModeEnum
		{
			Off = 0,
			Blacklist = 1,
			Whitelist = 2
		}

		public class DiscordUrieTag
		{
			public string Tag {get; set;}
			public string Output {get; set;}
			public ulong Owner {get; set;}
		}

		public class DiscordUrieGuild
		{
			public async Task<int> SaveGuild(SQLiteConnection conn)
			{
				JsonSerializerSettings serializerSettings = new JsonSerializerSettings
				{
					Formatting = Formatting.None,
					NullValueHandling = NullValueHandling.Ignore
				};
				var command = new SQLiteCommand($"INSERT OR REPLACE INTO config VALUES(@Id, " +
				"@ColorEnabled, @ColorLocked, @ColorBlacklistMode, @ColorBlacklist, " +
				"@BansEnabled, @BannedIds, @Admins, @Tags)", conn);
				command.Parameters.AddWithValue("@Id", Id);
				command.Parameters.AddWithValue("@ColorEnabled", ColorEnabled);
				command.Parameters.AddWithValue("@ColorLocked", ColorLocked);
				command.Parameters.AddWithValue("@ColorBlacklistMode", ColorBlacklistMode);
				command.Parameters.AddWithValue("@ColorBlacklist", JsonConvert.SerializeObject(ColorBlacklist, serializerSettings));
				command.Parameters.AddWithValue("@BansEnabled", BansEnabled);
				command.Parameters.AddWithValue("@BannedIds", JsonConvert.SerializeObject(BannedIds, serializerSettings));
				command.Parameters.AddWithValue("@Admins", JsonConvert.SerializeObject(Admins, serializerSettings));
				command.Parameters.AddWithValue("@Tags", JsonConvert.SerializeObject(Tags, serializerSettings));
				return await command.ExecuteNonQueryAsync();
			}
			public ulong Id {get; set;}
			public bool ColorEnabled {get; set;}
			public bool ColorLocked {get; set;}
			public BlackListModeEnum ColorBlacklistMode {get; set;}
			public List<ulong> ColorBlacklist {get; set;}
			public bool BansEnabled {get; set;}
			public List<ulong> BannedIds {get; set;}
			public List<ulong> Admins {get; set;}
			public List<DiscordUrieTag> Tags {get; set;}
		}

	}
}