using System.Collections.Generic;

namespace Quizzy.Models
{
    public class QuizCreatorModel
    {
        public string SaveQuiz(bool createNew)
        {
            if (createNew)
            {
                int newId = 0;
                // int newId = QuizCreationService.Instance.GenerateQuiz();
                if (!QuizSourceId.HasValue) QuizSourceId = newId;
            }
            else
            {
                //QuizCreationService.Instance.UpdateQuiz(QuizSourceId);
            }
            return "Quiz saved to your account successfully";
        }

        public string Title { get; set; } = string.Empty;
        public List<QuestionModel> Questions { get; set; } = new();
        public int? QuizSourceId { get; set; } = null; // If not null, this quiz is a copy of another quiz and can be saved as or updated

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