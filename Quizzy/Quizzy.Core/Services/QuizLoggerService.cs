using Quizzy.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.Services
{
    public class QuizLoggerService : IQuizLoggerService
    {
        IQuizLoggerService? instance = null;
        public IQuizLoggerService Instance { 
            get
            {
                if (instance == null) instance = new QuizLoggerService();
                return instance;
            } 
            set
            {
                instance = value;
            }
        }

        public void LogAnswer(QuizSession session, PlayerAnswer playerAnswer) 
        {
            LogInfo info = new LogInfo { Id = session.Id, PlayerAnswerId = playerAnswer.Id };
            info.TimeTaken = 0;
            // Save to db
        }

        public LogInfo[] RetrieveLogs(QuizSession session)
        {
            return new LogInfo[0];
        }

        public void ClearLogsForSession(QuizSession session)
        {

        }
    }
}
