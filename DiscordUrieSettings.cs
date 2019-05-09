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

		public static DiscordUrieConfig CreateAllDefaultSettings(BaseDiscordClient client)
		{
			DiscordUrieConfig OutputConfig = new DiscordUrieConfig
			{

				StartupActivity = new DiscordActivity("the voices in my head", ActivityType.ListeningTo),

				GuildSettings = CreateGuildDefaultSettings(client.Guilds.Values)
			};


			return OutputConfig;
		}

		public static DiscordUrieGuild CreateGuildDefaultSettings(DiscordGuild Guild)
		{
			return new DiscordUrieGuild()
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
				}
			};
		}

		public static List<DiscordUrieGuild> CreateGuildDefaultSettings(IEnumerable<DiscordGuild> GuildList)
		{
			List<DiscordUrieGuild> OutputGuildList = new List<DiscordUrieGuild>();

			foreach (DiscordGuild cur in GuildList)
			{
				OutputGuildList.Add(CreateGuildDefaultSettings(cur));
			}


			return OutputGuildList;
		}

		public static DiscordUrieConfig? LoadSettings()
		{
			JsonSerializerSettings serializerSettings = new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore
			};
			return JsonConvert.DeserializeObject<DiscordUrieConfig?>(File.ReadAllText("config.json"), serializerSettings);
		}

		public struct DiscordUrieConfig
		{
			public bool IsEmpty()
			{
				if (GuildSettings == null)
				{
					return true;
				}
				if (StartupActivity == null)
				{
					return true;
				}
				return false;
			}

			public bool SaveSettings()
			{
				try
				{
					JsonSerializerSettings serializerSettings = new JsonSerializerSettings
					{
						NullValueHandling = NullValueHandling.Ignore
					};
					File.WriteAllText("config.json", JsonConvert.SerializeObject(this, Formatting.Indented, serializerSettings));
					return true;
				}
				catch
				{
					return false;
				}
			}

			public DiscordUrieGuild FindGuildSettings(DiscordGuild SearchForGuild)
			{
				if (GuildSettings.Any(xr => xr.ServerId == SearchForGuild.Id))
				{
					return GuildSettings.First(xr => xr.ServerId == SearchForGuild.Id);
				}
				else
				{
					DiscordUrieGuild NewDefaultServer = CreateGuildDefaultSettings(SearchForGuild);


					GuildSettings.Add(NewDefaultServer);
					SaveSettings();
					return NewDefaultServer;
				}
			}
			public List<ulong> GetChatBanIdList(DiscordGuild Guild)
			{

				DiscordUrieSettings.DiscordUrieGuild GuildSettings = FindGuildSettings(Guild);

				return GuildSettings.CBSettings.BannedIds;
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

		public struct DiscordUrieGuild
		{
			public ulong ServerId { get; set; }
			public ColorSettings CSettings { get; set; }
			public ChatBanSettings CBSettings { get; set; }
			public AdminSettings ASettings { get; set; }
		}

	}
}