using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessPlatform
{
    public static class PositionExtensions
    {
        #region Public Methods

        public static Position[] GetValidPositions(this Position position, IList<byte> x88Offsets)
        {
            #region Argument Check

            if (x88Offsets == null)
            {
                throw new ArgumentNullException("x88Offsets");
            }

            #endregion

            var result = new List<Position>(x88Offsets.Count);

            // ReSharper disable once LoopCanBeConvertedToQuery
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var index = 0; index < x88Offsets.Count; index++)
            {
                var x88Offset = x88Offsets[index];
                if (x88Offset == 0)
                {
                    continue;
                }

                var x88Value = (byte)(position.X88Value + x88Offset);
                if (Position.IsValidX88Value(x88Value))
                {
                    result.Add(new Position(x88Value));
                }
            }

            return result.ToArray();
        }

        #endregion
    }
}