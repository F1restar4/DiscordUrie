using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DiscordUrie_DSharpPlus.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace DiscordUrie_DSharpPlus
{
	public partial class Commands : BaseCommandModule
	{
		[Command("setmsg"), RequireOwner]
		public async Task SetMsg(CommandContext ctx, int type, [RemainingText] string text)
		{
			var Activity = new DiscordActivity(text, (ActivityType)type);
			await ctx.Client.UpdateStatusAsync(Activity);
			discordUrie.Config.StartupActivity = Activity;
			await discordUrie.Config.SaveSettings(discordUrie.SQLConn);
			await ctx.Message.DeleteAsync();
		}

		[Command("uptime"), Description("Displays the bot's uptime.")]
		public async Task UptimeAsync(CommandContext ctx)
		{
			await ctx.RespondAsync($"Program uptime: {await DateTime.Now.Subtract(discordUrie.StartTime).ToDuration()} \n"+
			$"Socket uptime: {await DateTime.Now.Subtract(discordUrie.SocketStart).ToDuration()}");
		}

		[Command("lookup"), Description("Looks up info about a user")]
		public async Task LookupUserAsync(CommandContext ctx, [Description("The user to lookup, can be an ID")]DiscordUser InputUser = null)
		{
			if (InputUser == null)
			{
				InputUser = ctx.User;
			}

			DiscordMember member;

			try
			{
				member = await ctx.Guild.GetMemberAsync(InputUser.Id);
			}
			catch
			{
				member = null;
			}


			DiscordEmbedBuilder EBuilder = new DiscordEmbedBuilder()
			.WithAuthor($"{InputUser.Username}#{InputUser.Discriminator}")
			.WithImageUrl(InputUser.GetAvatarUrl(ImageFormat.Png, 64))
			.WithTimestamp(DateTime.Now)
			.AddField("\u200b", InputUser.Mention);
			if (member != null)
			{
				EBuilder.AddField("In current guild", "true");
			}
			else
			{
				EBuilder.AddField("In current guild", "false");
			}

			await ctx.RespondAsync(embed: EBuilder.Build());

		}
	
		[Command("shutdown"), Hidden, RequireOwner]
		public async Task ShutdownAsync(CommandContext ctx)
		{
			DiscordMessage HelpThanks = await ctx.RespondAsync("Shutting down...");
			await Task.Delay(3000);
			await ctx.Message.DeleteAsync("Command auto deletion.");
			await HelpThanks.DeleteAsync("Command auto deletion.");
			await ctx.Client.DisconnectAsync();
			Environment.Exit(0);
		}

		[Command("Reboot"), RequireOwner, Hidden]
		public async Task RebootAsync(CommandContext ctx)
		{
			await ctx.RespondAsync("Rebooting...");
			await Task.Delay(3000);
			await ctx.Client.DisconnectAsync();
			try
			{
				var Process = new Process();
				var ProcessStartInfo = new ProcessStartInfo()
				{
					WindowStyle = ProcessWindowStyle.Hidden,
					FileName = "/bin/bash",
					WorkingDirectory = "/home/bot",
					Arguments = "-c ./RebootBot.sh"
				};
				Process.StartInfo = ProcessStartInfo;
				Process.Start();
				Environment.Exit(0);
			}
			catch
			{

			}
		}

		public class globals
		{
			public CommandContext ctx;
			public DiscordUrieConfig settings;
			public DiscordUrie discordUrie;
		}

		[Command("eval")]
		public async Task Eval(CommandContext ctx, [RemainingText] string code)
		{
			var yes = code.IndexOf("```") + 3;
			yes = code.IndexOf('\n', yes) + 1;
			var alsoyes = code.LastIndexOf("```");
			if (yes == -1 || alsoyes == -1)
			{
				await ctx.RespondAsync("You need to wrap the code in a code block");
				return;
			}

			code = code.Substring(yes, alsoyes - yes);
			var embedbuilder = new DiscordEmbedBuilder()
			{
				Title = "Evaluating.",
				Color = new DiscordColor(0, 255, 255),
			};
			var response = await ctx.RespondAsync(embedbuilder.Build());
			var globals = new globals
			{
				ctx = ctx,
				settings = this.discordUrie.Config,
				discordUrie = this.discordUrie
			};
			var ScriptOpt = ScriptOptions.Default.WithImports("System", "System.Collections.Generic", "System.Diagnostics", "System.Linq", "System.Net.Http", "System.Net.Http.Headers", 
				"System.Reflection", "System.Text", "System.Threading.Tasks", "DSharpPlus", "DSharpPlus.CommandsNext", "DSharpPlus.Entities", "DSharpPlus.EventArgs", "DSharpPlus.Exceptions")
				.WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location))); // I have no idea what this does or why it fixes imports but /shrug
			Object result;
			try
			{
				result = await CSharpScript.EvaluateAsync(code, ScriptOpt, globals, typeof(globals));
			}
			catch (CompilationErrorException ex)
			{
				embedbuilder = new DiscordEmbedBuilder()
				{
					Title = "An error occurred",
					Color = new DiscordColor(255, 0, 0),
					Description = string.Join('\n', ex.Diagnostics.Take(3))
				};
				await response.ModifyAsync(embedbuilder.Build());
				return;
			}

			embedbuilder = new DiscordEmbedBuilder()
			{
				Title = "Evaluation successful",
				Color = new DiscordColor(0, 255, 0),
			};
			embedbuilder.AddField("Result", result != null ? result.ToString() : "Code didn't return a value");
			if (result != null)
				embedbuilder.AddField("Return type", result.GetType().ToString());
			await response.ModifyAsync(embedbuilder.Build());
		}
	}
}