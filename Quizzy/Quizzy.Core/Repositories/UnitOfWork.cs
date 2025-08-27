namespace Quizzy.Core.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {

        private readonly QuizzyDbContext _dbContext;

        public IQuizRepository Quizzes { get; }
        public IQuizQuestionRepository QuizQuestions { get; }
        public IQuizAnswerRepository QuizAnswers { get; }
        public IQuizPlayerRepository QuizPlayers { get; }
        public IQuizSessionRepository QuizSessions { get; }
        public IPlayerAnswerRepository PlayerAnswers { get; }
        public IUserAccountRepository UserAccounts { get; }

        public UnitOfWork(QuizzyDbContext dbContext) { 
            _dbContext = dbContext;

            Quizzes = new QuizRepository(dbContext);
            QuizQuestions = new QuizQuestionRepository(dbContext);
            QuizAnswers = new QuizAnswerRepository(dbContext);
            QuizPlayers = new QuizPlayerRepository(dbContext);
            QuizSessions = new QuizSessionRepository(dbContext);
            PlayerAnswers = new PlayerAnswerRepository(dbContext);
            UserAccounts = new UserAccountRepository(dbContext);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // If there is any code we would like to always run before writing to the DB, we can call those functions here!
            // E.g. some kind of validation, logging, counters etc. If you not sure if this is the right place for it, ask Evan

            return _dbContext.SaveChangesAsync(cancellationToken);
        }

    }
}
