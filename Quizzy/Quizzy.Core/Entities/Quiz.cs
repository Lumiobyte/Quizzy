using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.Entities
{
    public class Quiz
    {

        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;

        // Navigation properties
        public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
        public ICollection<QuizSession> Sessions { get; set; } = new List<QuizSession>();

    }
}
