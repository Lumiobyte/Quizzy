using Quizzy.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.Extensions
{
    public static class QuizExtensions
    {
        public static bool Validate(this Quiz quiz)
        {

            if (quiz.Questions.Count is 0)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(quiz.Title))
            {
                return false;
            }

            foreach(var question in quiz.Questions)
            {
                if (!question.Validate())
                {
                    return false;
                }
            }

            return true;
        }
    }
}
