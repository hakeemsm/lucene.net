using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lucene.Net.Store
{
    public abstract class RateLimiter
    {
        public abstract double MbPerSec { get; set; }

        public abstract long Pause(long bytes);

		public abstract long GetMinPauseCheckBytes();
        public class SimpleRateLimiter : RateLimiter
        {
			private const int MIN_PAUSE_CHECK_MSEC = 5;
            private double mbPerSec;
			private long minPauseCheckBytes;
            private long lastNS;

            public SimpleRateLimiter(double mbPerSec)
            {
                this.MbPerSec = mbPerSec;
            }

			public override long GetMinPauseCheckBytes()
			{
				return minPauseCheckBytes;
			}
            public override double MbPerSec
            {
                get
                {
                    return this.mbPerSec;
                }
                set
                {
                    this.mbPerSec = value;
                    	minPauseCheckBytes = (long)((MIN_PAUSE_CHECK_MSEC / 1000.0) * mbPerSec * 1024 * 1024
					);
                }
            }

            public override long Pause(long bytes)
            {
                long startNS = DateTime.UtcNow.Ticks*100;
				double secondsToPause = (bytes / 1024.0 / 1024.0) / mbPerSec;
                Interlocked.Exchange(ref lastNS, Interlocked.Read(ref lastNS) + (long)(1000000000 * secondsToPause));
                long targetNS = Interlocked.Read(ref lastNS);

                if (startNS >= targetNS)
                {
                    Interlocked.Exchange(ref lastNS, startNS);
                    return 0;
                }

                // TODO: this is purely instantaneous rate; maybe we
                // should also offer decayed recent history one?
                Interlocked.Exchange(ref lastNS, targetNS);

                long curNS = startNS;
                
                // While loop because Thread.sleep doesn't always sleep
                // enough:
                while (true)
                {
                    long pauseNS = targetNS - curNS;
                    if (pauseNS > 0)
                    {
                        try
                        {
                            Thread.Sleep((int)(pauseNS / 1000000));
                        }
                        catch (ThreadInterruptedException)
                        {
                            throw;
                        }
                        curNS = DateTime.UtcNow.Ticks * 100;
                        continue;
                    }
                    break;
                }
                return curNS - startNS;
            }
        }
    }
}
