using Quizzy.Core.Entities;

namespace Quizzy.Core.Repositories
{
    public interface IQuizPlayerRepository : IRepository<QuizPlayer>
    {
        Task LoadPlayerAnswersAsync(QuizPlayer player);
        Task LoadPlayerAnswersWithQuizAnswersAsync(QuizPlayer player);
    }
}
