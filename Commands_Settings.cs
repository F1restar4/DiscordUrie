using System.Threading.Tasks;
using System.Linq;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DiscordUrie_DSharpPlus.Attributes;

namespace DiscordUrie_DSharpPlus
{
    public partial class Commands : BaseCommandModule
    {
        [Group("Settings"), RequireAuth]
        public class Settings : BaseCommandModule
        {
            private DiscordUrie discordUrie {get; set;}
            public Settings(DiscordUrie du)
            {
                this.discordUrie = du;
            }

            public async Task NotificationChannel(CommandContext ctx)
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
                        Out = $"set to {ctx.Guild.GetChannel(GuildSettings.NotificationChannel)}";
                        break;
                }
                await ctx.RespondAsync($"Notifications currently {Out}");
            }

            [Command("NotificationChannel")]
            public async Task NotificationChannel(CommandContext ctx, DiscordChannel channel)
            {
                var GuildSettings = await this.discordUrie.Config.FindGuildSettings(ctx.Guild);
                if (GuildSettings.NotificationChannel == channel.Id)
                    return;
                this.discordUrie.Config.GuildSettings.Remove(GuildSettings);
                GuildSettings.NotificationChannel = channel.Id;
                this.discordUrie.Config.GuildSettings.Add(GuildSettings);
                await GuildSettings.SaveGuild(this.discordUrie.SQLConn);
                await ctx.RespondAsync($"Notification channel set to {channel.Mention}");
            }

            [Command("NotificationChannel")]
            public async Task NotificationChannel(CommandContext ctx, string command)
            {
                if (command == "disable" || command == "off" || command == "false")
                {
                    var GuildSettings = await this.discordUrie.Config.FindGuildSettings(ctx.Guild);
                    if (GuildSettings.NotificationChannel == 0)
                        return;
                    this.discordUrie.Config.GuildSettings.Remove(GuildSettings);
                    GuildSettings.NotificationChannel = 0;
                    this.discordUrie.Config.GuildSettings.Add(GuildSettings);
                    await GuildSettings.SaveGuild(this.discordUrie.SQLConn);
                    await ctx.RespondAsync("Notifications disabled.");
                }
            }
        }

    }

}