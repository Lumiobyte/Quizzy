namespace Quizzy.Core.Entities
{
    public class QuizAnswer
    {

        public int Id { get; set; }
        public string Text { get; set; }

        public int QuestionId { get; set; }
        public QuizQuestion Question { get; set; }

        public bool IsCorrect { get; set; }

    }
}
