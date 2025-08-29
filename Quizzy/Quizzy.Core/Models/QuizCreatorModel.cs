using System.Text.Json;

namespace Quizzy.Models
{
    public class QuizCreatorModel
    {
        public QuizCreatorModel() { }

        public QuizCreatorModel(Guid? id)
        {
            QuizSourceId = id;
            if (id is not null)
            {
                // Get data from db
            }
        }

        public QuizCreatorModel(string json)
        {
            QuizSourceId = null;
            try
            {
                var model = JsonSerializer.Deserialize<QuizCreatorModel>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (model != null)
                {
                    Title = model.Title;
                    Questions = model.Questions;
                    QuizSourceId = model.QuizSourceId;
                }
            }
            catch
            {
                Title = string.Empty;
                Questions = new();
            }
        }

        public string Title { get; set; } = string.Empty;
        public List<QuestionModel> Questions { get; set; } = new();
        public Guid? QuizSourceId { get; set; } = null; // If not null, this quiz is a copy of another quiz and can be saved as or updated

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
}