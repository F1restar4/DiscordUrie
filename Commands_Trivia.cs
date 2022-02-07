using System;
using System.Linq;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firestar4.OpenTDBWrapper;

namespace DiscordUrie_DSharpPlus
{
	public partial class Commands : BaseCommandModule
	{
        List<String> Insults = new List<string>
        {
            "dumbass",
            "shit face",
            "brainless",
            "republican",
            "shit ass",
            "stupid",
            "dingus",
            "bozo",
            "dipstick",
            "fart head",
            "bird brain",
            "cracker",
            "bonehead",
            "neanderthal",
            "cave man"
        };

        List<String> SmartyPantsWords = new List<string>
        {
            "smarty pants",
            "nerd",
            "giga brain",
            "guy who googled the answer",
            "genius",
            "smarty head",
            "hyper chad",
            "smartass",
            "cheater",
            "intellectual",
            

        };
        [Command("trivia")]
        public async Task TriviaQuestion(CommandContext ctx)
        {
            var question = await OpenTDBWrapper.GetQuestionAsync();
            List<string> AllAnswers = question.IncorrectAnswers;
            AllAnswers.Add(question.CorrectAnswer);
            AllAnswers = AllAnswers.OrderBy(a => Guid.NewGuid()).ToList();
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder
            {
                Title = $"Trivia Question for {ctx.Member.DisplayName}",
            };
            builder.AddField(question.Question, "You have twelve seconds.");
            builder.AddField("Difficulty", $"`{question.Difficulty}`", true);
            builder.AddField("Category", $"`{question.Category}`", true);
            builder.WithColor(new DiscordColor("00ffff"));
            DiscordMessageBuilder messageBuilder = new DiscordMessageBuilder();
            var embed = builder.Build();
            messageBuilder.WithEmbed(embed);
            List<DiscordComponent> Buttons = new List<DiscordComponent>();
            foreach (var cur in AllAnswers)
                Buttons.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, cur, cur));

            messageBuilder.AddComponents(Buttons.ToArray());

            var message = await ctx.RespondAsync(messageBuilder);
            var Interaction = await message.WaitForButtonAsync(ctx.Member, TimeSpan.FromSeconds(12));
            if (Interaction.TimedOut)
            {
                messageBuilder.ClearComponents();
                Buttons.Clear();
                foreach (var cur in AllAnswers)
                {
                    if (cur == question.CorrectAnswer)
                    {
                        Buttons.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Success, $"{cur}", cur, true));
                        continue;
                    }
                    Buttons.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, cur, cur, true));
                }
                messageBuilder.AddComponents(Buttons.ToArray());
                messageBuilder.WithContent($"Looks like you ran out of time, {Insults.OrderBy(x => Guid.NewGuid()).First()}.");
                await message.ModifyAsync(messageBuilder);
                return;
            }

            var Result = Interaction.Result;
            if (Result.Id == question.CorrectAnswer)
            {
                Buttons.Clear();
                foreach (var cur in AllAnswers)
                {
                    if (cur == question.CorrectAnswer)
                    {
                        Buttons.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Success, cur, cur, true));
                        continue;
                    }
                    Buttons.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, cur, cur, true));
                }
                await Result.Interaction.CreateResponseAsync(DSharpPlus.InteractionResponseType.UpdateMessage, 
                new DiscordInteractionResponseBuilder().AddComponents(Buttons.ToArray()).WithContent($"Good job, {SmartyPantsWords.OrderBy(x => Guid.NewGuid()).First()}, that's correct.").AddEmbed(embed));
                return;
            }

            Buttons.Clear();
            foreach (var cur in AllAnswers)
            {
                if (cur == question.CorrectAnswer)
                {
                        Buttons.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Success, cur, cur, true));
                        continue;
                }
                if (cur == Result.Id)
                {
                        Buttons.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Danger, cur, cur, true));
                        continue;
                }
                Buttons.Add(new DiscordButtonComponent(DSharpPlus.ButtonStyle.Secondary, cur, cur, true));
            }
            await Result.Interaction.CreateResponseAsync(DSharpPlus.InteractionResponseType.UpdateMessage,
            new DiscordInteractionResponseBuilder().AddComponents(Buttons.ToArray()).WithContent($"Thats wrong, {Insults.OrderBy(x => Guid.NewGuid()).First()}. The correct answer was `{question.CorrectAnswer}`").AddEmbed(embed));
        }
    }

}