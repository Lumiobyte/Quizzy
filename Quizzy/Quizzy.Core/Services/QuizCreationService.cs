using Quizzy.Core.Entities;
using Quizzy.Core.Repositories;
using Quizzy.Models;

namespace Quizzy.Core.Services
{
    public class QuizCreationService(IUnitOfWork repository) : IQuizCreationService
    {
        public async Task GenerateQuiz(QuizCreatorModel model, Guid creatorId, bool createNew)
        {
            if (createNew && model.QuizSourceId is not null) await UpdateQuiz(model, creatorId);
            else await AddNewQuizToDB(model, creatorId);
        }

        public async Task UpdateQuiz(QuizCreatorModel model, Guid creatorId)
        {
            if (model.QuizSourceId is null) throw new ArgumentException("QuizSourceId cannot be null when updating a quiz.");
            await DeleteQuiz(model.QuizSourceId!.Value);
            await AddNewQuizToDB(model, creatorId);
        }

        public async Task DeleteQuiz(Guid id)
        {
            await repository.Quizzes.Remove(id);
            await repository.SaveChangesAsync();
        }

        async Task AddNewQuizToDB(QuizCreatorModel model, Guid creatorId)
        {
            var newQuiz = new Quiz
            {
                Id = model.QuizSourceId ?? Guid.NewGuid(),
                Title = model.Title,
                QuizAuthorId = creatorId,
                Questions = model.Questions.Select(q => new QuizQuestion
                {
                    Id = Guid.NewGuid(),
                    Text = q.Text,
                    Answers = q.Answers.Select(a => new QuizAnswer
                    {
                        Id = Guid.NewGuid(),
                        Text = a.Text,
                        IsCorrect = a.IsCorrect
                    }).ToList()
                }).ToList()
            };
            foreach (var question in newQuiz.Questions)
            {
                question.QuizId = newQuiz.Id;
                foreach (var answer in question.Answers)
                {
                    answer.QuestionId = question.Id;
                }
            }
            await repository.Quizzes.AddAsync(newQuiz);
            await repository.SaveChangesAsync();
        }
    }
}
