namespace Quizzy.Core.Entities
{
    public class PlayerAnswer
    {

        public Guid Id { get; set; }

        public Guid PlayerId { get; set; }
        public QuizPlayer Player { get; set; }

        public Guid QuestionId { get; set; }
        public QuizQuestion Question { get; set; }

        public Guid AnswerId { get; set; }
        public QuizAnswer Answer { get; set; }

        public DateTime DateTime { get; set; }
        public int PointsValue { get; set; } = 0;

    }
}
