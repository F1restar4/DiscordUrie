using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

using Newtonsoft.Json;

namespace DiscordUrie_DSharpPlus
{
	public partial class Commands : ApplicationCommandModule
	{
		[SlashCommand("raw", "Supplies information about a givin object")]
		public async Task Raw(InteractionContext ctx, [Option("Object", "The target object")] SnowflakeObject obj)
			=> await ctx.CreateResponseAsync($"```\n{JsonConvert.SerializeObject(obj, Formatting.Indented)}\n```");
	}
}