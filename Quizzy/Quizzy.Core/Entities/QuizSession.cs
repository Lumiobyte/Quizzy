using Quizzy.Core.Enums;

namespace Quizzy.Core.Entities
{
    public class QuizSession
    {

        public Guid Id { get; set; }

        public string GamePin { get; set; }

        public QuizState State { get; set; } = QuizState.Lobby;

        public Guid QuizHostId { get; set; }
        public UserAccount QuizHost { get; set; }

        public Guid QuizId { get; set; }
        public Quiz Quiz { get; set; }

        public ICollection<QuizPlayer> Players { get; set; } = new List<QuizPlayer>();

    }
}
