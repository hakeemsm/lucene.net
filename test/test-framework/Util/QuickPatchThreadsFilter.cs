/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Com.Carrotsearch.Randomizedtesting;
using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Util
{
	/// <summary>Last minute patches.</summary>
	/// <remarks>
	/// Last minute patches.
	/// TODO: remove when integrated in system filters in rr.
	/// </remarks>
	public class QuickPatchThreadsFilter : ThreadFilter
	{
		internal static readonly bool isJ9;

		static QuickPatchThreadsFilter()
		{
			isJ9 = Runtime.GetProperty("java.vm.info", "<?>").Contains("IBM J9");
		}

		public virtual bool Reject(Sharpen.Thread t)
		{
			if (isJ9)
			{
				StackTraceElement[] stack = t.GetStackTrace();
				if (stack.Length > 0 && stack[stack.Length - 1].GetClassName().Equals("java.util.Timer$TimerImpl"
					))
				{
					return true;
				}
			}
			// LUCENE-4736
			return false;
		}
	}
}
