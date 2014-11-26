using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Support;

namespace Lucene.Net.Store
{
	public sealed class RateLimitedDirectoryWrapper : FilterDirectory
    {

        private IDictionary<IOContext.Context, RateLimiter> contextRateLimiters = new ConcurrentHashMap<IOContext.Context, RateLimiter>();

        public RateLimitedDirectoryWrapper(Directory wrapped) : base(wrapped)
        {
        }

		// we need to be volatile here to make sure we see all the values that are set
		// / modified concurrently
		/// <exception cref="System.IO.IOException"></exception>
        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            EnsureOpen();
			IndexOutput output = base.CreateOutput(name, context);
            RateLimiter limiter = GetRateLimiter(context.context);
            if (limiter != null)
            {
                return new RateLimitedIndexOutput(limiter, output);
            }
            return output;
        }

		/// <exception cref="System.IO.IOException"></exception>

        public override Directory.IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            EnsureOpen();
            return del.CreateSlicer(name, context);
        }

		/// <exception cref="System.IO.IOException"></exception>
        public override void Copy(Directory to, string src, string dest, IOContext context)
        {
            EnsureOpen();
            del.Copy(to, src, dest, context);
        }

        private RateLimiter GetRateLimiter(IOContext.Context context)
        {
            //assert context != null;
            return contextRateLimiters[context];
        }

        public void SetMaxWriteMBPerSec(double mbPerSec, IOContext.Context context)
        {
            EnsureOpen();
            if (context == null)
            {
                throw new ArgumentException("Context must not be null");
            }
            RateLimiter limiter = contextRateLimiters[context];
            if (mbPerSec == null)
            {
                if (limiter != null)
                {
                    limiter.MbPerSec = double.MaxValue;
                    contextRateLimiters[context] = null;
                }
            }
            else if (limiter != null)
            {
                limiter.MbPerSec = mbPerSec;
                contextRateLimiters[context] = limiter; // cross the mem barrier again
            }
            else
            {
                contextRateLimiters[context] = new RateLimiter.SimpleRateLimiter(mbPerSec);
            }
        }

        public void SetRateLimiter(RateLimiter mergeWriteRateLimiter, IOContext.Context context)
        {
            EnsureOpen();
			{
				throw new ArgumentException("Context must not be null");
			}
            contextRateLimiters[context] = mergeWriteRateLimiter;
        }

        public double? GetMaxWriteMBPerSec(IOContext.Context context)
        {
            EnsureOpen();
			if (context == null)
			{
				throw new ArgumentException("Context must not be null");
			}
            RateLimiter limiter = GetRateLimiter(context);
            return limiter == null ? (double?)null : limiter.MbPerSec;
        }
    }
}
