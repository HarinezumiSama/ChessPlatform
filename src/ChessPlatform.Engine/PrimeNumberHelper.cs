﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessPlatform.Engine
{
    internal static class PrimeNumberHelper
    {
        private const int BasicPrimeUpperBound = 10000;
        private const int MinUpperBound = 2;

        private static readonly int[] BasicOddPrimeNumbers = ComputeBasicOddPrimeNumbers();

        public static int FindPrimeNotGreaterThanSpecified(int upperBound)
        {
            if (upperBound < MinUpperBound)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(upperBound),
                    upperBound,
                    $@"The value must be at least {MinUpperBound}.");
            }

            if (upperBound <= MinUpperBound)
            {
                return MinUpperBound;
            }

            var current = upperBound;
            if ((current & 1) == 0)
            {
                current--;
            }

            var foundIndex = Array.BinarySearch(BasicOddPrimeNumbers, current);
            if (foundIndex >= 0)
            {
                return current;
            }

            var knownLast = BasicOddPrimeNumbers.Last();
            while (current > knownLast)
            {
                var limit = (int)Math.Sqrt(current);

                var isComposite = false;
                for (int index = 0, tester = BasicOddPrimeNumbers[index];
                    tester <= limit;
                    index++, tester = index < BasicOddPrimeNumbers.Length ? BasicOddPrimeNumbers[index] : tester + 2)
                {
                    //// ReSharper disable once InvertIf
                    if (current % tester == 0)
                    {
                        isComposite = true;
                        break;
                    }
                }

                if (!isComposite)
                {
                    return current;
                }

                current -= 2;
            }

            return knownLast;
        }

        private static int[] ComputeBasicOddPrimeNumbers()
        {
            var oddPrimes = new List<int> { 3 };
            var current = oddPrimes.Last() + 2;
            while (current < BasicPrimeUpperBound)
            {
                var limit = (int)Math.Sqrt(current);

                var isComposite = false;
                for (int index = 0, tester = oddPrimes[index];
                    index < oddPrimes.Count && tester <= limit;
                    index++, tester = oddPrimes[index])
                {
                    //// ReSharper disable once InvertIf
                    if (current % tester == 0)
                    {
                        isComposite = true;
                        break;
                    }
                }

                if (!isComposite)
                {
                    oddPrimes.Add(current);
                }

                current += 2;
            }

            return oddPrimes.ToArray();
        }
    }
}