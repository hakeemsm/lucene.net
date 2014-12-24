/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Util
{
	public class TestSloppyMath : LuceneTestCase
	{
		internal static double COS_DELTA = 1E-15;

		internal static double ASIN_DELTA = 1E-7;

		// accuracy for cos()
		// accuracy for asin()
		public virtual void TestCos()
		{
			IsTrue(double.IsNaN(SloppyMath.Cos(double.NaN)));
			IsTrue(double.IsNaN(SloppyMath.Cos(double.NegativeInfinity
				)));
			IsTrue(double.IsNaN(SloppyMath.Cos(double.PositiveInfinity
				)));
			AreEqual(StrictMath.Cos(1), SloppyMath.Cos(1), COS_DELTA);
			AreEqual(StrictMath.Cos(0), SloppyMath.Cos(0), COS_DELTA);
			AreEqual(StrictMath.Cos(Math.PI / 2), SloppyMath.Cos(Math.
				PI / 2), COS_DELTA);
			AreEqual(StrictMath.Cos(-Math.PI / 2), SloppyMath.Cos(-Math
				.PI / 2), COS_DELTA);
			AreEqual(StrictMath.Cos(Math.PI / 4), SloppyMath.Cos(Math.
				PI / 4), COS_DELTA);
			AreEqual(StrictMath.Cos(-Math.PI / 4), SloppyMath.Cos(-Math
				.PI / 4), COS_DELTA);
			AreEqual(StrictMath.Cos(Math.PI * 2 / 3), SloppyMath.Cos(Math
				.PI * 2 / 3), COS_DELTA);
			AreEqual(StrictMath.Cos(-Math.PI * 2 / 3), SloppyMath.Cos(
				-Math.PI * 2 / 3), COS_DELTA);
			AreEqual(StrictMath.Cos(Math.PI / 6), SloppyMath.Cos(Math.
				PI / 6), COS_DELTA);
			AreEqual(StrictMath.Cos(-Math.PI / 6), SloppyMath.Cos(-Math
				.PI / 6), COS_DELTA);
			// testing purely random longs is inefficent, as for stupid parameters we just 
			// pass thru to Math.cos() instead of doing some huperduper arg reduction
			for (int i = 0; i < 10000; i++)
			{
				double d = Random().NextDouble() * SloppyMath.SIN_COS_MAX_VALUE_FOR_INT_MODULO;
				if (Random().NextBoolean())
				{
					d = -d;
				}
				AreEqual(StrictMath.Cos(d), SloppyMath.Cos(d), COS_DELTA);
			}
		}

		public virtual void TestAsin()
		{
			IsTrue(double.IsNaN(SloppyMath.Asin(double.NaN)));
			IsTrue(double.IsNaN(SloppyMath.Asin(2)));
			IsTrue(double.IsNaN(SloppyMath.Asin(-2)));
			AreEqual(-Math.PI / 2, SloppyMath.Asin(-1), ASIN_DELTA);
			AreEqual(-Math.PI / 3, SloppyMath.Asin(-0.8660254), ASIN_DELTA
				);
			AreEqual(-Math.PI / 4, SloppyMath.Asin(-0.7071068), ASIN_DELTA
				);
			AreEqual(-Math.PI / 6, SloppyMath.Asin(-0.5), ASIN_DELTA);
			AreEqual(0, SloppyMath.Asin(0), ASIN_DELTA);
			AreEqual(Math.PI / 6, SloppyMath.Asin(0.5), ASIN_DELTA);
			AreEqual(Math.PI / 4, SloppyMath.Asin(0.7071068), ASIN_DELTA
				);
			AreEqual(Math.PI / 3, SloppyMath.Asin(0.8660254), ASIN_DELTA
				);
			AreEqual(Math.PI / 2, SloppyMath.Asin(1), ASIN_DELTA);
			// only values -1..1 are useful
			for (int i = 0; i < 10000; i++)
			{
				double d = Random().NextDouble();
				if (Random().NextBoolean())
				{
					d = -d;
				}
				AreEqual(StrictMath.Asin(d), SloppyMath.Asin(d), ASIN_DELTA
					);
				IsTrue(SloppyMath.Asin(d) >= -Math.PI / 2);
				IsTrue(SloppyMath.Asin(d) <= Math.PI / 2);
			}
		}

		public virtual void TestHaversin()
		{
			IsTrue(double.IsNaN(SloppyMath.Haversin(1, 1, 1, double.NaN
				)));
			IsTrue(double.IsNaN(SloppyMath.Haversin(1, 1, double.NaN, 
				1)));
			IsTrue(double.IsNaN(SloppyMath.Haversin(1, double.NaN, 1, 
				1)));
			IsTrue(double.IsNaN(SloppyMath.Haversin(double.NaN, 1, 1, 
				1)));
			AreEqual(0, SloppyMath.Haversin(0, 0, 0, 0), 0D);
			AreEqual(0, SloppyMath.Haversin(0, -180, 0, -180), 0D);
			AreEqual(0, SloppyMath.Haversin(0, -180, 0, 180), 0D);
			AreEqual(0, SloppyMath.Haversin(0, 180, 0, 180), 0D);
			AreEqual(0, SloppyMath.Haversin(90, 0, 90, 0), 0D);
			AreEqual(0, SloppyMath.Haversin(90, -180, 90, -180), 0D);
			AreEqual(0, SloppyMath.Haversin(90, -180, 90, 180), 0D);
			AreEqual(0, SloppyMath.Haversin(90, 180, 90, 180), 0D);
			// Test half a circle on the equator, using WGS84 earth radius
			double earthRadiusKMs = 6378.137;
			double halfCircle = earthRadiusKMs * Math.PI;
			AreEqual(halfCircle, SloppyMath.Haversin(0, 0, 0, 180), 0D
				);
			Random r = Random();
			double randomLat1 = 40.7143528 + (r.Next(10) - 5) * 360;
			double randomLon1 = -74.0059731 + (r.Next(10) - 5) * 360;
			double randomLat2 = 40.65 + (r.Next(10) - 5) * 360;
			double randomLon2 = -73.95 + (r.Next(10) - 5) * 360;
			AreEqual(8.572, SloppyMath.Haversin(randomLat1, randomLon1
				, randomLat2, randomLon2), 0.01D);
			// from solr and ES tests (with their respective epsilons)
			AreEqual(0, SloppyMath.Haversin(40.7143528, -74.0059731, 40.7143528
				, -74.0059731), 0D);
			AreEqual(5.286, SloppyMath.Haversin(40.7143528, -74.0059731
				, 40.759011, -73.9844722), 0.01D);
			AreEqual(0.4621, SloppyMath.Haversin(40.7143528, -74.0059731
				, 40.718266, -74.007819), 0.01D);
			AreEqual(1.055, SloppyMath.Haversin(40.7143528, -74.0059731
				, 40.7051157, -74.0088305), 0.01D);
			AreEqual(1.258, SloppyMath.Haversin(40.7143528, -74.0059731
				, 40.7247222, -74), 0.01D);
			AreEqual(2.029, SloppyMath.Haversin(40.7143528, -74.0059731
				, 40.731033, -73.9962255), 0.01D);
			AreEqual(8.572, SloppyMath.Haversin(40.7143528, -74.0059731
				, 40.65, -73.95), 0.01D);
		}
	}
}
