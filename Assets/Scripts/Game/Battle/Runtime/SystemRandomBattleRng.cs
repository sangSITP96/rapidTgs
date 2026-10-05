using System;

namespace Game.Battle
{
    public sealed class SystemRandomBattleRng : IBattleRng
    {
        private readonly Random _random;

        public SystemRandomBattleRng(int seed)
        {
            _random = new Random(seed);
        }

        public float NextFloat()
        {
            return (float)_random.NextDouble();
        }

        public float NextRange(float minInclusive, float maxInclusive)
        {
            if (maxInclusive <= minInclusive)
                return minInclusive;

            return minInclusive + (float)_random.NextDouble() * (maxInclusive - minInclusive);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                return minInclusive;

            return _random.Next(minInclusive, maxExclusive);
        }
    }
}
