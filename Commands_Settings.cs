using System.Threading.Tasks;
using System.Linq;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using DSharpPlus.Entities;
using DiscordUrie_DSharpPlus.Attributes;

namespace DiscordUrie_DSharpPlus
{
	public partial class Commands : ApplicationCommandModule
	{
		[SlashCommandGroup("Settings", "Various bot settings"), RequireAuth]
		public class Settings : ApplicationCommandModule
		{
			private DiscordUrie discordUrie { get; set; }
			public Settings(DiscordUrie du)
			{
				this.discordUrie = du;
			}

			[SlashCommand("ToggleMusic", "Globally toggles music functionality"), SlashRequireOwner]
			public async Task ToggleMusic(InteractionContext ctx)
			{
				discordUrie.BootConfig.MusicEnabled = !discordUrie.BootConfig.MusicEnabled;
				await DiscordUrieBootSettings.SaveBootConfig(discordUrie.BootConfig);
				await ctx.CreateResponseAsync($"Music enabled set to {discordUrie.BootConfig.MusicEnabled}.", ephemeral: true);
			}

			[SlashCommand("GetNotificationChannel", "Gets the notification channel")]
			public async Task GetNotificationChannel(InteractionContext ctx)
			{
				var GuildSettings = await this.discordUrie.Config.FindGuildSettings(ctx.Guild);
				string Out;
				switch (GuildSettings.NotificationChannel)
				{
					case 0:
						Out = "disabled.";
						break;
					case 1:
						Out = $"set to {ctx.Guild.GetDefaultChannel().Mention}";
						break;
					default:
						Out = $"set to {ctx.Guild.GetChannel(GuildSettings.NotificationChannel).Mention}";
						break;
				}
				await ctx.CreateResponseAsync($"Notifications currently {Out}");
			}

			[SlashCommand("NotificationChannel", "Sets the notification channel")]
			public async Task NotificationChannel(InteractionContext ctx, [Option("Channel", "The channel to send notifications to")] DiscordChannel channel)
			{
				var GuildSettings = await this.discordUrie.Config.FindGuildSettings(ctx.Guild);
				if (GuildSettings.NotificationChannel == channel.Id)
					return;
				this.discordUrie.Config.GuildSettings.Remove(GuildSettings);
				GuildSettings.NotificationChannel = channel.Id;
				this.discordUrie.Config.GuildSettings.Add(GuildSettings);
				await GuildSettings.SaveGuild(this.discordUrie.SQLConn);
				await ctx.CreateResponseAsync($"Notification channel set to {channel.Mention}");
			}

			[SlashCommand("DisableNotificationChannel", "Disables notifications")]
			public async Task DisableNotificationChannel(InteractionContext ctx)
			{
				var GuildSettings = await this.discordUrie.Config.FindGuildSettings(ctx.Guild);
				if (GuildSettings.NotificationChannel == 0)
					return;
				this.discordUrie.Config.GuildSettings.Remove(GuildSettings);
				GuildSettings.NotificationChannel = 0;
				this.discordUrie.Config.GuildSettings.Add(GuildSettings);
				await GuildSettings.SaveGuild(this.discordUrie.SQLConn);
				await ctx.CreateResponseAsync("Notifications disabled.");
			}

			[SlashCommand("GetAutorole", "Gets the autorole setting")]
			public async Task GetAutoRole(InteractionContext ctx)
			{
				var GuildSettings = await this.discordUrie.Config.FindGuildSettings(ctx.Guild);
				if (GuildSettings.AutoRole == 0)
				{
					await ctx.CreateResponseAsync("Autorole is currently disabled.");
				}
				else
				{
					var role = ctx.Guild.GetRole(GuildSettings.AutoRole);
					await ctx.CreateResponseAsync($"Autorole is set to {role.Mention}");
				}
			}

			[SlashCommand("Autorole", "Sets the auto role")]
			public async Task AutoRole(InteractionContext ctx, [Option("Role", "The role to give to users when they join")] DiscordRole role)
			{
				var GuildSettings = await this.discordUrie.Config.FindGuildSettings(ctx.Guild);
				if (GuildSettings.AutoRole == role.Id)
					return;
				this.discordUrie.Config.GuildSettings.Remove(GuildSettings);
				GuildSettings.AutoRole = role.Id;
				this.discordUrie.Config.GuildSettings.Add(GuildSettings);
				await GuildSettings.SaveGuild(this.discordUrie.SQLConn);
				await ctx.CreateResponseAsync($"Autorole set to {role.Mention}");
			}

			[SlashCommand("DisableAutorole", "Disables autorole")]
			public async Task DisableAutoRole(InteractionContext ctx)
			{
				var GuildSettings = await this.discordUrie.Config.FindGuildSettings(ctx.Guild);
				if (GuildSettings.AutoRole == 0)
					return;
				this.discordUrie.Config.GuildSettings.Remove(GuildSettings);
				GuildSettings.AutoRole = 0;
				this.discordUrie.Config.GuildSettings.Add(GuildSettings);
				await GuildSettings.SaveGuild(this.discordUrie.SQLConn);
				await ctx.CreateResponseAsync("Autorole disabled");
			}

		}

	}

}