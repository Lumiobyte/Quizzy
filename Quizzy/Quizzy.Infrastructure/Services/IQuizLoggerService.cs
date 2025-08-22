using Quizzy.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Infrastructure.Services
{
    public interface IQuizLoggerService
    {
        IQuizLoggerService Instance { get; set; }

        public void LogAnswer(QuizSession session, PlayerAnswer playerAnswer);
        public void RetrieveLogs(QuizSession session); // Will not be void in final product, is just a stub since im not sure how i want to handle this rn
        public void ClearLogsForSession(QuizSession session);
    }
}
