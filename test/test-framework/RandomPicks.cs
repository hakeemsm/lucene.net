using System;
using System.Collections.Generic;
using Lucene.Net.JavaCompatibility;

namespace Lucene.Net
{
    public static class RandomPicks
    {
        public static T RandomFrom<T>(this Random r, T[] array)
        {
            if (array.Length == 0)
                throw new ArgumentException("Can't pick a random object from an empty array.");
            return array[r.NextInt(array.Length)];
        }

        
        public static T RandomFrom<T>(this Random r, List<T> list)
        {
            if (list.size() == 0)
                throw new ArgumentException("Can't pick a random object from an empty list.");
            return list[r.NextInt(list.Count)];
        }

        
        public static T RandomFrom<T>(this Random r, ICollection<T> collection)
        {
            int size = collection.Count;
            if (size == 0)
                throw new ArgumentException("Can't pick a random object from an empty collection.");
            int pick = r.NextInt(size);
            T value = default(T);
            foreach (var v in collection)
            {
                pick--;
                value = v;
                if (pick == 0)
                {
                    break;
                }
            }

            return value;
        }
    }
}