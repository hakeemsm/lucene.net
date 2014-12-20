using System;
using System.Collections.Generic;

namespace Lucene.Net.JavaCompatibility
{
    public static class RandomHelpers
    {
        public static int NextInt(this Random random, int maxValue)
        {
            return random.Next(maxValue);
        }

        public static string[] Values(this Enum bag)
        {
            return Enum.GetNames(bag.GetType());
        }
    }
}
