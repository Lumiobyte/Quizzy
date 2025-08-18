using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.Entities
{
    public class QuizQuestion
    {

        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;

        public int QuizId { get; set; }
        public Quiz Quiz { get; set; }

        public ICollection<QuizAnswer> Answers { get; set; } = new List<QuizAnswer>();

    }
}
