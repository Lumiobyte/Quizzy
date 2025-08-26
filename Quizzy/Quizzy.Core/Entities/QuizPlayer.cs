namespace Quizzy.Core.Entities
{
    public class QuizPlayer
    {

        public int Id { get; set; }
        public string Name { get; set; }

        public int UserAccountId { get; set; }
        public UserAccount UserAccount { get; set; }

        public int QuizSessionId { get; set; }
        public QuizSession QuizSession { get; set; }

        public ICollection<QuizAnswer> Answers { get; set; } = new List<QuizAnswer>();

    }
}
