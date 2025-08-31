using Quizzy.Core.Entities;

namespace Quizzy.Core.DTOs
{
    public class QuizResponse
    {
        public QuizResponse(Quiz quiz)
        {
            name = quiz.Title ?? "Untitled";
            authorName = quiz.QuizAuthor?.Username ?? "Unknown";
            authorId = quiz.QuizAuthorId;
            questionsNum = quiz.Questions?.Count ?? 0;
            id = quiz.Id;
        }

        public string name { get; set; }
        public string authorName { get; set; }
        public Guid authorId { get; set; }
        public int questionsNum { get; set; }
        public Guid id { get; set; }
    }

    public class QuestionModel
    {
        public string Text { get; set; }
        public List<AnswerModel> Answers { get; set; }
    }

    public class AnswerModel
    {
        public string Text { get; set; }
        public bool IsCorrect { get; set; }
    }
}
