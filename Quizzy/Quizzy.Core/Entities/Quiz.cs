namespace Quizzy.Core.Entities
{
    public class Quiz
    {

        public int Id { get; set; }
        public string Title { get; set; }

        public int QuizAuthorId { get; set; }
        public UserAccount QuizAuthor { get; set; }

        public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
        public ICollection<QuizSession> Sessions { get; set; } = new List<QuizSession>();

    }
}
