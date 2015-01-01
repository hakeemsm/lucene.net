using System.Threading;

namespace Lucene.Net.Support
{
    public class AtomicLong
    {
        private long _value;

        public AtomicLong(long value)
        {
            _value = value;
        }

        public AtomicLong()
        {
            
        }

        public long Get()
        {
             return Interlocked.Read(ref _value); 
        }

        public void Set(long value)
        {
            Interlocked.Exchange(ref _value,value); 
        }

        public long AddAndGet(long value)
        {
            return Interlocked.Add(ref _value, value);
        }

        public long DecrementAndGet()
        {
            return Interlocked.Decrement(ref _value);
        }
    }
}