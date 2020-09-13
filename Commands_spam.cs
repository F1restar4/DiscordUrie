using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DiscordUrie_DSharpPlus.Attributes;
using Microsoft.Extensions.Logging;

namespace DiscordUrie_DSharpPlus
{
	public partial class Commands : BaseCommandModule
	{
		[Group("spam"), Description("Spam remove group"), Aliases("s"), RequireAuth]
		public class SpamStuffGroup : BaseCommandModule
		{

			[Command("add")]
			public async Task SpamAddAsync(CommandContext ctx, string Yes, int Count)
			{
				await ctx.Message.DeleteAsync();

				for (int i = 0; i < Count; i++)
				{
					await ctx.RespondAsync(Yes);
					await Task.Delay(1000);
				}
			}

			[Command("remove"), Description("Mass remove the last x number of messages")]
			public async Task SpamRemoveAsync(CommandContext ctx, int amount)
			{
				IReadOnlyList<DiscordMessage> Messages = await ctx.Channel.GetMessagesBeforeAsync(ctx.Message.Id, amount);
				await ctx.Channel.DeleteMessagesAsync(Messages, $"Spam remove command by '{ctx.Member.DisplayName}'");
				await ctx.Message.DeleteAsync();	
			}

			[Command("remove")]
			[Description("Mass remove messages from a channel using the key as a search term")]
			public async Task SpamRemoveAsync(CommandContext ctx, [Description("The key to search for.")] string key, [Description("A specific user to search for, this param is optional")] DiscordMember ByUser = null)
			{

				try
				{
					IReadOnlyList<DiscordMessage> Messages = await ctx.Channel.GetMessagesAsync();
					DiscordMessage InitRemoveMessage;
					key = key.ToLower();

					bool ByUserInput = false;


					InitRemoveMessage = await ctx.RespondAsync("Checking messages...");


					ByUserInput |= ByUser != null;
					IEnumerable<DiscordMessage> FilteredMessages;

					if (ByUserInput)
						FilteredMessages = Messages.Where(xr => xr.Content.ToLower().Contains(key) && xr.Author.Id == ByUser.Id && xr != ctx.Message);
					else
						FilteredMessages = Messages.Where(xr => xr.Content.ToLower().Contains(key) && xr != ctx.Message);

					await ctx.Channel.DeleteMessagesAsync(FilteredMessages, "Spam Removal Command Deletion");

					ctx.Client.Logger.Log(Microsoft.Extensions.Logging.LogLevel.Information, "Discord Urie", $"Removed {FilteredMessages.Count()} matching messages.");

					await InitRemoveMessage.ModifyAsync($"Removed {FilteredMessages.Count()} matching messages.");
					await Task.Delay(3000);
					await InitRemoveMessage.DeleteAsync("Command auto deletion.");

					await ctx.Message.DeleteAsync();
				}
				catch (Exception ex)
				{
					DiscordMessage ErrorMessage = await ctx.RespondAsync($"Something went wrong: {ex.Message}");
					ctx.Client.Logger.Log(LogLevel.Error, "Dicord Urie", $"Error in /s remove; {ex.Message}");

					await Task.Delay(5000);

					await ErrorMessage.DeleteAsync("Command auto deletion.");
					await ctx.Message.DeleteAsync("Command auto deletion.");

				}

			}
		}
	}
}