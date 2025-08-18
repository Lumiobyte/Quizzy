using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.Entities
{
    public class QuizAnswer
    {

        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;

        public int QuestionId { get; set; }
        public QuizQuestion Question { get; set; }

        public bool IsCorrect { get; set; }

    }
}
