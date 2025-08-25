using Quizzy.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.Services
{
    public interface IQuizCreationService // Mostly acting as a stub
    {
        public static IQuizCreationService Instance { get; set; }

        public void GenerateQuiz();
        public void UpdateQuiz();
        public void DeleteQuiz();
    }
}
