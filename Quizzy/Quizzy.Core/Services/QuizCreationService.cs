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
            repository.Quizzes.Update(CreateQuizFromModel(model, creatorId));
            await repository.SaveChangesAsync();
        }

        public async Task DeleteQuiz(Guid id)
        {
            await repository.Quizzes.Remove(id);
            await repository.SaveChangesAsync();
        }

        async Task AddNewQuizToDB(QuizCreatorModel model, Guid creatorId)
        {
            var newQuiz = CreateQuizFromModel(model, creatorId);
            await repository.Quizzes.AddAsync(newQuiz);
            await repository.SaveChangesAsync();
        }

        Quiz CreateQuizFromModel(QuizCreatorModel model, Guid creatorId)
        {
            var quiz = new Quiz
            {
                Id = model.QuizSourceId ?? Guid.NewGuid(),
                Title = model.Title,
                QuizAuthorId = creatorId,
                Questions = model.Questions.Select(q => new QuizQuestion
                {
                    Id = Guid.NewGuid(),
                    Text = q.Text,
                    QuestionType = q.Answers.Any(a => a.IsCorrect) ? Enums.QuestionType.MultipleChoice : Enums.QuestionType.ShortAnswer,
                    Answers = q.Answers.Select(a => new QuizAnswer
                    {
                        Id = Guid.NewGuid(),
                        Text = a.Text,
                        IsCorrect = a.IsCorrect
                    }).ToList()
                }).ToList()
            };
            foreach (var question in quiz.Questions)
            {
                question.QuizId = quiz.Id;
                foreach (var answer in question.Answers)
                {
                    answer.QuestionId = question.Id;
                    if (question.QuestionType == Enums.QuestionType.ShortAnswer) answer.IsCorrect = true;
                }
            }
            return quiz;
        }
    }
}
