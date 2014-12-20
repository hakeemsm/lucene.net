/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Sharpen;

namespace Lucene.Net.TestFramework.Util
{
	/// <summary>time unit constants for use in annotations.</summary>
	/// <remarks>time unit constants for use in annotations.</remarks>
	public sealed class TimeUnits
	{
		public TimeUnits()
		{
		}

		/// <summary>1 second in milliseconds</summary>
		public const int SECOND = 1000;

		/// <summary>1 minute in milliseconds</summary>
		public const int MINUTE = 60 * SECOND;

		/// <summary>1 hour in milliseconds</summary>
		public const int HOUR = 60 * MINUTE;
	}
}
