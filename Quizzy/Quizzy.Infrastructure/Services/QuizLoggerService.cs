using Quizzy.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Infrastructure.Services
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

        public void RetrieveLogs(QuizSession session)
        {

        }

        public void ClearLogsForSession(QuizSession session)
        {

        }
    }
}
