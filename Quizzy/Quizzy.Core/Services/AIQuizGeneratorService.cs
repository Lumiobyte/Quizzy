using OpenAI;
using OpenAI.Chat;
using Quizzy.Core.Entities;
using Quizzy.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            var messages = new List<ChatMessage>
            {
                //new ChatMessage(ChatMessageRole.System, systemPrompt),
                //new ChatMessage(ChatMessageRole.User, $"Create a quiz themed around: {prompt}") // prompt injection up the wazoo with this one
            };

            //var chatRequest = new ChatCompletionOptions
            //{
            //    Model = "gpt-4", // figure out who is cheapest??
            //    Messages = { messages[0], messages[1] },
            //    Temperature = 0.7
            //};

            // return quiz;

            return new Quiz { };
        }

    }
}
