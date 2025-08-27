namespace Quizzy.Core.Repositories
{
    public interface IUnitOfWork
    {

        public IQuizRepository Quizzes { get; }
        public IQuizQuestionRepository QuizQuestions { get; }
        public IQuizAnswerRepository QuizAnswers { get; }
        public IQuizPlayerRepository QuizPlayers { get; }
        public IQuizSessionRepository QuizSessions { get; }
        public IPlayerAnswerRepository PlayerAnswers { get; }
        public IUserAccountRepository UserAccounts { get; }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default);

    }
}
