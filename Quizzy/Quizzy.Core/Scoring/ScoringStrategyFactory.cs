using Microsoft.Extensions.DependencyInjection;
using Quizzy.Core.Enums;

namespace Quizzy.Core.Scoring
{
    public class ScoringStrategyFactory : IScoringStrategyFactory
    {

        readonly IServiceProvider _serviceProvider;

        public ScoringStrategyFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IScoringStrategy GetStrategy(ScoringStrategyType strategy)
        {
            Type strategyType = strategy switch
            {
                ScoringStrategyType.Speed => typeof(SpeedScoringStrategy),
                ScoringStrategyType.Ranking => typeof(RankingScoringStrategy),
                ScoringStrategyType.Fixed => typeof(FixedScoringStrategy),
                ScoringStrategyType.Streak => typeof(StreakScoringStrategy),
                _ => throw new NotImplementedException()
            };

            return (IScoringStrategy) ActivatorUtilities.CreateInstance(_serviceProvider, strategyType);
        }

    }
}
