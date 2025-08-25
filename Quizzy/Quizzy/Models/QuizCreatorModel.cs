using System.Collections.Generic;

namespace Quizzy.Models
{
    public class QuizCreatorModel
    {
        public QuizCreatorModel() { }

        public QuizCreatorModel(int id = -1)
        {
            QuizSourceId = id;
            if (id > 0)
            {
                // Get data from db
            }
        }

        public string Title { get; set; } = string.Empty;
        public List<QuestionModel> Questions { get; set; } = new();
        public int QuizSourceId { get; set; } // If not null, this quiz is a copy of another quiz and can be saved as or updated

        public struct QuestionModel
        {
            public string Text;
            public List<AnswerModel> Answers;
        }

        public struct AnswerModel
        {
            public string Text;
            public bool IsCorrect;
        }
    }
}