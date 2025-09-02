using Quizzy.Core.Entities;

namespace Quizzy.Core.Repositories
{
    public interface IQuizRepository : IRepository<Quiz>
    {
        Task<Quiz?> GetByIdWithDetailsAsync(Guid id);
        Task<List<Quiz>?> GetAllWithDetailsAsync();
        Task LoadQuizQuestionsAsync(Quiz quiz);
        Task LoadQuizQuestionsWithAnswersAsync(Quiz quiz);
    }
}
