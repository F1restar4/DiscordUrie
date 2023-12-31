using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using DSharpPlus.Entities;
using DiscordUrie_DSharpPlus.Attributes;
using Microsoft.Extensions.Logging;

namespace DiscordUrie_DSharpPlus
{
	public partial class Commands : ApplicationCommandModule
	{
		[SlashCommandGroup("s", "Spam remove group"), RequireAuth]
		public class SpamStuffGroup : ApplicationCommandModule
		{

			[SlashCommand("add", "More spam")]
			public async Task SpamAddAsync(InteractionContext ctx, [Option("Phrase", "yes")] string Yes, [Option("count", "how many")] long Count)
			{
				await ctx.CreateResponseAsync(";)", ephemeral: true);
				for (int i = 0; i < Count; i++)
				{
					await ctx.Channel.SendMessageAsync(Yes);
					await Task.Delay(1000);
				}
			}

			[SlashCommand("remove", "Mass remove the last x number of messages")]
			public async Task SpamRemoveAsync(InteractionContext ctx, [Option("Amount", "Number of messages to remove")] long amount)
			{
				await ctx.CreateResponseAsync(";)", ephemeral: true);
				var Messages = ctx.Channel.GetMessagesBeforeAsync(ctx.InteractionId, Convert.ToInt32(amount));
				await ctx.Channel.DeleteMessagesAsync(Messages, $"Spam remove command by '{ctx.Member.DisplayName}'");
			}

			[SlashCommand("removeSearch", "Mass remove messages from a channel using the key as a search term")]
			public async Task SpamRemoveAsync(InteractionContext ctx, [Option("key", "The key to search for.")] string key, [Option("User", "A specific user to search for, this param is optional")] DiscordUser ByUser = null)
			{

				try
				{
					List<DiscordMessage> Messages = ctx.Channel.GetMessagesAsync().ToBlockingEnumerable().ToList();
					key = key.ToLower();

					bool ByUserInput = false;


					await ctx.DeferAsync();


					ByUserInput |= ByUser != null;
					IEnumerable<DiscordMessage> FilteredMessages;

					if (ByUserInput)
						FilteredMessages = Messages.Where(xr => xr.Content.ToLower().Contains(key) && xr.Author.Id == ByUser.Id);
					else
						FilteredMessages = Messages.Where(xr => xr.Content.ToLower().Contains(key));

					await ctx.Channel.DeleteMessagesAsync(FilteredMessages.ToList(), "Spam Removal Command Deletion");

					ctx.Client.Logger.Log(Microsoft.Extensions.Logging.LogLevel.Information, $"Removed {FilteredMessages.Count()} matching messages.");

					await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Removed {FilteredMessages.Count()} matching messages."));
					await Task.Delay(3000);
					await ctx.DeleteResponseAsync();
				}
				catch (Exception ex)
				{
					DiscordMessage ErrorMessage = await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Something went wrong: {ex.Message}"));
					ctx.Client.Logger.Log(LogLevel.Error, $"Error in /s remove; {ex.Message}");

					await Task.Delay(5000);

					await ctx.DeleteResponseAsync();

				}

			}
		}
	}
}