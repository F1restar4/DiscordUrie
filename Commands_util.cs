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

		[Command("uptime")]
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

		public class globals
		{
			public CommandContext ctx;
			public DiscordUrieConfig settings;
		}

		[Command("eval"), Description("Evaluates a snippet of C# code, in context."), Hidden, RequireOwner]
       	public async Task EvaluateAsync(CommandContext ctx, [RemainingText, Description("Code to evaluate.")] string code)
        {
            var cs1 = code.IndexOf("```") + 3;
            cs1 = code.IndexOf('\n', cs1) + 1;
            var cs2 = code.LastIndexOf("```");

            if (cs1 == -1 || cs2 == -1)
               	throw new ArgumentException("You need to wrap the code into a code block.", nameof(code));

            code = code.Substring(cs1, cs2 - cs1);

            var embed = new DiscordEmbedBuilder
            {
                Title = "Evaluating...",
                Color = new DiscordColor(0xD091B2)
            };
            var msg = await ctx.RespondAsync("", embed: embed.Build()).ConfigureAwait(false);

            var globals = new globals
			{
				ctx = ctx,
				settings = discordUrie.Config
			};
            var sopts = ScriptOptions.Default
                .WithImports("System", "System.Collections.Generic", "System.Diagnostics", "System.Linq", "System.Net.Http", "System.Net.Http.Headers", "System.Reflection", "System.Text", 
                             "System.Threading.Tasks", "DSharpPlus", "DSharpPlus.CommandsNext", "DSharpPlus.Entities", "DSharpPlus.EventArgs", "DSharpPlus.Exceptions")
                .WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)));
            
            var sw1 = Stopwatch.StartNew();
            var cs = CSharpScript.Create(code, sopts, typeof(globals));
            var csc = cs.Compile();
            sw1.Stop();
            
            if (csc.Any(xd => xd.Severity == DiagnosticSeverity.Error))
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = "Compilation failed",
                    Description = string.Concat("Compilation failed after ", sw1.ElapsedMilliseconds.ToString("#,##0"), "ms with ", csc.Length.ToString("#,##0"), " errors."),
                    Color = new DiscordColor(255,0,0)
                };
                foreach (var xd in csc.Take(3))
                {
                    var ls = xd.Location.GetLineSpan();
                    embed.AddField(string.Concat("Error at ", ls.StartLinePosition.Line.ToString("#,##0"), ", ", ls.StartLinePosition.Character.ToString("#,##0")), Formatter.InlineCode(xd.GetMessage()), false);
                }
                if (csc.Length > 3)
                {
                    embed.AddField("Some errors ommited", string.Concat((csc.Length - 3).ToString("#,##0"), " more errors not displayed"), false);
                }
                await msg.ModifyAsync(embed: embed.Build()).ConfigureAwait(false);
                return;
            }

            Exception rex = null;
            ScriptState<object> css = null;
            var sw2 = Stopwatch.StartNew();
            try
            {
                css = await cs.RunAsync(globals).ConfigureAwait(false);
                rex = css.Exception;
            }
            catch (Exception ex)
            {
                rex = ex;
            }
            sw2.Stop();

            if (rex != null)
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = "Execution failed",
                    Description = string.Concat("Execution failed after ", sw2.ElapsedMilliseconds.ToString("#,##0"), "ms with `", rex.GetType(), ": ", rex.Message, "`."),
                    Color = new DiscordColor(255,0,0),
                };
                await msg.ModifyAsync(embed: embed.Build()).ConfigureAwait(false);
                return;
            }

            // execution succeeded
            embed = new DiscordEmbedBuilder
            {
                Title = "Evaluation successful",
                Color = new DiscordColor(0,255,0),
            };

            embed.AddField("Result", css.ReturnValue != null ? css.ReturnValue.ToString() : "No value returned", false)
                .AddField("Compilation time", string.Concat(sw1.ElapsedMilliseconds.ToString("#,##0"), "ms"), true)
                .AddField("Execution time", string.Concat(sw2.ElapsedMilliseconds.ToString("#,##0"), "ms"), true);

            if (css.ReturnValue != null)
                embed.AddField("Return type", css.ReturnValue.GetType().ToString(), true);

            await msg.ModifyAsync(embed: embed.Build()).ConfigureAwait(false);
		}

    }
}