using Quizzy.Core.Enums;

namespace Quizzy.Core.Entities
{
    public class QuizQuestion
    {

        public Guid Id { get; set; }
        public string Text { get; set; }

        public int OrderIndex { get; set; }

        public Guid QuizId { get; set; }
        public Quiz Quiz { get; set; }

        public QuestionType QuestionType { get; set; }

        public ICollection<QuizAnswer> Answers { get; set; } = new List<QuizAnswer>();

    }
}
