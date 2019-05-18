using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using DSharpPlus;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;

namespace DiscordUrie_DSharpPlus
{
	public class DiscordUrieSettings
	{

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
				ServerId = Guild.Id,
				CSettings = new ColorSettings()
				{
					Enabled = true,
					Locked = false,
					BlacklistMode = BlackListModeEnum.Off,
					Blacklist = new List<ulong>()
				},

				CBSettings = new ChatBanSettings()
				{
					Enabled = false,
					BannedIds = new List<ulong>()
				},

				ASettings = new AdminSettings()
				{
					Admins = new List<ulong>()
				},

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

		public static async Task<DiscordUrieConfig?> LoadSettings()
		{
			JsonSerializerSettings serializerSettings = new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore
			};
			return JsonConvert.DeserializeObject<DiscordUrieConfig?>(await File.ReadAllTextAsync("config.json"), serializerSettings);
		}

		public struct DiscordUrieConfig
		{
			public Task<bool> IsEmpty()
			{
				if (GuildSettings == null || StartupActivity == null)
				{
					return Task.FromResult(true);
				}
				return Task.FromResult(false);
			}

			public async Task<bool> SaveSettings()
			{
				try
				{
					JsonSerializerSettings serializerSettings = new JsonSerializerSettings
					{
						NullValueHandling = NullValueHandling.Ignore
					};
					await File.WriteAllTextAsync("config.json", JsonConvert.SerializeObject(this, Formatting.Indented, serializerSettings));
					return true;
				}
				catch
				{
					return false;
				}
			}

			public async Task<bool> RemoveGuild(ulong guildid)
			{
				bool result = GuildSettings.Remove(GuildSettings.First(xr => xr.ServerId == guildid));
				await this.SaveSettings();
				return result;
			}

			public async Task<bool> AddGuild(DiscordGuild guild)
			{
				if (GuildSettings.Any(xr => xr.ServerId == guild.Id))
					return false;

				GuildSettings.Add(await CreateGuildDefaultSettings(guild));
				await this.SaveSettings();
				return true;
			}

			public async Task<DiscordUrieGuild> FindGuildSettings(DiscordGuild SearchForGuild)
			{
				if (GuildSettings.Any(xr => xr.ServerId == SearchForGuild.Id))
				{
					return GuildSettings.First(xr => xr.ServerId == SearchForGuild.Id);
				}
				else
				{
					DiscordUrieGuild NewDefaultServer = await CreateGuildDefaultSettings(SearchForGuild);


					GuildSettings.Add(NewDefaultServer);
					await SaveSettings();
					return NewDefaultServer;
				}
			}

			public async Task<List<ulong>> GetChatBanIdList(DiscordGuild Guild)
			{

				DiscordUrieSettings.DiscordUrieGuild GuildSettings = await FindGuildSettings(Guild);

				return GuildSettings.CBSettings.BannedIds;
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

		public struct ColorSettings
		{
			public bool Enabled { get; set; }
			public bool Locked { get; set; }
			public BlackListModeEnum BlacklistMode { get; set; }
			public List<ulong> Blacklist { get; set; }
		}

		public struct ChatBanSettings
		{
			public bool Enabled { get; set; }
			public List<ulong> BannedIds { get; set; }
		}

		public struct AdminSettings
		{
			public List<ulong> Admins { get; set; }
		}

		public struct DiscordUrieTag
		{
			public string Tag {get; set;}
			public string Output {get; set;}
			public ulong Owner {get; set;}
		}

		public struct DiscordUrieGuild
		{
			public ulong ServerId { get; set; }
			public ColorSettings CSettings { get; set; }
			public ChatBanSettings CBSettings { get; set; }
			public AdminSettings ASettings { get; set; }
			
			public List<DiscordUrieTag> Tags { get; set;}
		}

	}
}