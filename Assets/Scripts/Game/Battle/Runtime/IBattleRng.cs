namespace Game.Battle
{
    public interface IBattleRng
    {
        float NextFloat();

        float NextRange(float minInclusive, float maxInclusive);

        int NextInt(int minInclusive, int maxExclusive);
    }
}
