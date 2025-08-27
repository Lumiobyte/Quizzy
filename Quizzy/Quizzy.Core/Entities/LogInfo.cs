using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.Entities
{
    public class LogInfo
    {

        public Guid Id { get; set; }

        public Guid PlayerAnswerId { get; set; }
        public PlayerAnswer PlayerAnswer { get; set; }

        public int TimeTaken { get; set; }

        // May have more later, or may end up getting removed idk yet
    }
}
