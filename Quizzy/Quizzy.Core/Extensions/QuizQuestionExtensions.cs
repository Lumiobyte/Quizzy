using Quizzy.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quizzy.Core.Enums;

namespace Quizzy.Core.Extensions
{
    public static class QuizQuestionExtensions
    {

        public static bool Validate(this QuizQuestion question)
        {
            if (string.IsNullOrWhiteSpace(question.Text))
            {
                return false;
            }

            switch (question.QuestionType)
            {
                case QuestionType.MultipleChoice:
                    var count = question.Answers.Count();
                    return question.ValidateAnswers() && count >= 1 && count <= 6;

                case QuestionType.ShortAnswer:
                    return question.ValidateAnswers() &&  question.Answers.Count() == 1;

                default:
                    return false;
            }
        }

        public static bool ValidateAnswers(this QuizQuestion question)
        {
            foreach(var answer in question.Answers)
            {
                if (!answer.Validate(question.QuestionType)) return false;
            }

            return true;
        }

    }
}
