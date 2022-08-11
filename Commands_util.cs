using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using DSharpPlus.Entities;
using DiscordUrie_DSharpPlus.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace DiscordUrie_DSharpPlus
{
	public partial class Commands : ApplicationCommandModule
	{
		[SlashCommand("setmsg", "Sets the bot's activity"), SlashRequireOwner]
		public async Task SetMsg(InteractionContext ctx, [Option("Type", "The activity type")]ActivityType type, [Option("text", "The text to set")] string text)
		{
			var Activity = new DiscordActivity(text, (ActivityType)type);
			await ctx.Client.UpdateStatusAsync(Activity);
			discordUrie.BootConfig.StartupActivity = Activity;
			await DiscordUrieBootSettings.SaveBootConfig(discordUrie.BootConfig);
			await ctx.CreateResponseAsync("Done!", ephemeral: true);
		}

		[SlashCommand("uptime", "Displays the bot's uptime.")]
		public async Task UptimeAsync(InteractionContext ctx)
		{
			await ctx.CreateResponseAsync($"Program uptime: {await DateTime.Now.Subtract(discordUrie.StartTime).ToDuration()} \n"+
			$"Socket uptime: {await DateTime.Now.Subtract(discordUrie.SocketStart).ToDuration()}");
		}

		[SlashCommand("lookup", "Looks up info about a user")]
		public async Task LookupUserAsync(InteractionContext ctx, [Option("InputUser", "The user to lookup, can be an ID")]DiscordUser InputUser = null)
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

			await ctx.CreateResponseAsync(embed: EBuilder.Build());

		}
	
		[SlashCommand("shutdown", "Shuts down the bot"), SlashRequireOwner]
		public async Task ShutdownAsync(InteractionContext ctx)
		{
			await ctx.CreateResponseAsync("Shutting down...");
			await Task.Delay(3000);
			await ctx.Client.DisconnectAsync();
			Environment.Exit(0);
		}

		[SlashCommand("Reboot", "Restarts the bot"), SlashRequireOwner]
		public async Task RebootAsync(InteractionContext ctx)
		{
			await ctx.CreateResponseAsync("Rebooting...");
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
			public InteractionContext ctx;
			public DiscordUrieConfig settings;
			public DiscordUrie discordUrie;
		}

		[SlashCommand("eval", "Evaulates code"), SlashRequireOwner]
		public async Task Eval(InteractionContext ctx, [Option("code", "the code to execute")]string code)
		{

			var embedbuilder = new DiscordEmbedBuilder()
			{
				Title = "Evaluating.",
				Color = new DiscordColor(0, 255, 255),
			};
			await ctx.CreateResponseAsync(embedbuilder.Build());
			var msg = await ctx.GetOriginalResponseAsync();
			var globals = new globals
			{
				ctx = ctx,
				settings = this.discordUrie.Config,
				discordUrie = this.discordUrie
			};
			var ScriptOpt = ScriptOptions.Default.WithImports("System", "System.Collections.Generic", "System.Diagnostics", "System.Linq", "System.Net.Http", "System.Net.Http.Headers", 
				"System.Reflection", "System.Text", "System.Threading.Tasks", "DSharpPlus", "DSharpPlus.SlashCommands", "DSharpPlus.Entities", "DSharpPlus.EventArgs", "DSharpPlus.Exceptions")
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
				await msg.ModifyAsync(embedbuilder.Build());
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
			await msg.ModifyAsync(embedbuilder.Build());
		}
	}
}