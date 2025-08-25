using Quizzy.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.Services
{
    public interface IQuizLoggerService
    {
        IQuizLoggerService Instance { get; set; }

        public void LogAnswer(QuizSession session, PlayerAnswer playerAnswer);
        public LogInfo[] RetrieveLogs(QuizSession session);
        public void ClearLogsForSession(QuizSession session);
    }
}
