using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using DiscordUrie_DSharpPlus.Attributes;
using Newtonsoft.Json;

namespace DiscordUrie_DSharpPlus
{
	public partial class Commands : ApplicationCommandModule
	{
		public DiscordUrie discordUrie { get; set; }

		public Commands(DiscordUrie du)
		{
			discordUrie = du;
		}
	}
}