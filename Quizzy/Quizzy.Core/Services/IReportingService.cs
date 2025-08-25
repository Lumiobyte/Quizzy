using Quizzy.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.Services
{
    public interface IReportingService
    {
        public static IReportingService Instance { get; set; }

        public void SendReportsToUser(QuizSession session, UserAccount user);
        public byte[] GenerateReportPDF(QuizSession session); // For download directly on frontend rather than email - Optional
        public void GenerateInGameReport(QuizSession session);
    }
}
