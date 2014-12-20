using System;
using System.Globalization;
using Lucene.Net.Index;
using Lucene.Net.TestFramework.Util;

namespace Lucene.Net.TestFramework.Index
{
	/// <summary>
	/// <p>
	/// Merge policy for testing, it is like an alcoholic.
	/// </summary>
	/// <remarks>
	/// <p>
	/// Merge policy for testing, it is like an alcoholic.
	/// It drinks (merges) at night, and randomly decides what to drink.
	/// During the daytime it sleeps.
	/// </p>
	/// <p>
	/// if tests pass with this, then they are likely to pass with any
	/// bizarro merge policy users might write.
	/// </p>
	/// <p>
	/// It is a fine bottle of champagne (Ordered by Martijn).
	/// </p>
	/// </remarks>
	public class AlcoholicMergePolicy : LogMergePolicy
	{
		private readonly Random random;

		private readonly Calendar calendar;

		public AlcoholicMergePolicy(TimeZoneInfo tz, Random random)
		{
			this.calendar = new GregorianCalendar(tz, CultureInfo.ROOT);
			calendar.SetTimeInMillis(TestUtil.NextLong(random, 0, long.MaxValue));
			this.random = random;
			maxMergeSize = TestUtil.NextInt(random, 1024 * 1024, int.MaxValue);
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected override long Size(SegmentCommitInfo info)
		{
			//@BlackMagic(level=Voodoo);
			int hourOfDay = calendar.Get(Calendar.HOUR_OF_DAY);
			if (hourOfDay < 6 || hourOfDay > 20 || random.Next(23) == 5)
			{
				// its 5 o'clock somewhere
				AlcoholicMergePolicy.Drink[] values = AlcoholicMergePolicy.Drink.Values();
				// pick a random drink during the day
				return values[random.Next(values.Length)].drunkFactor * info.SizeInBytes();
			}
			return info.SizeInBytes();
		}

		private enum Drink
		{
			Beer,
			Wine,
			Champagne,
			WhiteRussian,
			SingleMalt
		}
	}
}
