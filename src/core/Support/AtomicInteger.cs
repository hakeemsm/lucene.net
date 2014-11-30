using System.Threading;

namespace Lucene.Net.Support
{
    public class AtomicInteger
    {
        private long _value;

        public AtomicInteger(int value)
        {
            _value = value;
        }


        //Keeping Get & Set separate to match with Java
        public int Get()
        {
            return (int)Interlocked.Read(ref _value);
        }

        public void Set(int value)
        {
            Interlocked.Exchange(ref _value, value);
        }

        public int IncrementAndGet()
        {
            return (int) Interlocked.Increment(ref _value);
        }

        public int DecrementAndGet()
        {
            return (int)Interlocked.Decrement(ref _value);
        }
    }
}