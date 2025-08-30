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
        public Task SendReportsForSession(QuizSession session);
    }
}
