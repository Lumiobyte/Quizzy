using Quizzy.Core.Entities;

namespace Quizzy.Core.Repositories
{
    public interface IQuizSessionRepository : IRepository<QuizSession>
    {
        QuizSession? GetByIdWithDetailsAsync(Guid id);
        QuizQuestion? GetFirstQuestion(QuizSession session);
        Task<QuizQuestion?> GetNextQuestion(QuizSession session, int currentOrderIndex);
    }
}
