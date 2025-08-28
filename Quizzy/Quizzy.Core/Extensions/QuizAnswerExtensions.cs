using Quizzy.Core.Entities;
using Quizzy.Core.Enums;

namespace Quizzy.Core.Extensions
{
    public static class QuizAnswerExtensions
    {

        public static bool Validate(this QuizAnswer answer, QuestionType questionType)
        {
            switch (questionType)
            {
                case QuestionType.MultipleChoice:
                    return !string.IsNullOrWhiteSpace(answer.Text) && answer.Text.Length < 300;

                case QuestionType.ShortAnswer:
                    return !string.IsNullOrWhiteSpace(answer.Text) && answer.Text.Length < 100;

                default:
                    return false;
            }
        }

    }
}
