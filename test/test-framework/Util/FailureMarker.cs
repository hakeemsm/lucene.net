/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using NUnit.Framework.Runner.Notification;
using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Util
{
	/// <summary>
	/// A
	/// <see cref="NUnit.Framework.Runner.Notification.RunListener">NUnit.Framework.Runner.Notification.RunListener
	/// 	</see>
	/// that detects suite/ test failures. We need it because failures
	/// due to thread leaks happen outside of any rule contexts.
	/// </summary>
	public class FailureMarker : RunListener
	{
		internal static readonly AtomicInteger failures = new AtomicInteger();

		/// <exception cref="System.Exception"></exception>
		public override void TestFailure(Failure failure)
		{
			failures.IncrementAndGet();
		}

		public static bool HadFailures()
		{
			return failures.Get() > 0;
		}

		internal static int GetFailures()
		{
			return failures.Get();
		}

		public static void ResetFailures()
		{
			failures.Set(0);
		}
	}
}
