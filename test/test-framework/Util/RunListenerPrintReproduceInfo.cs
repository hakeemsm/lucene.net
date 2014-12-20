/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Text;
using Com.Carrotsearch.Randomizedtesting;
using NUnit.Framework.Runner;
using NUnit.Framework.Runner.Notification;
using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Util
{
	/// <summary>A suite listener printing a "reproduce string".</summary>
	/// <remarks>
	/// A suite listener printing a "reproduce string". This ensures test result
	/// events are always captured properly even if exceptions happen at
	/// initialization or suite/ hooks level.
	/// </remarks>
	public sealed class RunListenerPrintReproduceInfo : RunListener
	{
		/// <summary>
		/// A list of all test suite classes executed so far in this JVM (ehm,
		/// under this class's classloader).
		/// </summary>
		/// <remarks>
		/// A list of all test suite classes executed so far in this JVM (ehm,
		/// under this class's classloader).
		/// </remarks>
		private static IList<string> testClassesRun = new AList<string>();

		/// <summary>The currently executing scope.</summary>
		/// <remarks>The currently executing scope.</remarks>
		private LifecycleScope scope;

		/// <summary>Current test failed.</summary>
		/// <remarks>Current test failed.</remarks>
		private bool testFailed;

		/// <summary>Suite-level code (initialization, rule, hook) failed.</summary>
		/// <remarks>Suite-level code (initialization, rule, hook) failed.</remarks>
		private bool suiteFailed;

		/// <summary>A marker to print full env.</summary>
		/// <remarks>A marker to print full env. diagnostics after the suite.</remarks>
		private bool printDiagnosticsAfterClass;

		/// <exception cref="System.Exception"></exception>
		public override void TestRunStarted(Description description)
		{
			suiteFailed = false;
			testFailed = false;
			scope = LifecycleScope.SUITE;
			Type targetClass = RandomizedContext.Current().GetTargetClass();
			testClassesRun.AddItem(targetClass.Name);
		}

		/// <exception cref="System.Exception"></exception>
		public override void TestStarted(Description description)
		{
			this.testFailed = false;
			this.scope = LifecycleScope.TEST;
		}

		/// <exception cref="System.Exception"></exception>
		public override void TestFailure(Failure failure)
		{
			if (scope == LifecycleScope.TEST)
			{
				testFailed = true;
			}
			else
			{
				suiteFailed = true;
			}
			printDiagnosticsAfterClass = true;
		}

		/// <exception cref="System.Exception"></exception>
		public override void TestFinished(Description description)
		{
			if (testFailed)
			{
				ReportAdditionalFailureInfo(StripTestNameAugmentations(description.GetMethodName(
					)));
			}
			scope = LifecycleScope.SUITE;
			testFailed = false;
		}

		/// <summary>
		/// The
		/// <see cref="NUnit.Framework.Runner.Description">NUnit.Framework.Runner.Description
		/// 	</see>
		/// object in JUnit does not expose the actual test method,
		/// instead it has the concept of a unique "name" of a test. To run the same method (tests)
		/// repeatedly, randomizedtesting must make those "names" unique: it appends the current iteration
		/// and seeds to the test method's name. We strip this information here.
		/// </summary>
		private string StripTestNameAugmentations(string methodName)
		{
			if (methodName != null)
			{
				methodName = methodName.ReplaceAll("\\s*\\{.+?\\}", string.Empty);
			}
			return methodName;
		}

		/// <exception cref="System.Exception"></exception>
		public override void TestRunFinished(Result result)
		{
			if (printDiagnosticsAfterClass || LuceneTestCase.VERBOSE)
			{
				RunListenerPrintReproduceInfo.PrintDebuggingInformation();
			}
			if (suiteFailed)
			{
				ReportAdditionalFailureInfo(null);
			}
		}

		/// <summary>print some useful debugging information about the environment</summary>
		private static void PrintDebuggingInformation()
		{
			if (classEnvRule != null)
			{
				System.Console.Error.WriteLine("NOTE: test params are: codec=" + classEnvRule.codec
					 + ", sim=" + classEnvRule.similarity + ", locale=" + classEnvRule.locale + ", timezone="
					 + (classEnvRule.timeZone == null ? "(null)" : classEnvRule.timeZone.GetID()));
			}
			System.Console.Error.WriteLine("NOTE: " + Runtime.GetProperty("os.name") + " " + 
				Runtime.GetProperty("os.version") + " " + Runtime.GetProperty("os.arch") + "/" +
				 Runtime.GetProperty("java.vendor") + " " + Runtime.GetProperty("java.version") 
				+ " " + (Constants.JRE_IS_64BIT ? "(64-bit)" : "(32-bit)") + "/" + "cpus=" + Runtime
				.GetRuntime().AvailableProcessors() + "," + "threads=" + Sharpen.Thread.ActiveCount
				() + "," + "free=" + Runtime.GetRuntime().FreeMemory() + "," + "total=" + Runtime
				.GetRuntime().TotalMemory());
			System.Console.Error.WriteLine("NOTE: All tests run in this JVM: " + Arrays.ToString
				(Sharpen.Collections.ToArray(testClassesRun)));
		}

		private void ReportAdditionalFailureInfo(string testName)
		{
			if (TEST_LINE_DOCS_FILE.EndsWith(JENKINS_LARGE_LINE_DOCS_FILE))
			{
				System.Console.Error.WriteLine("NOTE: download the large Jenkins line-docs file by running "
					 + "'ant get-jenkins-line-docs' in the lucene directory.");
			}
			StringBuilder b = new StringBuilder();
			b.Append("NOTE: reproduce with: ant test ");
			// Test case, method, seed.
			AddVmOpt(b, "testcase", RandomizedContext.Current().GetTargetClass().Name);
			AddVmOpt(b, "tests.method", testName);
			AddVmOpt(b, "tests.seed", RandomizedContext.Current().GetRunnerSeedAsString());
			// Test groups and multipliers.
			if (RANDOM_MULTIPLIER > 1)
			{
				AddVmOpt(b, "tests.multiplier", RANDOM_MULTIPLIER);
			}
			if (TEST_NIGHTLY)
			{
				AddVmOpt(b, SYSPROP_NIGHTLY, TEST_NIGHTLY);
			}
			if (TEST_WEEKLY)
			{
				AddVmOpt(b, SYSPROP_WEEKLY, TEST_WEEKLY);
			}
			if (TEST_SLOW)
			{
				AddVmOpt(b, SYSPROP_SLOW, TEST_SLOW);
			}
			if (TEST_AWAITSFIX)
			{
				AddVmOpt(b, SYSPROP_AWAITSFIX, TEST_AWAITSFIX);
			}
			// Codec, postings, directories.
			if (!TEST_CODEC.Equals("random"))
			{
				AddVmOpt(b, "tests.codec", TEST_CODEC);
			}
			if (!TEST_POSTINGSFORMAT.Equals("random"))
			{
				AddVmOpt(b, "tests.postingsformat", TEST_POSTINGSFORMAT);
			}
			if (!TEST_DOCVALUESFORMAT.Equals("random"))
			{
				AddVmOpt(b, "tests.docvaluesformat", TEST_DOCVALUESFORMAT);
			}
			if (!TEST_DIRECTORY.Equals("random"))
			{
				AddVmOpt(b, "tests.directory", TEST_DIRECTORY);
			}
			// Environment.
			if (!TEST_LINE_DOCS_FILE.Equals(DEFAULT_LINE_DOCS_FILE))
			{
				AddVmOpt(b, "tests.linedocsfile", TEST_LINE_DOCS_FILE);
			}
			if (classEnvRule != null)
			{
				AddVmOpt(b, "tests.locale", classEnvRule.locale);
				if (classEnvRule.timeZone != null)
				{
					AddVmOpt(b, "tests.timezone", classEnvRule.timeZone.GetID());
				}
			}
			AddVmOpt(b, "tests.file.encoding", Runtime.GetProperty("file.encoding"));
			System.Console.Error.WriteLine(b.ToString());
		}

		/// <summary>
		/// Append a VM option (-Dkey=value) to a
		/// <see cref="System.Text.StringBuilder">System.Text.StringBuilder</see>
		/// . Add quotes if
		/// spaces or other funky characters are detected.
		/// </summary>
		internal static void AddVmOpt(StringBuilder b, string key, object value)
		{
			if (value == null)
			{
				return;
			}
			b.Append(" -D").Append(key).Append("=");
			string v = value.ToString();
			// Add simplistic quoting. This varies a lot from system to system and between
			// shells... ANT should have some code for doing it properly.
			if (Sharpen.Pattern.Compile("[\\s=']").Matcher(v).Find())
			{
				v = '"' + v + '"';
			}
			b.Append(v);
		}
	}
}
