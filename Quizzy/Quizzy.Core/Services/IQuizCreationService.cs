using Quizzy.Core.DTOs;
using System;

namespace Quizzy.Core.Services
{
    public interface IQuizCreationService
    {
        public Task GenerateQuiz(QuizCreatorModel model, Guid creatorId);
        public Task UpdateQuiz(QuizCreatorModel model, Guid creatorId); // Quiz to update is identified by model.QuizSourceId
    }
}
