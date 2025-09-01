using Quizzy.Core.Enums;

namespace Quizzy.Core.Entities
{
    public class QuizPlayer
    {

        public Guid Id { get; set; }
        public string Name { get; set; }

        public Guid UserAccountId { get; set; }
        public UserAccount UserAccount { get; set; }

        public Guid QuizSessionId { get; set; }
        public QuizSession QuizSession { get; set; }

        public ICollection<PlayerAnswer> Answers { get; set; } = new List<PlayerAnswer>();

        public ScoringStrategyType ScoringStrategy { get; set; }

        public string ScoringData { get; set; } = ""; // Scoring strategies can store different data here if they need it to persist between requests e.g. streak data

    }
}
