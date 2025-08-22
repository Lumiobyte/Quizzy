using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.Entities
{
    public class PlayerAnswer
    {

        public int Id { get; set; }

        public int PlayerId { get; set; }
        public QuizPlayer Player { get; set; }

        public int QuestionId { get; set; }
        public QuizQuestion Question { get; set; }

        public int AnswerId { get; set; }
        public QuizAnswer Answer { get; set; }

        public DateTime DateTime { get; set; }
        public int PointsValue { get; set; } = 0;

    }
}
