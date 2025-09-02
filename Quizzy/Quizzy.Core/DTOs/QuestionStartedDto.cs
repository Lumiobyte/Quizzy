using Quizzy.Core.Entities;
using Quizzy.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.DTOs
{
    public sealed class QuestionStartedDto
    {
        public string Question { get; set; }

        public List<string> Options { get; set; }

        public QuestionType QuestionType { get; set; }

        public int DurationSeconds { get; set; }

        public DateTimeOffset StartTimeOffset { get; set; }
    }
}
