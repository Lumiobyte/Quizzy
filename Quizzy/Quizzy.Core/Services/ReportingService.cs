using Quizzy.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.Services
{
    public class ReportingService : IReportingService
    {
        IReportingService? instance = null;
        public IReportingService Instance
        {
            get
            {
                if (instance == null) instance = new ReportingService();
                return instance;
            }
            set
            {
                instance = value;
            }
        }

        public void SendReportsToUser(QuizSession session, UserAccount user)
        {

        }

        public byte[] GenerateReportPDF(QuizSession session)
        {
            return new byte[0];
        }

        public void GenerateInGameReport(QuizSession session)
        {

        }
    }
}
