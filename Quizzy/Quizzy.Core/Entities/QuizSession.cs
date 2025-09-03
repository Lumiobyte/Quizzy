using Quizzy.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Quizzy.Core.Entities
{
    public class QuizSession
    {

        public Guid Id { get; set; }

        public string GamePin { get; set; }

        public QuizState State { get; set; } = QuizState.Lobby;

        public string QuestionOrder { get; set; } = "";

        [NotMapped]
        public List<int> QuestionOrderList => QuestionOrder.Split(',').Select(int.Parse).ToList();

        public Guid QuizHostId { get; set; }
        public UserAccount QuizHost { get; set; }

        public Guid QuizId { get; set; }
        public Quiz Quiz { get; set; }

        public ICollection<QuizPlayer> Players { get; set; } = new List<QuizPlayer>();

        public ScoringStrategyType ScoringStrategy { get; set; }
        public bool ScoringComplete { get; set; } = false;

    }
}
