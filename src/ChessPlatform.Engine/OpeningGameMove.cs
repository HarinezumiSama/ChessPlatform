using Omnifactotum.Annotations;

namespace ChessPlatform.Engine
{
    public sealed class OpeningGameMove
    {
        public OpeningGameMove([NotNull] GameMove move, int weight, long learn)
        {
            Move = move;
            Weight = weight;
            Learn = learn;
        }

        [NotNull]
        public GameMove Move
        {
            get;
        }

        public int Weight
        {
            get;
        }

        public long Learn
        {
            get;
        }
    }
}