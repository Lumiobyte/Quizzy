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

        public int TotalScore { get; set; } = 0;

    }
}
