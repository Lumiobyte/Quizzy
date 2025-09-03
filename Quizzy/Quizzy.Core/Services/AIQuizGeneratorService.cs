using OpenAI;
using OpenAI.Chat;
using Quizzy.Core.Entities;
using Quizzy.Core.Enums;
using Quizzy.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Quizzy.Core.Services
{
    public class AIQuizGeneratorService : IAIQuizGeneratorService
    {

        const string systemPrompt = """
            You are an AI quiz generator for an online quiz website. Please respond only with JSON representing the quiz. Do not include explanations or ANY OTHER TEXT.
            You can use either MULTIPLE CHOICE questions featuring between 2 and 4 possible answers with one correct. Alternatively you can use SHORT ANSWER questions which are 1-3 words, one word is best. SHORT ANSWER questions do not have "incorrect" choices. All choices are correct, any answer that is not equal to a choice will be considered incorrect.
            The JSON format looks like: { 1: {"type": "mcq", "text": "What is the answer?", "choices": [{"text": "This one", "correct": false}, {"text": "This other one", "correct": true}] }, 2: {"type": "short", "text": "Name a fruit", "choices": ["apple", "orange"] } }
            """;

        readonly OpenAIClient _openAiClient;
        IUnitOfWork _unitOfWork;

        public AIQuizGeneratorService(IUnitOfWork unitOfWork, OpenAIClient openAiClient)
        {
            _openAiClient = openAiClient;
            _unitOfWork = unitOfWork;
        }

        public async Task<Quiz> AIGenerateQuiz(string prompt)
        {

            var chatClient = _openAiClient.GetChatClient("gpt-4o-mini");
            var response = await chatClient.CompleteChatAsync(new ChatMessage[]
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage($"Create a quiz themed around: {prompt}") // prompt injection up the wazoo with this one
            });

            var quizObject = MapFromJson(response.Value.Content[0].Text, prompt + " Quiz");

            return quizObject;
        }

        Quiz MapFromJson(string json, string quizTitle)
        {
            var doc = JsonDocument.Parse(json);

            var quiz = new Quiz
            {
                Id = Guid.NewGuid(),
                Title = quizTitle,
                Questions = new List<QuizQuestion>()
            };

            int orderIndex = 0;

            foreach (var questionProp in doc.RootElement.EnumerateObject())
            {
                var qObj = questionProp.Value;
                string type = qObj.GetProperty("type").GetString() ?? "";
                string text = qObj.GetProperty("text").GetString() ?? "";

                var quizQuestion = new QuizQuestion
                {
                    Id = Guid.NewGuid(),
                    Text = text,
                    OrderIndex = orderIndex++,
                    QuestionType = type.ToLower() switch
                    {
                        "mcq" => QuestionType.MultipleChoice,
                        "short" => QuestionType.ShortAnswer,
                        _ => QuestionType.MultipleChoice
                    },
                    Answers = new List<QuizAnswer>()
                };

                if (type == "mcq")
                {
                    foreach (var choice in qObj.GetProperty("choices").EnumerateArray())
                    {
                        quizQuestion.Answers.Add(new QuizAnswer
                        {
                            Id = Guid.NewGuid(),
                            Text = choice.GetProperty("text").GetString() ?? "",
                            IsCorrect = choice.GetProperty("correct").GetBoolean()
                        });
                    }
                }
                else if (type == "short")
                {
                    foreach (var choice in qObj.GetProperty("choices").EnumerateArray())
                    {
                        quizQuestion.Answers.Add(new QuizAnswer
                        {
                            Id = Guid.NewGuid(),
                            Text = choice.GetString() ?? "",
                            IsCorrect = true // all answer objects for a short answer question are marked correct
                        });
                    }
                }

                quiz.Questions.Add(quizQuestion);
            }

            return quiz;
        }

    }
}
