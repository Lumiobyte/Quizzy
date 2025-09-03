using Quizzy.Core.Enums;

namespace Quizzy.Core.Scoring
{
    public interface IScoringStrategyFactory
    {
        IScoringStrategy GetStrategy(ScoringStrategyType type);
    }
}
