namespace Quizzy.Core.Entities
{
    public class QuizAnswer
    {

        public Guid Id { get; set; }
        public string Text { get; set; }

        public Guid QuestionId { get; set; }
        public QuizQuestion Question { get; set; }

        public bool IsCorrect { get; set; }

    }
}
