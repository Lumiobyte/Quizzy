using Quizzy.Core.Enums;

namespace Quizzy.Core.Scoring
{
    public class ScoringStrategyFactory
    {

        public IScoringStrategy GetStrategy(ScoringStrategyType strategy)
        {
            return strategy switch
            {
                ScoringStrategyType.Speed => new SpeedScoringStrategy(),
                ScoringStrategyType.Ranking => new RankingScoringStrategy(),
                ScoringStrategyType.Fixed => new FixedScoringStrategy(),
                ScoringStrategyType.Streak => new StreakScoringStrategy(),
            };
        }

    }
}
