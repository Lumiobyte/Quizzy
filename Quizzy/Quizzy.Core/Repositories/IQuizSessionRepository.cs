using Quizzy.Core.Entities;

namespace Quizzy.Core.Repositories
{
    public interface IQuizSessionRepository : IRepository<QuizSession>
    {
        Task<QuizSession?> GetByIdWithDetailsAsync(Guid id);
        QuizQuestion? GetFirstQuestion(QuizSession session);
        QuizQuestion? GetNextQuestion(QuizSession session, int currentOrderIndex);
    }
}
