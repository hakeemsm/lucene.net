/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Com.Carrotsearch.Randomizedtesting.Generators;
using Mono.Math;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Util
{
	public class TestMathUtil : LuceneTestCase
	{
		internal static long[] PRIMES = new long[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29 };

		internal static long RandomLong()
		{
			if (Random().NextBoolean())
			{
				long l = 1;
				if (Random().NextBoolean())
				{
					l *= -1;
				}
				foreach (long i in PRIMES)
				{
					int m = Random().Next(3);
					for (int j = 0; j < m; ++j)
					{
						l *= i;
					}
				}
				return l;
			}
			else
			{
				if (Random().NextBoolean())
				{
					return Random().NextLong();
				}
				else
				{
					return RandomPicks.RandomFrom(Random(), Arrays.AsList(long.MinValue, long.MaxValue
						, 0L, -1L, 1L));
				}
			}
		}

		// slow version used for testing
		internal static long Gcd(long l1, long l2)
		{
			BigInteger gcd = BigInteger.ValueOf(l1).Gcd(BigInteger.ValueOf(l2));
			//HM:revisit 
			//assert gcd.bitCount() <= 64;
			return gcd;
		}

		public virtual void TestGCD()
		{
			int iters = AtLeast(100);
			for (int i = 0; i < iters; ++i)
			{
				long l1 = RandomLong();
				long l2 = RandomLong();
				long gcd = MathUtil.Gcd(l1, l2);
				long actualGcd = Gcd(l1, l2);
				AreEqual(actualGcd, gcd);
				if (gcd != 0)
				{
					AreEqual(l1, (l1 / gcd) * gcd);
					AreEqual(l2, (l2 / gcd) * gcd);
				}
			}
		}

		// ported test from commons-math
		public virtual void TestGCD2()
		{
			long a = 30;
			long b = 50;
			long c = 77;
			AreEqual(0, MathUtil.Gcd(0, 0));
			AreEqual(b, MathUtil.Gcd(0, b));
			AreEqual(a, MathUtil.Gcd(a, 0));
			AreEqual(b, MathUtil.Gcd(0, -b));
			AreEqual(a, MathUtil.Gcd(-a, 0));
			AreEqual(10, MathUtil.Gcd(a, b));
			AreEqual(10, MathUtil.Gcd(-a, b));
			AreEqual(10, MathUtil.Gcd(a, -b));
			AreEqual(10, MathUtil.Gcd(-a, -b));
			AreEqual(1, MathUtil.Gcd(a, c));
			AreEqual(1, MathUtil.Gcd(-a, c));
			AreEqual(1, MathUtil.Gcd(a, -c));
			AreEqual(1, MathUtil.Gcd(-a, -c));
			AreEqual(3L * (1L << 45), MathUtil.Gcd(3L * (1L << 50), 9L
				 * (1L << 45)));
			AreEqual(1L << 45, MathUtil.Gcd(1L << 45, long.MinValue));
			AreEqual(long.MaxValue, MathUtil.Gcd(long.MaxValue, 0L));
			AreEqual(long.MaxValue, MathUtil.Gcd(-long.MaxValue, 0L));
			AreEqual(1, MathUtil.Gcd(60247241209L, 153092023L));
			AreEqual(long.MinValue, MathUtil.Gcd(long.MinValue, 0));
			AreEqual(long.MinValue, MathUtil.Gcd(0, long.MinValue));
			AreEqual(long.MinValue, MathUtil.Gcd(long.MinValue, long.MinValue
				));
		}

		public virtual void TestAcoshMethod()
		{
			// acosh(NaN) == NaN
			IsTrue(double.IsNaN(MathUtil.Acosh(double.NaN)));
			// acosh(1) == +0
			AreEqual(0, double.DoubleToLongBits(MathUtil.Acosh(1D)));
			// acosh(POSITIVE_INFINITY) == POSITIVE_INFINITY
			AreEqual(double.DoubleToLongBits(double.PositiveInfinity), 
				double.DoubleToLongBits(MathUtil.Acosh(double.PositiveInfinity)));
			// acosh(x) : x < 1 == NaN
			IsTrue(double.IsNaN(MathUtil.Acosh(0.9D)));
			// x < 1
			IsTrue(double.IsNaN(MathUtil.Acosh(0D)));
			// x == 0
			IsTrue(double.IsNaN(MathUtil.Acosh(-0D)));
			// x == -0
			IsTrue(double.IsNaN(MathUtil.Acosh(-0.9D)));
			// x < 0
			IsTrue(double.IsNaN(MathUtil.Acosh(-1D)));
			// x == -1
			IsTrue(double.IsNaN(MathUtil.Acosh(-10D)));
			// x < -1
			IsTrue(double.IsNaN(MathUtil.Acosh(double.NegativeInfinity
				)));
			// x == -Inf
			double epsilon = 0.000001;
			AreEqual(0, MathUtil.Acosh(1), epsilon);
			AreEqual(1.5667992369724109, MathUtil.Acosh(2.5), epsilon);
			AreEqual(14.719378760739708, MathUtil.Acosh(1234567.89), epsilon
				);
		}

		public virtual void TestAsinhMethod()
		{
			// asinh(NaN) == NaN
			IsTrue(double.IsNaN(MathUtil.Asinh(double.NaN)));
			// asinh(+0) == +0
			AreEqual(0, double.DoubleToLongBits(MathUtil.Asinh(0D)));
			// asinh(-0) == -0
			AreEqual(double.DoubleToLongBits(-0D), double.DoubleToLongBits
				(MathUtil.Asinh(-0D)));
			// asinh(POSITIVE_INFINITY) == POSITIVE_INFINITY
			AreEqual(double.DoubleToLongBits(double.PositiveInfinity), 
				double.DoubleToLongBits(MathUtil.Asinh(double.PositiveInfinity)));
			// asinh(NEGATIVE_INFINITY) == NEGATIVE_INFINITY
			AreEqual(double.DoubleToLongBits(double.NegativeInfinity), 
				double.DoubleToLongBits(MathUtil.Asinh(double.NegativeInfinity)));
			double epsilon = 0.000001;
			AreEqual(-14.719378760740035, MathUtil.Asinh(-1234567.89), 
				epsilon);
			AreEqual(-1.6472311463710958, MathUtil.Asinh(-2.5), epsilon
				);
			AreEqual(-0.8813735870195429, MathUtil.Asinh(-1), epsilon);
			AreEqual(0, MathUtil.Asinh(0), 0);
			AreEqual(0.8813735870195429, MathUtil.Asinh(1), epsilon);
			AreEqual(1.6472311463710958, MathUtil.Asinh(2.5), epsilon);
			AreEqual(14.719378760740035, MathUtil.Asinh(1234567.89), epsilon
				);
		}

		public virtual void TestAtanhMethod()
		{
			// atanh(NaN) == NaN
			IsTrue(double.IsNaN(MathUtil.Atanh(double.NaN)));
			// atanh(+0) == +0
			AreEqual(0, double.DoubleToLongBits(MathUtil.Atanh(0D)));
			// atanh(-0) == -0
			AreEqual(double.DoubleToLongBits(-0D), double.DoubleToLongBits
				(MathUtil.Atanh(-0D)));
			// atanh(1) == POSITIVE_INFINITY
			AreEqual(double.DoubleToLongBits(double.PositiveInfinity), 
				double.DoubleToLongBits(MathUtil.Atanh(1D)));
			// atanh(-1) == NEGATIVE_INFINITY
			AreEqual(double.DoubleToLongBits(double.NegativeInfinity), 
				double.DoubleToLongBits(MathUtil.Atanh(-1D)));
			// atanh(x) : Math.abs(x) > 1 == NaN
			IsTrue(double.IsNaN(MathUtil.Atanh(1.1D)));
			// x > 1
			IsTrue(double.IsNaN(MathUtil.Atanh(double.PositiveInfinity
				)));
			// x == Inf
			IsTrue(double.IsNaN(MathUtil.Atanh(-1.1D)));
			// x < -1
			IsTrue(double.IsNaN(MathUtil.Atanh(double.NegativeInfinity
				)));
			// x == -Inf
			double epsilon = 0.000001;
			AreEqual(double.NegativeInfinity, MathUtil.Atanh(-1), 0);
			AreEqual(-0.5493061443340549, MathUtil.Atanh(-0.5), epsilon
				);
			AreEqual(0, MathUtil.Atanh(0), 0);
			AreEqual(0.5493061443340549, MathUtil.Atanh(0.5), epsilon);
			AreEqual(double.PositiveInfinity, MathUtil.Atanh(1), 0);
		}
	}
}
