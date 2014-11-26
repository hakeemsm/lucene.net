using System;
using Lucene.Net.Support;

namespace Lucene.Net.Util
{
    public static class MathUtil
    {
        public static int Log(long x, int @base)
        {
            if (@base <= 1)
            {
                throw new ArgumentException("base must be > 1");
            }
            int ret = 0;
            while (x >= @base)
            {
                x /= @base;
                ret++;
            }
            return ret;
        }

		/// <remarks>Calculates logarithm in a given base with doubles.</remarks>
		public static double Log(double @base, double x)
		{
			return Math.Log(x) / Math.Log(@base);
		}
		/// <summary>
		/// Return the greatest common divisor of <code>a</code> and <code>b</code>,
		/// consistently with
		/// <see cref="Mono.Math.BigInteger.Gcd(Mono.Math.BigInteger)">Mono.Math.BigInteger.Gcd(Mono.Math.BigInteger)
		/// 	</see>
		/// .
		/// <p><b>NOTE</b>: A greatest common divisor must be positive, but
		/// <code>2^64</code> cannot be expressed as a long although it
		/// is the GCD of
		/// <see cref="long.MinValue">long.MinValue</see>
		/// and <code>0</code> and the GCD of
		/// <see cref="long.MinValue">long.MinValue</see>
		/// and
		/// <see cref="long.MinValue">long.MinValue</see>
		/// . So in these 2 cases,
		/// and only them, this method will return
		/// <see cref="long.MinValue">long.MinValue</see>
		/// .
		/// </summary>
		public static long Gcd(long a, long b)
		{
			// see http://en.wikipedia.org/wiki/Binary_GCD_algorithm#Iterative_version_in_C.2B.2B_using_ctz_.28count_trailing_zeros.29
			a = Math.Abs(a);
			b = Math.Abs(b);
			if (a == 0)
			{
				return b;
			}
			else
			{
				if (b == 0)
				{
					return a;
				}
			}
			int commonTrailingZeros = (a | b).NumberOfTrailingZeros();
			a = (long)(((ulong)a) >> a.NumberOfTrailingZeros());
			while (true)
			{
				b = (long)(((ulong)b) >> b.NumberOfTrailingZeros());
				if (a == b)
				{
					break;
				}
			    if (a > b || a == long.MinValue)
			    {
			        // MIN_VALUE is treated as 2^64
			        long tmp = a;
			        a = b;
			        b = tmp;
			    }
			    if (a == 1)
				{
					break;
				}
				b -= a;
			}
			return a << commonTrailingZeros;
		}
		/// <summary>
		/// Calculates inverse hyperbolic sine of a
		/// <code>double</code>
		/// value.
		/// <p>
		/// Special cases:
		/// <ul>
		/// <li>If the argument is NaN, then the result is NaN.
		/// <li/>If the argument is zero, then the result is a zero with the same sign as the argument.
		/// <li>If the argument is infinite, then the result is infinity with the same sign as the argument.</li></li>
		/// </ul></p>
		/// </summary>
		public static double Asinh(double a)
		{
			double sign;
			// check the sign bit of the raw representation to handle -0
			if (a.ToRawLongBits() < 0)
			{
				a = Math.Abs(a);
				sign = -1.0d;
			}
			else
			{
				sign = 1.0d;
			}
			return sign * Math.Log(Math.Sqrt(a * a + 1.0d) + a);
		}
		/// <summary>
		/// Calculates inverse hyperbolic cosine of a
		/// <code>double</code>
		/// value.
		/// <p>
		/// Special cases:
		/// <ul>
		/// <li>If the argument is NaN, then the result is NaN.
		/// <li>If the argument is +1, then the result is a zero.
		/// <li>If the argument is positive infinity, then the result is positive infinity.
		/// <li>If the argument is less than 1, then the result is NaN.
		/// </ul>
		/// </summary>
		public static double Acosh(double a)
		{
			return Math.Log(Math.Sqrt(a * a - 1.0d) + a);
		}
		/// <summary>
		/// Calculates inverse hyperbolic tangent of a
		/// <code>double</code>
		/// value.
		/// <p>
		/// Special cases:
		/// <ul>
		/// <li>If the argument is NaN, then the result is NaN.
		/// <li>If the argument is zero, then the result is a zero with the same sign as the argument.
		/// <li>If the argument is +1, then the result is positive infinity.
		/// <li>If the argument is -1, then the result is negative infinity.
		/// <li>If the argument's absolute value is greater than 1, then the result is NaN.
		/// </ul>
		/// </summary>
		public static double Atanh(double a)
		{
			double mult;
			// check the sign bit of the raw representation to handle -0
			if (a.ToRawLongBits() < 0)
			{
				a = Math.Abs(a);
				mult = -0.5d;
			}
			else
			{
				mult = 0.5d;
			}
			return mult * Math.Log((1.0d + a) / (1.0d - a));
		}

        public static double ToRadians(double angle)
        {
            return (Math.PI/180)*angle;
        }
    }
}
