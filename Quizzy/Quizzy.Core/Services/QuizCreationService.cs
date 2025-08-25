using Quizzy.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.Services
{
    public class QuizCreationService : IQuizCreationService
    {
        IQuizCreationService? instance = null;
        public IQuizCreationService Instance
        {
            get
            {
                if (instance == null) instance = new QuizCreationService();
                return instance;
            }
            set
            {
                instance = value;
            }
        }

        public void GenerateQuiz()
        {

        }

        public void UpdateQuiz()
        {

        }

        public void DeleteQuiz()
        {

        }
    }
}
