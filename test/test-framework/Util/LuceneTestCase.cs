

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Lucene.Net.TestFramework.Support;
using NUnit.Framework;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Util;
using Org.Apache.Lucene.Util.Automaton;

namespace Lucene.Net.Util.TestFramework
{
	/// <summary>Base class for all Lucene unit tests, Junit3 or Junit4 variant.</summary>
	/// <remarks>
	/// Base class for all Lucene unit tests, Junit3 or Junit4 variant.
	/// <h3>Class and instance setup.</h3>
	/// <p>
	/// The preferred way to specify class (suite-level) setup/cleanup is to use
	/// static methods annotated with
	/// <see cref="NUnit.Framework.BeforeClass">NUnit.Framework.BeforeClass</see>
	/// and
	/// <see cref="NUnit.Framework.AfterClass">NUnit.Framework.AfterClass</see>
	/// . Any
	/// code in these methods is executed within the test framework's control and
	/// ensure proper setup has been made. <b>Try not to use static initializers
	/// (including complex final field initializers).</b> Static initializers are
	/// executed before any setup rules are fired and may cause you (or somebody
	/// else) headaches.
	/// <p>
	/// For instance-level setup, use
	/// <see cref="NUnit.Framework.SetUp">NUnit.Framework.SetUp</see>
	/// and
	/// <see cref="NUnit.Framework.TearDown">NUnit.Framework.TearDown</see>
	/// annotated
	/// methods. If you override either
	/// <see cref="SetUp()">SetUp()</see>
	/// or
	/// <see cref="TearDown()">TearDown()</see>
	/// in
	/// your subclass, make sure you call <code>super.setUp()</code> and
	/// <code>super.tearDown()</code>. This is detected and enforced.
	/// <h3>Specifying test cases</h3>
	/// <p>
	/// Any test method with a <code>testXXX</code> prefix is considered a test case.
	/// Any test method annotated with
	/// <see cref="NUnit.Framework.Test">NUnit.Framework.Test</see>
	/// is considered a test case.
	/// <h3>Randomized execution and test facilities</h3>
	/// <p>
	/// <see cref="LuceneTestCase">LuceneTestCase</see>
	/// uses
	/// <see cref="Com.Carrotsearch.Randomizedtesting.RandomizedRunner">Com.Carrotsearch.Randomizedtesting.RandomizedRunner
	/// 	</see>
	/// to execute test cases.
	/// <see cref="Com.Carrotsearch.Randomizedtesting.RandomizedRunner">Com.Carrotsearch.Randomizedtesting.RandomizedRunner
	/// 	</see>
	/// has built-in support for tests randomization
	/// including access to a repeatable
	/// <see cref="Sharpen.Random">Sharpen.Random</see>
	/// instance. See
	/// <see cref="Random()">Random()</see>
	/// method. Any test using
	/// <see cref="Sharpen.Random">Sharpen.Random</see>
	/// acquired from
	/// <see cref="Random()">Random()</see>
	/// should be fully reproducible (assuming no race conditions
	/// between threads etc.). The initial seed for a test case is reported in many
	/// ways:
	/// <ul>
	/// <li>as part of any exception thrown from its body (inserted as a dummy stack
	/// trace entry),</li>
	/// <li>as part of the main thread executing the test case (if your test hangs,
	/// just dump the stack trace of all threads and you'll see the seed),</li>
	/// <li>the master seed can also be accessed manually by getting the current
	/// context (
	/// <see cref="Com.Carrotsearch.Randomizedtesting.RandomizedContext.Current()">Com.Carrotsearch.Randomizedtesting.RandomizedContext.Current()
	/// 	</see>
	/// ) and then calling
	/// <see cref="Com.Carrotsearch.Randomizedtesting.RandomizedContext.GetRunnerSeedAsString()
	/// 	">Com.Carrotsearch.Randomizedtesting.RandomizedContext.GetRunnerSeedAsString()</see>
	/// .</li>
	/// </ul>
	/// </remarks>
	public abstract class LuceneTestCase : Assert
	{
		public static readonly string SYSPROP_NIGHTLY = "tests.nightly";

		public static readonly string SYSPROP_WEEKLY = "tests.weekly";

		public static readonly string SYSPROP_AWAITSFIX = "tests.awaitsfix";

		public static readonly string SYSPROP_SLOW = "tests.slow";

		public static readonly string SYSPROP_BADAPPLES = "tests.badapples";

		/// <seealso cref="ignoreAfterMaxFailures">ignoreAfterMaxFailures</seealso>
		public static readonly string SYSPROP_MAXFAILURES = "tests.maxfailures";

		/// <seealso cref="ignoreAfterMaxFailures">ignoreAfterMaxFailures</seealso>
		public static readonly string SYSPROP_FAILFAST = "tests.failfast";

		/// <summary>Use this constant when creating Analyzers and any other version-dependent stuff.
		/// 	</summary>
		/// <remarks>
		/// Use this constant when creating Analyzers and any other version-dependent stuff.
		/// <p><b>NOTE:</b> Change this when development starts for new Lucene version:
		/// </remarks>
		public static readonly Version TEST_VERSION_CURRENT = Version.LUCENE_48;

		/// <summary>True if and only if tests are run in verbose mode.</summary>
		/// <remarks>
		/// True if and only if tests are run in verbose mode. If this flag is false
		/// tests are not expected to print any messages.
		/// </remarks>
		public static readonly bool VERBOSE = RandomizedTest.SystemPropertyAsBoolean("tests.verbose"
			, false);

		/// <summary>TODO: javadoc?</summary>
		public static readonly bool INFOSTREAM = RandomizedTest.SystemPropertyAsBoolean("tests.infostream"
			, VERBOSE);

		/// <summary>
		/// A random multiplier which you should use when writing random tests:
		/// multiply it by the number of iterations to scale your tests (for nightly builds).
		/// </summary>
		/// <remarks>
		/// A random multiplier which you should use when writing random tests:
		/// multiply it by the number of iterations to scale your tests (for nightly builds).
		/// </remarks>
		public static readonly int RANDOM_MULTIPLIER = RandomizedTest.SystemPropertyAsInt
			("tests.multiplier", 1);

		/// <summary>TODO: javadoc?</summary>
		public static readonly string DEFAULT_LINE_DOCS_FILE = "europarl.lines.txt.gz";

		/// <summary>TODO: javadoc?</summary>
		public static readonly string JENKINS_LARGE_LINE_DOCS_FILE = "enwiki.random.lines.txt";

		/// <summary>Gets the codec to run tests with.</summary>
		/// <remarks>Gets the codec to run tests with.</remarks>
		public static readonly string TEST_CODEC = Runtime.GetProperty("tests.codec", "random"
			);

		/// <summary>Gets the postingsFormat to run tests with.</summary>
		/// <remarks>Gets the postingsFormat to run tests with.</remarks>
		public static readonly string TEST_POSTINGSFORMAT = Runtime.GetProperty("tests.postingsformat"
			, "random");

		/// <summary>Gets the docValuesFormat to run tests with</summary>
		public static readonly string TEST_DOCVALUESFORMAT = Runtime.GetProperty("tests.docvaluesformat"
			, "random");

		/// <summary>Gets the directory to run tests with</summary>
		public static readonly string TEST_DIRECTORY = Runtime.GetProperty("tests.directory"
			, "random");

		/// <summary>the line file used by LineFileDocs</summary>
		public static readonly string TEST_LINE_DOCS_FILE = Runtime.GetProperty("tests.linedocsfile"
			, DEFAULT_LINE_DOCS_FILE);

		/// <summary>
		/// Whether or not
		/// <see cref="Nightly">Nightly</see>
		/// tests should run.
		/// </summary>
		public static readonly bool TEST_NIGHTLY = RandomizedTest.SystemPropertyAsBoolean
			(SYSPROP_NIGHTLY, false);

		/// <summary>
		/// Whether or not
		/// <see cref="Weekly">Weekly</see>
		/// tests should run.
		/// </summary>
		public static readonly bool TEST_WEEKLY = RandomizedTest.SystemPropertyAsBoolean(
			SYSPROP_WEEKLY, false);

		/// <summary>
		/// Whether or not
		/// <see cref="AwaitsFix">AwaitsFix</see>
		/// tests should run.
		/// </summary>
		public static readonly bool TEST_AWAITSFIX = RandomizedTest.SystemPropertyAsBoolean
			(SYSPROP_AWAITSFIX, false);

		/// <summary>
		/// Whether or not
		/// <see cref="Slow">Slow</see>
		/// tests should run.
		/// </summary>
		public static readonly bool TEST_SLOW = RandomizedTest.SystemPropertyAsBoolean(SYSPROP_SLOW
			, false);

		/// <summary>
		/// Throttling, see
		/// <see cref="Org.Apache.Lucene.Store.MockDirectoryWrapper.SetThrottling(Org.Apache.Lucene.Store.MockDirectoryWrapper.Throttling)
		/// 	">Org.Apache.Lucene.Store.MockDirectoryWrapper.SetThrottling(Org.Apache.Lucene.Store.MockDirectoryWrapper.Throttling)
		/// 	</see>
		/// .
		/// </summary>
		public static readonly MockDirectoryWrapper.Throttling TEST_THROTTLING = TEST_NIGHTLY
			 ? MockDirectoryWrapper.Throttling.SOMETIMES : MockDirectoryWrapper.Throttling.NEVER;

		/// <summary>Leave temporary files on disk, even on successful runs.</summary>
		/// <remarks>Leave temporary files on disk, even on successful runs.</remarks>
		public static readonly bool LEAVE_TEMPORARY;

		static LuceneTestCase()
		{
			ruleChain = RuleChain.OuterRule(testFailureMarker).Around(ignoreAfterMaxFailures)
				.Around(threadAndTestNameRule).Around(new SystemPropertiesInvariantRule(IGNORED_INVARIANT_PROPERTIES
				)).Around(new TestRuleSetupAndRestoreInstanceEnv()).Around(new TestRuleFieldCacheSanity
				()).Around(parentChainCallRule);
			// See LUCENE-3995 for rationale.
			// Wait long for leaked threads to complete before failure. zk needs this.
			//HM:revisit 
			//assert {
			// --------------------------------------------------------------------
			// Test groups, system properties and other annotations modifying tests
			// --------------------------------------------------------------------
			// -----------------------------------------------------------------
			// Truly immutable fields and constants, initialized once and valid 
			// for all suites ever since.
			// -----------------------------------------------------------------
			// :Post-Release-Update-Version.LUCENE_XY:
			bool defaultValue = false;
			foreach (string property in Arrays.AsList("tests.leaveTemporary", "tests.leavetemporary"
				, "tests.leavetmpdir", "solr.test.leavetmpdir"))
			{
				defaultValue |= RandomizedTest.SystemPropertyAsBoolean(property, false);
			}
			LEAVE_TEMPORARY = defaultValue;
		}

		/// <summary>These property keys will be ignored in verification of altered properties.
		/// 	</summary>
		/// <remarks>These property keys will be ignored in verification of altered properties.
		/// 	</remarks>
		/// <seealso cref="Com.Carrotsearch.Randomizedtesting.Rules.SystemPropertiesInvariantRule
		/// 	">Com.Carrotsearch.Randomizedtesting.Rules.SystemPropertiesInvariantRule</seealso>
		/// <seealso cref="ruleChain">ruleChain</seealso>
		/// <seealso cref="classRules">classRules</seealso>
		private static readonly string[] IGNORED_INVARIANT_PROPERTIES = new string[] { "user.timezone"
			, "java.rmi.server.randomIDs" };

		/// <summary>
		/// Filesystem-based
		/// <see cref="Org.Apache.Lucene.Store.Directory">Org.Apache.Lucene.Store.Directory</see>
		/// implementations.
		/// </summary>
		private static readonly IList<string> FS_DIRECTORIES = Arrays.AsList("SimpleFSDirectory"
			, "NIOFSDirectory", "MMapDirectory");

		/// <summary>
		/// All
		/// <see cref="Org.Apache.Lucene.Store.Directory">Org.Apache.Lucene.Store.Directory</see>
		/// implementations.
		/// </summary>
		private static readonly IList<string> CORE_DIRECTORIES;

		static LuceneTestCase()
		{
			ruleChain = RuleChain.OuterRule(testFailureMarker).Around(ignoreAfterMaxFailures)
				.Around(threadAndTestNameRule).Around(new SystemPropertiesInvariantRule(IGNORED_INVARIANT_PROPERTIES
				)).Around(new TestRuleSetupAndRestoreInstanceEnv()).Around(new TestRuleFieldCacheSanity
				()).Around(parentChainCallRule);
			CORE_DIRECTORIES = new AList<string>(FS_DIRECTORIES);
			CORE_DIRECTORIES.AddItem("RAMDirectory");
		}

		protected internal static readonly ICollection<string> doesntSupportOffsets = new 
			HashSet<string>(Arrays.AsList("Lucene3x", "MockFixedIntBlock", "MockVariableIntBlock"
			, "MockSep", "MockRandom"));

		/// <summary>
		/// When
		/// <code>true</code>
		/// , Codecs for old Lucene version will support writing
		/// indexes in that format. Defaults to
		/// <code>false</code>
		/// , can be disabled by
		/// specific tests on demand.
		/// </summary>
		/// <lucene.internal></lucene.internal>
		public static bool OLD_FORMAT_IMPERSONATION_IS_ACTIVE = false;

		/// <summary>Stores the currently class under test.</summary>
		/// <remarks>Stores the currently class under test.</remarks>
		private static readonly TestRuleStoreClassName classNameRule;

		/// <summary>Class environment setup rule.</summary>
		/// <remarks>Class environment setup rule.</remarks>
		internal static readonly TestRuleSetupAndRestoreClassEnv classEnvRule;

		/// <summary>Suite failure marker (any error in the test or suite scope).</summary>
		/// <remarks>Suite failure marker (any error in the test or suite scope).</remarks>
		public static TestRuleMarkFailure suiteFailureMarker;

		/// <summary>Ignore tests after hitting a designated number of initial failures.</summary>
		/// <remarks>
		/// Ignore tests after hitting a designated number of initial failures. This
		/// is truly a "static" global singleton since it needs to span the lifetime of all
		/// test classes running inside this JVM (it cannot be part of a class rule).
		/// <p>This poses some problems for the test framework's tests because these sometimes
		/// trigger intentional failures which add up to the global count. This field contains
		/// a (possibly) changing reference to
		/// <see cref="TestRuleIgnoreAfterMaxFailures">TestRuleIgnoreAfterMaxFailures</see>
		/// and we
		/// dispatch to its current value from the
		/// <see cref="classRules">classRules</see>
		/// chain using
		/// <see cref="TestRuleDelegate{T}">TestRuleDelegate&lt;T&gt;</see>
		/// .
		/// </remarks>
		private static readonly AtomicReference<TestRuleIgnoreAfterMaxFailures> ignoreAfterMaxFailuresDelegate;

		private static readonly TestRule ignoreAfterMaxFailures;

		static LuceneTestCase()
		{
			ruleChain = RuleChain.OuterRule(testFailureMarker).Around(ignoreAfterMaxFailures)
				.Around(threadAndTestNameRule).Around(new SystemPropertiesInvariantRule(IGNORED_INVARIANT_PROPERTIES
				)).Around(new TestRuleSetupAndRestoreInstanceEnv()).Around(new TestRuleFieldCacheSanity
				()).Around(parentChainCallRule);
			// -----------------------------------------------------------------
			// Fields initialized in class or instance rules.
			// -----------------------------------------------------------------
			// -----------------------------------------------------------------
			// Class level (suite) rules.
			// -----------------------------------------------------------------
			int maxFailures = RandomizedTest.SystemPropertyAsInt(SYSPROP_MAXFAILURES, int.MaxValue
				);
			bool failFast = RandomizedTest.SystemPropertyAsBoolean(SYSPROP_FAILFAST, false);
			if (failFast)
			{
				if (maxFailures == int.MaxValue)
				{
					maxFailures = 1;
				}
				else
				{
					Logger.GetLogger(typeof(LuceneTestCase).Name).Warning("Property '" + SYSPROP_MAXFAILURES
						 + "'=" + maxFailures + ", 'failfast' is" + " ignored.");
				}
			}
			ignoreAfterMaxFailuresDelegate = new AtomicReference<TestRuleIgnoreAfterMaxFailures
				>(new TestRuleIgnoreAfterMaxFailures(maxFailures));
			ignoreAfterMaxFailures = TestRuleDelegate.Of(ignoreAfterMaxFailuresDelegate);
		}

		/// <summary>
		/// Temporarily substitute the global
		/// <see cref="TestRuleIgnoreAfterMaxFailures">TestRuleIgnoreAfterMaxFailures</see>
		/// . See
		/// <see cref="ignoreAfterMaxFailuresDelegate">ignoreAfterMaxFailuresDelegate</see>
		/// for some explanation why this method
		/// is needed.
		/// </summary>
		public static TestRuleIgnoreAfterMaxFailures ReplaceMaxFailureRule(TestRuleIgnoreAfterMaxFailures
			 newValue)
		{
			return ignoreAfterMaxFailuresDelegate.GetAndSet(newValue);
		}

		/// <summary>Max 10mb of static data stored in a test suite class after the suite is complete.
		/// 	</summary>
		/// <remarks>
		/// Max 10mb of static data stored in a test suite class after the suite is complete.
		/// Prevents static data structures leaking and causing OOMs in subsequent tests.
		/// </remarks>
		private const long STATIC_LEAK_THRESHOLD = 10 * 1024 * 1024;

		/// <summary>By-name list of ignored types like loggers etc.</summary>
		/// <remarks>By-name list of ignored types like loggers etc.</remarks>
		private static readonly ICollection<string> STATIC_LEAK_IGNORED_TYPES = Sharpen.Collections
			.UnmodifiableSet(new HashSet<string>(Arrays.AsList("org.slf4j.Logger", "org.apache.solr.SolrLogFormatter"
			, typeof(EnumSet).FullName)));

		private sealed class _StaticFieldsInvariantRule_547 : StaticFieldsInvariantRule
		{
			public _StaticFieldsInvariantRule_547(long baseArg1, bool baseArg2) : base(baseArg1
				, baseArg2)
			{
			}

			protected override bool Accept(FieldInfo field)
			{
				// Don't count known classes that consume memory once.
				if (LuceneTestCase.STATIC_LEAK_IGNORED_TYPES.Contains(field.FieldType.FullName))
				{
					return false;
				}
				// Don't count references from ourselves, we're top-level.
				if (field.DeclaringType == typeof(LuceneTestCase))
				{
					return false;
				}
				return base.Accept(field);
			}
		}

		private sealed class _NoInstanceHooksOverridesRule_562 : NoInstanceHooksOverridesRule
		{
			public _NoInstanceHooksOverridesRule_562()
			{
			}

			protected override bool Verify(MethodInfo key)
			{
				string name = key.Name;
				return !(name.Equals("setUp") || name.Equals("tearDown"));
			}
		}

		/// <summary>This controls how suite-level rules are nested.</summary>
		/// <remarks>
		/// This controls how suite-level rules are nested. It is important that _all_ rules declared
		/// in
		/// <see cref="LuceneTestCase">LuceneTestCase</see>
		/// are executed in proper order if they depend on each
		/// other.
		/// </remarks>
		[ClassRule]
		public static TestRule classRules = RuleChain.OuterRule(new TestRuleIgnoreTestSuites
			()).Around(ignoreAfterMaxFailures).Around(suiteFailureMarker = new TestRuleMarkFailure
			()).Around(new TestRuleAssertionsRequired()).Around(new LuceneTestCase.TemporaryFilesCleanupRule
			()).Around(new _StaticFieldsInvariantRule_547(STATIC_LEAK_THRESHOLD, true)).Around
			(new NoClassHooksShadowingRule()).Around(new _NoInstanceHooksOverridesRule_562()
			).Around(new SystemPropertiesInvariantRule(IGNORED_INVARIANT_PROPERTIES)).Around
			(classNameRule = new TestRuleStoreClassName()).Around(classEnvRule = new TestRuleSetupAndRestoreClassEnv
			());

		/// <summary>
		/// Enforces
		/// <see cref="SetUp()">SetUp()</see>
		/// and
		/// <see cref="TearDown()">TearDown()</see>
		/// calls are chained.
		/// </summary>
		private TestRuleSetupTeardownChained parentChainCallRule = new TestRuleSetupTeardownChained
			();

		/// <summary>Save test thread and name.</summary>
		/// <remarks>Save test thread and name.</remarks>
		private TestRuleThreadAndTestName threadAndTestNameRule = new TestRuleThreadAndTestName
			();

		/// <summary>Taint suite result with individual test failures.</summary>
		/// <remarks>Taint suite result with individual test failures.</remarks>
		private TestRuleMarkFailure testFailureMarker = new TestRuleMarkFailure(suiteFailureMarker
			);

		/// <summary>This controls how individual test rules are nested.</summary>
		/// <remarks>
		/// This controls how individual test rules are nested. It is important that
		/// _all_ rules declared in
		/// <see cref="LuceneTestCase">LuceneTestCase</see>
		/// are executed in proper order
		/// if they depend on each other.
		/// </remarks>
		[Rule]
		public readonly TestRule ruleChain;

		// -----------------------------------------------------------------
		// Test level rules.
		// -----------------------------------------------------------------
		// -----------------------------------------------------------------
		// Suite and test case setup/ cleanup.
		// -----------------------------------------------------------------
		/// <summary>For subclasses to override.</summary>
		/// <remarks>
		/// For subclasses to override. Overrides must call
		/// <code>super.setUp()</code>
		/// .
		/// </remarks>
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.SetUp]
		public virtual void SetUp()
		{
			parentChainCallRule.setupCalled = true;
		}

		/// <summary>For subclasses to override.</summary>
		/// <remarks>
		/// For subclasses to override. Overrides must call
		/// <code>super.tearDown()</code>
		/// .
		/// </remarks>
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.TearDown]
		public virtual void TearDown()
		{
			parentChainCallRule.teardownCalled = true;
		}

		// -----------------------------------------------------------------
		// Test facilities and facades for subclasses. 
		// -----------------------------------------------------------------
		/// <summary>
		/// Access to the current
		/// <see cref="Com.Carrotsearch.Randomizedtesting.RandomizedContext">Com.Carrotsearch.Randomizedtesting.RandomizedContext
		/// 	</see>
		/// 's Random instance. It is safe to use
		/// this method from multiple threads, etc., but it should be called while within a runner's
		/// scope (so no static initializers). The returned
		/// <see cref="Sharpen.Random">Sharpen.Random</see>
		/// instance will be
		/// <b>different</b> when this method is called inside a
		/// <see cref="NUnit.Framework.BeforeClass">NUnit.Framework.BeforeClass</see>
		/// hook (static
		/// suite scope) and within
		/// <see cref="NUnit.Framework.SetUp">NUnit.Framework.SetUp</see>
		/// /
		/// <see cref="NUnit.Framework.TearDown">NUnit.Framework.TearDown</see>
		/// hooks or test methods.
		/// <p>The returned instance must not be shared with other threads or cross a single scope's
		/// boundary. For example, a
		/// <see cref="Sharpen.Random">Sharpen.Random</see>
		/// acquired within a test method shouldn't be reused
		/// for another test case.
		/// <p>There is an overhead connected with getting the
		/// <see cref="Sharpen.Random">Sharpen.Random</see>
		/// for a particular context
		/// and thread. It is better to cache the
		/// <see cref="Sharpen.Random">Sharpen.Random</see>
		/// locally if tight loops with multiple
		/// invocations are present or create a derivative local
		/// <see cref="Sharpen.Random">Sharpen.Random</see>
		/// for millions of calls
		/// like this:
		/// <pre>
		/// Random random = new Random(random().nextLong());
		/// // tight loop with many invocations.
		/// </pre>
		/// </summary>
		public static Sharpen.Random Random()
		{
			return RandomizedContext.Current().GetRandom();
		}

		/// <summary>
		/// Registers a
		/// <see cref="System.IDisposable">System.IDisposable</see>
		/// resource that should be closed after the test
		/// completes.
		/// </summary>
		/// <returns><code>resource</code> (for call chaining).</returns>
		public virtual T CloseAfterTest<T>(T resource) where T:IDisposable
		{
			return RandomizedContext.Current().CloseAtEnd(resource, LifecycleScope.TEST);
		}

		/// <summary>
		/// Registers a
		/// <see cref="System.IDisposable">System.IDisposable</see>
		/// resource that should be closed after the suite
		/// completes.
		/// </summary>
		/// <returns><code>resource</code> (for call chaining).</returns>
		public static T CloseAfterSuite<T>(T resource) where T:IDisposable
		{
			return RandomizedContext.Current().CloseAtEnd(resource, LifecycleScope.SUITE);
		}

		/// <summary>Return the current class being tested.</summary>
		/// <remarks>Return the current class being tested.</remarks>
		public static Type GetTestClass()
		{
			return classNameRule.GetTestClass();
		}

		/// <summary>Return the name of the currently executing test case.</summary>
		/// <remarks>Return the name of the currently executing test case.</remarks>
		public virtual string GetTestName()
		{
			return threadAndTestNameRule.testMethodName;
		}

		/// <summary>
		/// Some tests expect the directory to contain a single segment, and want to
		/// do tests on that segment's reader.
		/// </summary>
		/// <remarks>
		/// Some tests expect the directory to contain a single segment, and want to
		/// do tests on that segment's reader. This is an utility method to help them.
		/// </remarks>
		public static SegmentReader GetOnlySegmentReader(DirectoryReader reader)
		{
			IList<AtomicReaderContext> subReaders = reader.Leaves();
			if (subReaders.Count != 1)
			{
				throw new ArgumentException(reader + " has " + subReaders.Count + " segments instead of exactly one"
					);
			}
			AtomicReader r = ((AtomicReader)subReaders[0].Reader());
			NUnit.Framework.Assert.IsTrue(r is SegmentReader);
			return (SegmentReader)r;
		}

		/// <summary>
		/// Returns true if and only if the calling thread is the primary thread
		/// executing the test case.
		/// </summary>
		/// <remarks>
		/// Returns true if and only if the calling thread is the primary thread
		/// executing the test case.
		/// </remarks>
		protected internal virtual bool IsTestThread()
		{
			NUnit.Framework.Assert.IsNotNull("Test case thread not set?", threadAndTestNameRule
				.testCaseThread);
			return Sharpen.Thread.CurrentThread() == threadAndTestNameRule.testCaseThread;
		}

		/// <summary>
		/// Asserts that FieldCacheSanityChecker does not detect any
		/// problems with FieldCache.DEFAULT.
		/// </summary>
		/// <remarks>
		/// Asserts that FieldCacheSanityChecker does not detect any
		/// problems with FieldCache.DEFAULT.
		/// <p>
		/// If any problems are found, they are logged to System.err
		/// (allong with the msg) when the Assertion is thrown.
		/// </p>
		/// <p>
		/// This method is called by tearDown after every test method,
		/// however IndexReaders scoped inside test methods may be garbage
		/// collected prior to this method being called, causing errors to
		/// be overlooked. Tests are encouraged to keep their IndexReaders
		/// scoped at the class level, or to explicitly call this method
		/// directly in the same scope as the IndexReader.
		/// </p>
		/// </remarks>
		/// <seealso cref="FieldCacheSanityChecker">FieldCacheSanityChecker</seealso>
		protected internal static void AssertSaneFieldCaches(string msg)
		{
			FieldCache.CacheEntry[] entries = FieldCache.DEFAULT.GetCacheEntries();
			FieldCacheSanityChecker.Insanity[] insanity = null;
			try
			{
				try
				{
					insanity = FieldCacheSanityChecker.CheckSanity(entries);
				}
				catch (RuntimeException e)
				{
					DumpArray(msg + ": FieldCache", entries, System.Console.Error);
					throw;
				}
				NUnit.Framework.Assert.AreEqual(msg + ": Insane FieldCache usage(s) found", 0, insanity
					.Length);
				insanity = null;
			}
			finally
			{
				// report this in the event of any exception/failure
				// if no failure, then insanity will be null anyway
				if (null != insanity)
				{
					DumpArray(msg + ": Insane FieldCache usage(s)", insanity, System.Console.Error);
				}
			}
		}

		/// <summary>
		/// Returns a number of at least <code>i</code>
		/// <p>
		/// The actual number returned will be influenced by whether
		/// <see cref="TEST_NIGHTLY">TEST_NIGHTLY</see>
		/// is active and
		/// <see cref="RANDOM_MULTIPLIER">RANDOM_MULTIPLIER</see>
		/// , but also with some random fudge.
		/// </summary>
		public static int AtLeast(Sharpen.Random random, int i)
		{
			int min = (TEST_NIGHTLY ? 2 * i : i) * RANDOM_MULTIPLIER;
			int max = min + (min / 2);
			return TestUtil.NextInt(random, min, max);
		}

		public static int AtLeast(int i)
		{
			return AtLeast(Random(), i);
		}

		/// <summary>
		/// Returns true if something should happen rarely,
		/// <p>
		/// The actual number returned will be influenced by whether
		/// <see cref="TEST_NIGHTLY">TEST_NIGHTLY</see>
		/// is active and
		/// <see cref="RANDOM_MULTIPLIER">RANDOM_MULTIPLIER</see>
		/// .
		/// </summary>
		public static bool Rarely(Sharpen.Random random)
		{
			int p = TEST_NIGHTLY ? 10 : 1;
			p += (p * Math.Log(RANDOM_MULTIPLIER));
			int min = 100 - Math.Min(p, 50);
			// never more than 50
			return random.Next(100) >= min;
		}

		public static bool Rarely()
		{
			return Rarely(Random());
		}

		public static bool Usually(Sharpen.Random random)
		{
			return !Rarely(random);
		}

		public static bool Usually()
		{
			return Usually(Random());
		}

		public static void AssumeTrue(string msg, bool condition)
		{
			RandomizedTest.AssumeTrue(msg, condition);
		}

		public static void AssumeFalse(string msg, bool condition)
		{
			RandomizedTest.AssumeFalse(msg, condition);
		}

		public static void AssumeNoException(string msg, Exception e)
		{
			RandomizedTest.AssumeNoException(msg, e);
		}

		/// <summary>
		/// Return <code>args</code> as a
		/// <see cref="Sharpen.Set{E}">Sharpen.Set&lt;E&gt;</see>
		/// instance. The order of elements is not
		/// preserved in iterators.
		/// </summary>
		[SafeVarargs]
		public static ICollection<T> AsSet<T>(params T[] args)
		{
			return new HashSet<T>(Arrays.AsList(args));
		}

		/// <summary>Convenience method for logging an iterator.</summary>
		/// <remarks>Convenience method for logging an iterator.</remarks>
		/// <param name="label">String logged before/after the items in the iterator</param>
		/// <param name="iter">Each next() is toString()ed and logged on it's own line. If iter is null this is logged differnetly then an empty iterator.
		/// 	</param>
		/// <param name="stream">Stream to log messages to.</param>
		public static void DumpIterator<_T0>(string label, Iterator<_T0> iter, TextWriter
			 stream)
		{
			stream.WriteLine("*** BEGIN " + label + " ***");
			if (null == iter)
			{
				stream.WriteLine(" ... NULL ...");
			}
			else
			{
				while (iter.HasNext())
				{
					stream.WriteLine(iter.Next().ToString());
				}
			}
			stream.WriteLine("*** END " + label + " ***");
		}

		/// <summary>Convenience method for logging an array.</summary>
		/// <remarks>Convenience method for logging an array.  Wraps the array in an iterator and delegates
		/// 	</remarks>
		/// <seealso cref="DumpIterator(string, Sharpen.Iterator{E}, System.IO.TextWriter)">DumpIterator(string, Sharpen.Iterator&lt;E&gt;, System.IO.TextWriter)
		/// 	</seealso>
		public static void DumpArray(string label, object[] objs, TextWriter stream)
		{
			Iterator<object> iter = (null == objs) ? null : Arrays.AsList(objs).Iterator();
			DumpIterator(label, iter, stream);
		}

		/// <summary>create a new index writer config with random defaults</summary>
		public static IndexWriterConfig NewIndexWriterConfig(Version v, Analyzer a)
		{
			return NewIndexWriterConfig(Random(), v, a);
		}

		/// <summary>create a new index writer config with random defaults using the specified random
		/// 	</summary>
		public static IndexWriterConfig NewIndexWriterConfig(Sharpen.Random r, Version v, 
			Analyzer a)
		{
			IndexWriterConfig c = new IndexWriterConfig(v, a);
			c.SetSimilarity(classEnvRule.similarity);
			if (VERBOSE)
			{
				// Even though TestRuleSetupAndRestoreClassEnv calls
				// InfoStream.setDefault, we do it again here so that
				// the PrintStreamInfoStream.messageID increments so
				// that when there are separate instances of
				// IndexWriter created we see "IW 0", "IW 1", "IW 2",
				// ... instead of just always "IW 0":
				c.SetInfoStream(new TestRuleSetupAndRestoreClassEnv.ThreadNameFixingPrintStreamInfoStream
					(System.Console.Out));
			}
			if (r.NextBoolean())
			{
				c.SetMergeScheduler(new SerialMergeScheduler());
			}
			else
			{
				if (Rarely(r))
				{
					int maxThreadCount = TestUtil.NextInt(Random(), 1, 4);
					int maxMergeCount = TestUtil.NextInt(Random(), maxThreadCount, maxThreadCount + 4
						);
					ConcurrentMergeScheduler cms = new ConcurrentMergeScheduler();
					cms.SetMaxMergesAndThreads(maxMergeCount, maxThreadCount);
					c.SetMergeScheduler(cms);
				}
			}
			if (r.NextBoolean())
			{
				if (Rarely(r))
				{
					// crazy value
					c.SetMaxBufferedDocs(TestUtil.NextInt(r, 2, 15));
				}
				else
				{
					// reasonable value
					c.SetMaxBufferedDocs(TestUtil.NextInt(r, 16, 1000));
				}
			}
			if (r.NextBoolean())
			{
				if (Rarely(r))
				{
					// crazy value
					c.SetTermIndexInterval(r.NextBoolean() ? TestUtil.NextInt(r, 1, 31) : TestUtil.NextInt
						(r, 129, 1000));
				}
				else
				{
					// reasonable value
					c.SetTermIndexInterval(TestUtil.NextInt(r, 32, 128));
				}
			}
			if (r.NextBoolean())
			{
				int maxNumThreadStates = Rarely(r) ? TestUtil.NextInt(r, 5, 20) : TestUtil.NextInt
					(r, 1, 4);
				// crazy value
				// reasonable value
				c.SetMaxThreadStates(maxNumThreadStates);
			}
			c.SetMergePolicy(NewMergePolicy(r));
			if (Rarely(r))
			{
				c.SetMergedSegmentWarmer(new SimpleMergedSegmentWarmer(c.GetInfoStream()));
			}
			c.SetUseCompoundFile(r.NextBoolean());
			c.SetReaderPooling(r.NextBoolean());
			c.SetReaderTermsIndexDivisor(TestUtil.NextInt(r, 1, 4));
			c.SetCheckIntegrityAtMerge(r.NextBoolean());
			return c;
		}

		public static MergePolicy NewMergePolicy(Sharpen.Random r)
		{
			if (Rarely(r))
			{
				return new MockRandomMergePolicy(r);
			}
			else
			{
				if (r.NextBoolean())
				{
					return NewTieredMergePolicy(r);
				}
				else
				{
					if (r.Next(5) == 0)
					{
						return NewAlcoholicMergePolicy(r, classEnvRule.timeZone);
					}
				}
			}
			return NewLogMergePolicy(r);
		}

		public static MergePolicy NewMergePolicy()
		{
			return NewMergePolicy(Random());
		}

		public static LogMergePolicy NewLogMergePolicy()
		{
			return NewLogMergePolicy(Random());
		}

		public static TieredMergePolicy NewTieredMergePolicy()
		{
			return NewTieredMergePolicy(Random());
		}

		public static AlcoholicMergePolicy NewAlcoholicMergePolicy()
		{
			return NewAlcoholicMergePolicy(Random(), classEnvRule.timeZone);
		}

		public static AlcoholicMergePolicy NewAlcoholicMergePolicy(Sharpen.Random r, TimeZoneInfo
			 tz)
		{
			return new AlcoholicMergePolicy(tz, new Sharpen.Random(r.NextLong()));
		}

		public static LogMergePolicy NewLogMergePolicy(Sharpen.Random r)
		{
			LogMergePolicy logmp = r.NextBoolean() ? new LogDocMergePolicy() : new LogByteSizeMergePolicy
				();
			logmp.SetCalibrateSizeByDeletes(r.NextBoolean());
			if (Rarely(r))
			{
				logmp.SetMergeFactor(TestUtil.NextInt(r, 2, 9));
			}
			else
			{
				logmp.SetMergeFactor(TestUtil.NextInt(r, 10, 50));
			}
			ConfigureRandom(r, logmp);
			return logmp;
		}

		private static void ConfigureRandom(Sharpen.Random r, MergePolicy mergePolicy)
		{
			if (r.NextBoolean())
			{
				mergePolicy.SetNoCFSRatio(0.1 + r.NextDouble() * 0.8);
			}
			else
			{
				mergePolicy.SetNoCFSRatio(r.NextBoolean() ? 1.0 : 0.0);
			}
			if (Rarely())
			{
				mergePolicy.SetMaxCFSSegmentSizeMB(0.2 + r.NextDouble() * 2.0);
			}
			else
			{
				mergePolicy.SetMaxCFSSegmentSizeMB(double.PositiveInfinity);
			}
		}

		public static TieredMergePolicy NewTieredMergePolicy(Sharpen.Random r)
		{
			TieredMergePolicy tmp = new TieredMergePolicy();
			if (Rarely(r))
			{
				tmp.SetMaxMergeAtOnce(TestUtil.NextInt(r, 2, 9));
				tmp.SetMaxMergeAtOnceExplicit(TestUtil.NextInt(r, 2, 9));
			}
			else
			{
				tmp.SetMaxMergeAtOnce(TestUtil.NextInt(r, 10, 50));
				tmp.SetMaxMergeAtOnceExplicit(TestUtil.NextInt(r, 10, 50));
			}
			if (Rarely(r))
			{
				tmp.SetMaxMergedSegmentMB(0.2 + r.NextDouble() * 2.0);
			}
			else
			{
				tmp.SetMaxMergedSegmentMB(r.NextDouble() * 100);
			}
			tmp.SetFloorSegmentMB(0.2 + r.NextDouble() * 2.0);
			tmp.SetForceMergeDeletesPctAllowed(0.0 + r.NextDouble() * 30.0);
			if (Rarely(r))
			{
				tmp.SetSegmentsPerTier(TestUtil.NextInt(r, 2, 20));
			}
			else
			{
				tmp.SetSegmentsPerTier(TestUtil.NextInt(r, 10, 50));
			}
			ConfigureRandom(r, tmp);
			tmp.SetReclaimDeletesWeight(r.NextDouble() * 4);
			return tmp;
		}

		public static MergePolicy NewLogMergePolicy(bool useCFS)
		{
			MergePolicy logmp = NewLogMergePolicy();
			logmp.SetNoCFSRatio(useCFS ? 1.0 : 0.0);
			return logmp;
		}

		public static MergePolicy NewLogMergePolicy(bool useCFS, int mergeFactor)
		{
			LogMergePolicy logmp = NewLogMergePolicy();
			logmp.SetNoCFSRatio(useCFS ? 1.0 : 0.0);
			logmp.SetMergeFactor(mergeFactor);
			return logmp;
		}

		public static MergePolicy NewLogMergePolicy(int mergeFactor)
		{
			LogMergePolicy logmp = NewLogMergePolicy();
			logmp.SetMergeFactor(mergeFactor);
			return logmp;
		}

		// if you want it in LiveIndexWriterConfig: it must and will be tested here.
		public static void MaybeChangeLiveIndexWriterConfig(Sharpen.Random r, LiveIndexWriterConfig
			 c)
		{
			bool didChange = false;
			if (Rarely(r))
			{
				// change flush parameters:
				// this is complicated because the api requires you "invoke setters in a magical order!"
				// LUCENE-5661: workaround for race conditions in the API
				lock (c)
				{
					bool flushByRam = r.NextBoolean();
					if (flushByRam)
					{
						c.SetRAMBufferSizeMB(TestUtil.NextInt(r, 1, 10));
						c.SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH);
					}
					else
					{
						if (Rarely(r))
						{
							// crazy value
							c.SetMaxBufferedDocs(TestUtil.NextInt(r, 2, 15));
						}
						else
						{
							// reasonable value
							c.SetMaxBufferedDocs(TestUtil.NextInt(r, 16, 1000));
						}
						c.SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
					}
				}
				didChange = true;
			}
			if (Rarely(r))
			{
				// change buffered deletes parameters
				bool limitBufferedDeletes = r.NextBoolean();
				if (limitBufferedDeletes)
				{
					c.SetMaxBufferedDeleteTerms(TestUtil.NextInt(r, 1, 1000));
				}
				else
				{
					c.SetMaxBufferedDeleteTerms(IndexWriterConfig.DISABLE_AUTO_FLUSH);
				}
				didChange = true;
			}
			if (Rarely(r))
			{
				// change warmer parameters
				if (r.NextBoolean())
				{
					c.SetMergedSegmentWarmer(new SimpleMergedSegmentWarmer(c.GetInfoStream()));
				}
				else
				{
					c.SetMergedSegmentWarmer(null);
				}
				didChange = true;
			}
			if (Rarely(r))
			{
				// change CFS flush parameters
				c.SetUseCompoundFile(r.NextBoolean());
				didChange = true;
			}
			if (Rarely(r))
			{
				// change merge integrity check parameters
				c.SetCheckIntegrityAtMerge(r.NextBoolean());
				didChange = true;
			}
			if (Rarely(r))
			{
				// change CMS merge parameters
				MergeScheduler ms = c.GetMergeScheduler();
				if (ms is ConcurrentMergeScheduler)
				{
					int maxThreadCount = TestUtil.NextInt(r, 1, 4);
					int maxMergeCount = TestUtil.NextInt(r, maxThreadCount, maxThreadCount + 4);
					((ConcurrentMergeScheduler)ms).SetMaxMergesAndThreads(maxMergeCount, maxThreadCount
						);
				}
				didChange = true;
			}
			if (Rarely(r))
			{
				MergePolicy mp = c.GetMergePolicy();
				ConfigureRandom(r, mp);
				if (mp is LogMergePolicy)
				{
					LogMergePolicy logmp = (LogMergePolicy)mp;
					logmp.SetCalibrateSizeByDeletes(r.NextBoolean());
					if (Rarely(r))
					{
						logmp.SetMergeFactor(TestUtil.NextInt(r, 2, 9));
					}
					else
					{
						logmp.SetMergeFactor(TestUtil.NextInt(r, 10, 50));
					}
				}
				else
				{
					if (mp is TieredMergePolicy)
					{
						TieredMergePolicy tmp = (TieredMergePolicy)mp;
						if (Rarely(r))
						{
							tmp.SetMaxMergeAtOnce(TestUtil.NextInt(r, 2, 9));
							tmp.SetMaxMergeAtOnceExplicit(TestUtil.NextInt(r, 2, 9));
						}
						else
						{
							tmp.SetMaxMergeAtOnce(TestUtil.NextInt(r, 10, 50));
							tmp.SetMaxMergeAtOnceExplicit(TestUtil.NextInt(r, 10, 50));
						}
						if (Rarely(r))
						{
							tmp.SetMaxMergedSegmentMB(0.2 + r.NextDouble() * 2.0);
						}
						else
						{
							tmp.SetMaxMergedSegmentMB(r.NextDouble() * 100);
						}
						tmp.SetFloorSegmentMB(0.2 + r.NextDouble() * 2.0);
						tmp.SetForceMergeDeletesPctAllowed(0.0 + r.NextDouble() * 30.0);
						if (Rarely(r))
						{
							tmp.SetSegmentsPerTier(TestUtil.NextInt(r, 2, 20));
						}
						else
						{
							tmp.SetSegmentsPerTier(TestUtil.NextInt(r, 10, 50));
						}
						ConfigureRandom(r, tmp);
						tmp.SetReclaimDeletesWeight(r.NextDouble() * 4);
					}
				}
				didChange = true;
			}
			if (VERBOSE && didChange)
			{
				System.Console.Out.WriteLine("NOTE: LuceneTestCase: randomly changed IWC's live settings to:\n"
					 + c);
			}
		}

		/// <summary>Returns a new Directory instance.</summary>
		/// <remarks>
		/// Returns a new Directory instance. Use this when the test does not
		/// care about the specific Directory implementation (most tests).
		/// <p>
		/// The Directory is wrapped with
		/// <see cref="Org.Apache.Lucene.Store.BaseDirectoryWrapper">Org.Apache.Lucene.Store.BaseDirectoryWrapper
		/// 	</see>
		/// .
		/// this means usually it will be picky, such as ensuring that you
		/// properly close it and all open files in your test. It will emulate
		/// some features of Windows, such as not allowing open files to be
		/// overwritten.
		/// </remarks>
		public static BaseDirectoryWrapper NewDirectory()
		{
			return NewDirectory(Random());
		}

		/// <summary>Returns a new Directory instance, using the specified random.</summary>
		/// <remarks>
		/// Returns a new Directory instance, using the specified random.
		/// See
		/// <see cref="NewDirectory()">NewDirectory()</see>
		/// for more information.
		/// </remarks>
		public static BaseDirectoryWrapper NewDirectory(Sharpen.Random r)
		{
			return WrapDirectory(r, NewDirectoryImpl(r, TEST_DIRECTORY), Rarely(r));
		}

		public static MockDirectoryWrapper NewMockDirectory()
		{
			return NewMockDirectory(Random());
		}

		public static MockDirectoryWrapper NewMockDirectory(Sharpen.Random r)
		{
			return (MockDirectoryWrapper)WrapDirectory(r, NewDirectoryImpl(r, TEST_DIRECTORY)
				, false);
		}

		public static MockDirectoryWrapper NewMockFSDirectory(FilePath f)
		{
			return (MockDirectoryWrapper)NewFSDirectory(f, null, false);
		}

		/// <summary>
		/// Returns a new Directory instance, with contents copied from the
		/// provided directory.
		/// </summary>
		/// <remarks>
		/// Returns a new Directory instance, with contents copied from the
		/// provided directory. See
		/// <see cref="NewDirectory()">NewDirectory()</see>
		/// for more
		/// information.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public static BaseDirectoryWrapper NewDirectory(Directory d)
		{
			return NewDirectory(Random(), d);
		}

		/// <summary>Returns a new FSDirectory instance over the given file, which must be a folder.
		/// 	</summary>
		/// <remarks>Returns a new FSDirectory instance over the given file, which must be a folder.
		/// 	</remarks>
		public static BaseDirectoryWrapper NewFSDirectory(FilePath f)
		{
			return NewFSDirectory(f, null);
		}

		/// <summary>Returns a new FSDirectory instance over the given file, which must be a folder.
		/// 	</summary>
		/// <remarks>Returns a new FSDirectory instance over the given file, which must be a folder.
		/// 	</remarks>
		public static BaseDirectoryWrapper NewFSDirectory(FilePath f, LockFactory lf)
		{
			return NewFSDirectory(f, lf, Rarely());
		}

		private static BaseDirectoryWrapper NewFSDirectory(FilePath f, LockFactory lf, bool
			 bare)
		{
			string fsdirClass = TEST_DIRECTORY;
			if (fsdirClass.Equals("random"))
			{
				fsdirClass = RandomPicks.RandomFrom(Random(), FS_DIRECTORIES);
			}
			Type clazz;
			try
			{
				try
				{
					clazz = CommandLineUtil.LoadFSDirectoryClass(fsdirClass);
				}
				catch (InvalidCastException)
				{
					// TEST_DIRECTORY is not a sub-class of FSDirectory, so draw one at random
					fsdirClass = RandomPicks.RandomFrom(Random(), FS_DIRECTORIES);
					clazz = CommandLineUtil.LoadFSDirectoryClass(fsdirClass);
				}
				Directory fsdir = NewFSDirectoryImpl(clazz, f);
				BaseDirectoryWrapper wrapped = WrapDirectory(Random(), fsdir, bare);
				if (lf != null)
				{
					wrapped.SetLockFactory(lf);
				}
				return wrapped;
			}
			catch (Exception e)
			{
				Rethrow.Rethrow(e);
				throw null;
			}
		}

		// dummy to prevent compiler failure
		/// <summary>
		/// Returns a new Directory instance, using the specified random
		/// with contents copied from the provided directory.
		/// </summary>
		/// <remarks>
		/// Returns a new Directory instance, using the specified random
		/// with contents copied from the provided directory. See
		/// <see cref="NewDirectory()">NewDirectory()</see>
		/// for more information.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public static BaseDirectoryWrapper NewDirectory(Sharpen.Random r, Directory d)
		{
			Directory impl = NewDirectoryImpl(r, TEST_DIRECTORY);
			foreach (string file in d.ListAll())
			{
				d.Copy(impl, file, file, NewIOContext(r));
			}
			return WrapDirectory(r, impl, Rarely(r));
		}

		private static BaseDirectoryWrapper WrapDirectory(Sharpen.Random random, Directory
			 directory, bool bare)
		{
			if (Rarely(random))
			{
				directory = new NRTCachingDirectory(directory, random.NextDouble(), random.NextDouble
					());
			}
			if (Rarely(random))
			{
				double maxMBPerSec = 10 + 5 * (random.NextDouble() - 0.5);
				if (LuceneTestCase.VERBOSE)
				{
					System.Console.Out.WriteLine("LuceneTestCase: will rate limit output IndexOutput to "
						 + maxMBPerSec + " MB/sec");
				}
				RateLimitedDirectoryWrapper rateLimitedDirectoryWrapper = new RateLimitedDirectoryWrapper
					(directory);
				switch (random.Next(10))
				{
					case 3:
					{
						// sometimes rate limit on flush
						rateLimitedDirectoryWrapper.SetMaxWriteMBPerSec(maxMBPerSec, IOContext.Context.FLUSH
							);
						break;
					}

					case 2:
					{
						// sometimes rate limit flush & merge
						rateLimitedDirectoryWrapper.SetMaxWriteMBPerSec(maxMBPerSec, IOContext.Context.FLUSH
							);
						rateLimitedDirectoryWrapper.SetMaxWriteMBPerSec(maxMBPerSec, IOContext.Context.MERGE
							);
						break;
					}

					default:
					{
						rateLimitedDirectoryWrapper.SetMaxWriteMBPerSec(maxMBPerSec, IOContext.Context.MERGE
							);
						break;
					}
				}
				directory = rateLimitedDirectoryWrapper;
			}
			if (bare)
			{
				BaseDirectoryWrapper @base = new BaseDirectoryWrapper(directory);
				CloseAfterSuite(new CloseableDirectory(@base, suiteFailureMarker));
				return @base;
			}
			else
			{
				MockDirectoryWrapper mock = new MockDirectoryWrapper(random, directory);
				mock.SetThrottling(TEST_THROTTLING);
				CloseAfterSuite(new CloseableDirectory(mock, suiteFailureMarker));
				return mock;
			}
		}

		public static Field NewStringField(string name, string value, Field.Store stored)
		{
			return NewField(Random(), name, value, stored == Field.Store.YES ? StringField.TYPE_STORED
				 : StringField.TYPE_NOT_STORED);
		}

		public static Field NewTextField(string name, string value, Field.Store stored)
		{
			return NewField(Random(), name, value, stored == Field.Store.YES ? TextField.TYPE_STORED
				 : TextField.TYPE_NOT_STORED);
		}

		public static Field NewStringField(Sharpen.Random random, string name, string value
			, Field.Store stored)
		{
			return NewField(random, name, value, stored == Field.Store.YES ? StringField.TYPE_STORED
				 : StringField.TYPE_NOT_STORED);
		}

		public static Field NewTextField(Sharpen.Random random, string name, string value
			, Field.Store stored)
		{
			return NewField(random, name, value, stored == Field.Store.YES ? TextField.TYPE_STORED
				 : TextField.TYPE_NOT_STORED);
		}

		public static Field NewField(string name, string value, FieldType type)
		{
			return NewField(Random(), name, value, type);
		}

		public static Field NewField(Sharpen.Random random, string name, string value, FieldType
			 type)
		{
			name = new string(name);
			if (Usually(random) || !type.Indexed())
			{
				// most of the time, don't modify the params
				return new Field(name, value, type);
			}
			// TODO: once all core & test codecs can index
			// offsets, sometimes randomly turn on offsets if we are
			// already indexing positions...
			FieldType newType = new FieldType(type);
			if (!newType.Stored() && random.NextBoolean())
			{
				newType.SetStored(true);
			}
			// randomly store it
			if (!newType.StoreTermVectors() && random.NextBoolean())
			{
				newType.SetStoreTermVectors(true);
				if (!newType.StoreTermVectorOffsets())
				{
					newType.SetStoreTermVectorOffsets(random.NextBoolean());
				}
				if (!newType.StoreTermVectorPositions())
				{
					newType.SetStoreTermVectorPositions(random.NextBoolean());
					if (newType.StoreTermVectorPositions() && !newType.StoreTermVectorPayloads() && !
						OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
					{
						newType.SetStoreTermVectorPayloads(random.NextBoolean());
					}
				}
			}
			// TODO: we need to do this, but smarter, ie, most of
			// the time we set the same value for a given field but
			// sometimes (rarely) we change it up:
			return new Field(name, value, newType);
		}

		/// <summary>Return a random Locale from the available locales on the system.</summary>
		/// <remarks>Return a random Locale from the available locales on the system.</remarks>
		/// <seealso>"https://issues.apache.org/jira/browse/LUCENE-4020"</seealso>
		public static CultureInfo RandomLocale(Sharpen.Random random)
		{
			CultureInfo[] locales = CultureInfo.GetAvailableLocales();
			return locales[random.Next(locales.Length)];
		}

		/// <summary>Return a random TimeZone from the available timezones on the system</summary>
		/// <seealso>"https://issues.apache.org/jira/browse/LUCENE-4020"</seealso>
		public static TimeZoneInfo RandomTimeZone(Sharpen.Random random)
		{
			string[] tzIds = TimeZoneInfo.GetAvailableIDs();
			return Sharpen.Extensions.GetTimeZone(tzIds[random.Next(tzIds.Length)]);
		}

		/// <summary>return a Locale object equivalent to its programmatic name</summary>
		public static CultureInfo LocaleForName(string localeName)
		{
			string[] elements = localeName.Split("\\_");
			switch (elements.Length)
			{
				case 4:
				case 3:
				{
					return new CultureInfo(elements[0], elements[1], elements[2]);
				}

				case 2:
				{
					return new CultureInfo(elements[0], elements[1]);
				}

				case 1:
				{
					return new CultureInfo(elements[0]);
				}

				default:
				{
					throw new ArgumentException("Invalid Locale: " + localeName);
				}
			}
		}

		public static bool DefaultCodecSupportsDocValues()
		{
			return !Codec.GetDefault().GetName().Equals("Lucene3x");
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static Directory NewFSDirectoryImpl<_T0>(Type<_T0> clazz, FilePath file) where 
			_T0:FSDirectory
		{
			FSDirectory d = null;
			try
			{
				d = CommandLineUtil.NewFSDirectory(clazz, file);
			}
			catch (Exception e)
			{
				Rethrow.Rethrow(e);
			}
			return d;
		}

		internal static Directory NewDirectoryImpl(Sharpen.Random random, string clazzName
			)
		{
			if (clazzName.Equals("random"))
			{
				if (Rarely(random))
				{
					clazzName = RandomPicks.RandomFrom(random, CORE_DIRECTORIES);
				}
				else
				{
					clazzName = "RAMDirectory";
				}
			}
			try
			{
				Type clazz = CommandLineUtil.LoadDirectoryClass(clazzName);
				// If it is a FSDirectory type, try its ctor(File)
				if (typeof(FSDirectory).IsAssignableFrom(clazz))
				{
					FilePath dir = CreateTempDir("index-" + clazzName);
					dir.Mkdirs();
					// ensure it's created so we 'have' it.
					return NewFSDirectoryImpl(clazz.AsSubclass<FSDirectory>(), dir);
				}
				// try empty ctor
				return System.Activator.CreateInstance(clazz);
			}
			catch (Exception e)
			{
				Rethrow.Rethrow(e);
				throw null;
			}
		}

		// dummy to prevent compiler failure
		/// <summary>
		/// Sometimes wrap the IndexReader as slow, parallel or filter reader (or
		/// combinations of that)
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public static IndexReader MaybeWrapReader(IndexReader r)
		{
			Sharpen.Random random = Random();
			if (Rarely())
			{
				// TODO: remove this, and fix those tests to wrap before putting slow around:
				bool wasOriginallyAtomic = r is AtomicReader;
				for (int i = 0; i < c; i++)
				{
					switch (random.Next(5))
					{
						case 0:
						{
							r = SlowCompositeReaderWrapper.Wrap(r);
							break;
						}

						case 1:
						{
							// will create no FC insanity in atomic case, as ParallelAtomicReader has own cache key:
							r = (r is AtomicReader) ? new ParallelAtomicReader((AtomicReader)r) : new ParallelCompositeReader
								((CompositeReader)r);
							break;
						}

						case 2:
						{
							// HÃ¤ckidy-Hick-Hack: a standard MultiReader will cause FC insanity, so we use
							// QueryUtils' reader with a fake cache key, so insanity checker cannot walk
							// along our reader:
							r = new QueryUtils.FCInvisibleMultiReader(r);
							break;
						}

						case 3:
						{
							AtomicReader ar = SlowCompositeReaderWrapper.Wrap(r);
							IList<string> allFields = new AList<string>();
							foreach (FieldInfo fi in ar.GetFieldInfos())
							{
								allFields.AddItem(fi.name);
							}
							Sharpen.Collections.Shuffle(allFields, random);
							int end = allFields.IsEmpty() ? 0 : random.Next(allFields.Count);
							ICollection<string> fields = new HashSet<string>(allFields.SubList(0, end));
							// will create no FC insanity as ParallelAtomicReader has own cache key:
							r = new ParallelAtomicReader(new FieldFilterAtomicReader(ar, fields, false), new 
								FieldFilterAtomicReader(ar, fields, true));
							break;
						}

						case 4:
						{
							// HÃ¤ckidy-Hick-Hack: a standard Reader will cause FC insanity, so we use
							// QueryUtils' reader with a fake cache key, so insanity checker cannot walk
							// along our reader:
							if (r is AtomicReader)
							{
								r = new AssertingAtomicReader((AtomicReader)r);
							}
							else
							{
								if (r is DirectoryReader)
								{
									r = new AssertingDirectoryReader((DirectoryReader)r);
								}
							}
							break;
						}

						default:
						{
							NUnit.Framework.Assert.Fail("should not get here");
							break;
						}
					}
				}
				if (wasOriginallyAtomic)
				{
					r = SlowCompositeReaderWrapper.Wrap(r);
				}
				else
				{
					if ((r is CompositeReader) && !(r is QueryUtils.FCInvisibleMultiReader))
					{
						// prevent cache insanity caused by e.g. ParallelCompositeReader, to fix we wrap one more time:
						r = new QueryUtils.FCInvisibleMultiReader(r);
					}
				}
				if (VERBOSE)
				{
					System.Console.Out.WriteLine("maybeWrapReader wrapped: " + r);
				}
			}
			return r;
		}

		/// <summary>TODO: javadoc</summary>
		public static IOContext NewIOContext(Sharpen.Random random)
		{
			return NewIOContext(random, IOContext.DEFAULT);
		}

		/// <summary>TODO: javadoc</summary>
		public static IOContext NewIOContext(Sharpen.Random random, IOContext oldContext)
		{
			int randomNumDocs = random.Next(4192);
			int size = random.Next(512) * randomNumDocs;
			if (oldContext.flushInfo != null)
			{
				// Always return at least the estimatedSegmentSize of
				// the incoming IOContext:
				return new IOContext(new FlushInfo(randomNumDocs, Math.Max(oldContext.flushInfo.estimatedSegmentSize
					, size)));
			}
			else
			{
				if (oldContext.mergeInfo != null)
				{
					// Always return at least the estimatedMergeBytes of
					// the incoming IOContext:
					return new IOContext(new MergeInfo(randomNumDocs, Math.Max(oldContext.mergeInfo.estimatedMergeBytes
						, size), random.NextBoolean(), TestUtil.NextInt(random, 1, 100)));
				}
				else
				{
					// Make a totally random IOContext:
					IOContext context;
					switch (random.Next(5))
					{
						case 0:
						{
							context = IOContext.DEFAULT;
							break;
						}

						case 1:
						{
							context = IOContext.READ;
							break;
						}

						case 2:
						{
							context = IOContext.READONCE;
							break;
						}

						case 3:
						{
							context = new IOContext(new MergeInfo(randomNumDocs, size, true, -1));
							break;
						}

						case 4:
						{
							context = new IOContext(new FlushInfo(randomNumDocs, size));
							break;
						}

						default:
						{
							context = IOContext.DEFAULT;
							break;
						}
					}
					return context;
				}
			}
		}

		/// <summary>Create a new searcher over the reader.</summary>
		/// <remarks>
		/// Create a new searcher over the reader. This searcher might randomly use
		/// threads.
		/// </remarks>
		public static IndexSearcher NewSearcher(IndexReader r)
		{
			return NewSearcher(r, true);
		}

		/// <summary>Create a new searcher over the reader.</summary>
		/// <remarks>
		/// Create a new searcher over the reader. This searcher might randomly use
		/// threads.
		/// </remarks>
		public static IndexSearcher NewSearcher(IndexReader r, bool maybeWrap)
		{
			return NewSearcher(r, maybeWrap, true);
		}

		/// <summary>Create a new searcher over the reader.</summary>
		/// <remarks>
		/// Create a new searcher over the reader. This searcher might randomly use
		/// threads. if <code>maybeWrap</code> is true, this searcher might wrap the
		/// reader with one that returns null for getSequentialSubReaders. If
		/// <code>wrapWithAssertions</code> is true, this searcher might be an
		/// <see cref="Org.Apache.Lucene.Search.AssertingIndexSearcher">Org.Apache.Lucene.Search.AssertingIndexSearcher
		/// 	</see>
		/// instance.
		/// </remarks>
		public static IndexSearcher NewSearcher(IndexReader r, bool maybeWrap, bool wrapWithAssertions
			)
		{
			Sharpen.Random random = Random();
			if (Usually())
			{
				if (maybeWrap)
				{
					try
					{
						r = MaybeWrapReader(r);
					}
					catch (IOException e)
					{
						Rethrow.Rethrow(e);
					}
				}
				// TODO: this whole check is a coverage hack, we should move it to tests for various filterreaders.
				// ultimately whatever you do will be checkIndex'd at the end anyway. 
				if (random.Next(500) == 0 && r is AtomicReader)
				{
					// TODO: not useful to check DirectoryReader (redundant with checkindex)
					// but maybe sometimes run this on the other crazy readers maybeWrapReader creates?
					try
					{
						TestUtil.CheckReader(r);
					}
					catch (IOException e)
					{
						Rethrow.Rethrow(e);
					}
				}
				IndexSearcher ret;
				if (wrapWithAssertions)
				{
					ret = random.NextBoolean() ? new AssertingIndexSearcher(random, r) : new AssertingIndexSearcher
						(random, r.GetContext());
				}
				else
				{
					ret = random.NextBoolean() ? new IndexSearcher(r) : new IndexSearcher(r.GetContext
						());
				}
				ret.SetSimilarity(classEnvRule.similarity);
				return ret;
			}
			else
			{
				int threads = 0;
				ThreadPoolExecutor ex;
				if (random.NextBoolean())
				{
					ex = null;
				}
				else
				{
					threads = TestUtil.NextInt(random, 1, 8);
					ex = new ThreadPoolExecutor(threads, threads, 0L, TimeUnit.MILLISECONDS, new LinkedBlockingQueue
						<Runnable>(), new NamedThreadFactory("LuceneTestCase"));
				}
				// uncomment to intensify LUCENE-3840
				// ex.prestartAllCoreThreads();
				if (ex != null)
				{
					if (VERBOSE)
					{
						System.Console.Out.WriteLine("NOTE: newSearcher using ExecutorService with " + threads
							 + " threads");
					}
					r.AddReaderClosedListener(new _ReaderClosedListener_1569(ex));
				}
				IndexSearcher ret;
				if (wrapWithAssertions)
				{
					ret = random.NextBoolean() ? new AssertingIndexSearcher(random, r, ex) : new AssertingIndexSearcher
						(random, r.GetContext(), ex);
				}
				else
				{
					ret = random.NextBoolean() ? new IndexSearcher(r, ex) : new IndexSearcher(r.GetContext
						(), ex);
				}
				ret.SetSimilarity(classEnvRule.similarity);
				return ret;
			}
		}

		private sealed class _ReaderClosedListener_1569 : IndexReader.ReaderClosedListener
		{
			public _ReaderClosedListener_1569(ThreadPoolExecutor ex)
			{
				this.ex = ex;
			}

			public void OnClose(IndexReader reader)
			{
				TestUtil.ShutdownExecutorService(ex);
			}

			private readonly ThreadPoolExecutor ex;
		}

		/// <summary>
		/// Gets a resource from the classpath as
		/// <see cref="Sharpen.FilePath">Sharpen.FilePath</see>
		/// . This method should only
		/// be used, if a real file is needed. To get a stream, code should prefer
		/// <see cref="System.Type{T}.GetResourceAsStream(string)">System.Type&lt;T&gt;.GetResourceAsStream(string)
		/// 	</see>
		/// using
		/// <code>this.getClass()</code>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual FilePath GetDataFile(string name)
		{
			try
			{
				return new FilePath(this.GetType().GetResource(name).ToURI());
			}
			catch (Exception)
			{
				throw new IOException("Cannot find resource: " + name);
			}
		}

		/// <summary>Returns true if the default codec supports single valued docvalues with missing values
		/// 	</summary>
		public static bool DefaultCodecSupportsMissingDocValues()
		{
			string name = Codec.GetDefault().GetName();
			if (name.Equals("Lucene3x") || name.Equals("Lucene40") || name.Equals("Appending"
				) || name.Equals("Lucene41") || name.Equals("Lucene42"))
			{
				return false;
			}
			return true;
		}

		/// <summary>Returns true if the default codec supports SORTED_SET docvalues</summary>
		public static bool DefaultCodecSupportsSortedSet()
		{
			if (!DefaultCodecSupportsDocValues())
			{
				return false;
			}
			string name = Codec.GetDefault().GetName();
			if (name.Equals("Lucene40") || name.Equals("Lucene41") || name.Equals("Appending"
				))
			{
				return false;
			}
			return true;
		}

		/// <summary>
		/// Returns true if the codec "supports" docsWithField
		/// (other codecs return MatchAllBits, because you couldnt write missing values before)
		/// </summary>
		public static bool DefaultCodecSupportsDocsWithField()
		{
			if (!DefaultCodecSupportsDocValues())
			{
				return false;
			}
			string name = Codec.GetDefault().GetName();
			if (name.Equals("Appending") || name.Equals("Lucene40") || name.Equals("Lucene41"
				) || name.Equals("Lucene42"))
			{
				return false;
			}
			return true;
		}

		/// <summary>Returns true if the codec "supports" field updates.</summary>
		/// <remarks>Returns true if the codec "supports" field updates.</remarks>
		public static bool DefaultCodecSupportsFieldUpdates()
		{
			string name = Codec.GetDefault().GetName();
			if (name.Equals("Lucene3x") || name.Equals("Appending") || name.Equals("Lucene40"
				) || name.Equals("Lucene41") || name.Equals("Lucene42") || name.Equals("Lucene45"
				))
			{
				return false;
			}
			return true;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AssertReaderEquals(string info, IndexReader leftReader, IndexReader
			 rightReader)
		{
			AssertReaderStatisticsEquals(info, leftReader, rightReader);
			AssertFieldsEquals(info, leftReader, MultiFields.GetFields(leftReader), MultiFields
				.GetFields(rightReader), true);
			AssertNormsEquals(info, leftReader, rightReader);
			AssertStoredFieldsEquals(info, leftReader, rightReader);
			AssertTermVectorsEquals(info, leftReader, rightReader);
			AssertDocValuesEquals(info, leftReader, rightReader);
			AssertDeletedDocsEquals(info, leftReader, rightReader);
			AssertFieldInfosEquals(info, leftReader, rightReader);
		}

		/// <summary>checks that reader-level statistics are the same</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AssertReaderStatisticsEquals(string info, IndexReader leftReader
			, IndexReader rightReader)
		{
			// Somewhat redundant: we never delete docs
			NUnit.Framework.Assert.AreEqual(info, leftReader.MaxDoc(), rightReader.MaxDoc());
			NUnit.Framework.Assert.AreEqual(info, leftReader.NumDocs(), rightReader.NumDocs()
				);
			NUnit.Framework.Assert.AreEqual(info, leftReader.NumDeletedDocs(), rightReader.NumDeletedDocs
				());
			NUnit.Framework.Assert.AreEqual(info, leftReader.HasDeletions(), rightReader.HasDeletions
				());
		}

		/// <summary>Fields api equivalency</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AssertFieldsEquals(string info, IndexReader leftReader, Fields
			 leftFields, Fields rightFields, bool deep)
		{
			// Fields could be null if there are no postings,
			// but then it must be null for both
			if (leftFields == null || rightFields == null)
			{
				NUnit.Framework.Assert.IsNull(info, leftFields);
				NUnit.Framework.Assert.IsNull(info, rightFields);
				return;
			}
			AssertFieldStatisticsEquals(info, leftFields, rightFields);
			Iterator<string> leftEnum = leftFields.Iterator();
			Iterator<string> rightEnum = rightFields.Iterator();
			while (leftEnum.HasNext())
			{
				string field = leftEnum.Next();
				NUnit.Framework.Assert.AreEqual(info, field, rightEnum.Next());
				AssertTermsEquals(info, leftReader, leftFields.Terms(field), rightFields.Terms(field
					), deep);
			}
			NUnit.Framework.Assert.IsFalse(rightEnum.HasNext());
		}

		/// <summary>checks that top-level statistics on Fields are the same</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AssertFieldStatisticsEquals(string info, Fields leftFields, Fields
			 rightFields)
		{
			if (leftFields.Size() != -1 && rightFields.Size() != -1)
			{
				NUnit.Framework.Assert.AreEqual(info, leftFields.Size(), rightFields.Size());
			}
		}

		/// <summary>Terms api equivalency</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AssertTermsEquals(string info, IndexReader leftReader, Terms 
			leftTerms, Terms rightTerms, bool deep)
		{
			if (leftTerms == null || rightTerms == null)
			{
				NUnit.Framework.Assert.IsNull(info, leftTerms);
				NUnit.Framework.Assert.IsNull(info, rightTerms);
				return;
			}
			AssertTermsStatisticsEquals(info, leftTerms, rightTerms);
			NUnit.Framework.Assert.AreEqual(leftTerms.HasOffsets(), rightTerms.HasOffsets());
			NUnit.Framework.Assert.AreEqual(leftTerms.HasPositions(), rightTerms.HasPositions
				());
			NUnit.Framework.Assert.AreEqual(leftTerms.HasPayloads(), rightTerms.HasPayloads()
				);
			TermsEnum leftTermsEnum = leftTerms.Iterator(null);
			TermsEnum rightTermsEnum = rightTerms.Iterator(null);
			AssertTermsEnumEquals(info, leftReader, leftTermsEnum, rightTermsEnum, true);
			AssertTermsSeekingEquals(info, leftTerms, rightTerms);
			if (deep)
			{
				int numIntersections = AtLeast(3);
				for (int i = 0; i < numIntersections; i++)
				{
					string re = AutomatonTestUtil.RandomRegexp(Random());
					CompiledAutomaton automaton = new CompiledAutomaton(new RegExp(re, RegExp.NONE).ToAutomaton
						());
					if (automaton.type == CompiledAutomaton.AUTOMATON_TYPE.NORMAL)
					{
						// TODO: test start term too
						TermsEnum leftIntersection = leftTerms.Intersect(automaton, null);
						TermsEnum rightIntersection = rightTerms.Intersect(automaton, null);
						AssertTermsEnumEquals(info, leftReader, leftIntersection, rightIntersection, Rarely
							());
					}
				}
			}
		}

		/// <summary>checks collection-level statistics on Terms</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AssertTermsStatisticsEquals(string info, Terms leftTerms, Terms
			 rightTerms)
		{
			//HM:revisit 
			//assert leftTerms.getComparator() == rightTerms.getComparator();
			if (leftTerms.GetDocCount() != -1 && rightTerms.GetDocCount() != -1)
			{
				NUnit.Framework.Assert.AreEqual(info, leftTerms.GetDocCount(), rightTerms.GetDocCount
					());
			}
			if (leftTerms.GetSumDocFreq() != -1 && rightTerms.GetSumDocFreq() != -1)
			{
				NUnit.Framework.Assert.AreEqual(info, leftTerms.GetSumDocFreq(), rightTerms.GetSumDocFreq
					());
			}
			if (leftTerms.GetSumTotalTermFreq() != -1 && rightTerms.GetSumTotalTermFreq() != 
				-1)
			{
				NUnit.Framework.Assert.AreEqual(info, leftTerms.GetSumTotalTermFreq(), rightTerms
					.GetSumTotalTermFreq());
			}
			if (leftTerms.Size() != -1 && rightTerms.Size() != -1)
			{
				NUnit.Framework.Assert.AreEqual(info, leftTerms.Size(), rightTerms.Size());
			}
		}

		private class RandomBits : Bits
		{
			internal FixedBitSet bits;

			internal RandomBits(int maxDoc, double pctLive, Random random)
			{
				bits = new FixedBitSet(maxDoc);
				for (int i = 0; i < maxDoc; i++)
				{
					if (random.NextDouble() <= pctLive)
					{
						bits.Set(i);
					}
				}
			}

			public override bool Get(int index)
			{
				return bits.Get(index);
			}

			public override int Length()
			{
				return bits.Length();
			}
		}

		/// <summary>
		/// checks the terms enum sequentially
		/// if deep is false, it does a 'shallow' test that doesnt go down to the docsenums
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AssertTermsEnumEquals(string info, IndexReader leftReader, TermsEnum
			 leftTermsEnum, TermsEnum rightTermsEnum, bool deep)
		{
			BytesRef term;
			Bits randomBits = new LuceneTestCase.RandomBits(leftReader.MaxDoc(), Random().NextDouble
				(), Random());
			DocsAndPositionsEnum leftPositions = null;
			DocsAndPositionsEnum rightPositions = null;
			DocsEnum leftDocs = null;
			DocsEnum rightDocs = null;
			while ((term = leftTermsEnum.Next()) != null)
			{
				NUnit.Framework.Assert.AreEqual(info, term, rightTermsEnum.Next());
				AssertTermStatsEquals(info, leftTermsEnum, rightTermsEnum);
				if (deep)
				{
					AssertDocsAndPositionsEnumEquals(info, leftPositions = leftTermsEnum.DocsAndPositions
						(null, leftPositions), rightPositions = rightTermsEnum.DocsAndPositions(null, rightPositions
						));
					AssertDocsAndPositionsEnumEquals(info, leftPositions = leftTermsEnum.DocsAndPositions
						(randomBits, leftPositions), rightPositions = rightTermsEnum.DocsAndPositions(randomBits
						, rightPositions));
					AssertPositionsSkippingEquals(info, leftReader, leftTermsEnum.DocFreq(), leftPositions
						 = leftTermsEnum.DocsAndPositions(null, leftPositions), rightPositions = rightTermsEnum
						.DocsAndPositions(null, rightPositions));
					AssertPositionsSkippingEquals(info, leftReader, leftTermsEnum.DocFreq(), leftPositions
						 = leftTermsEnum.DocsAndPositions(randomBits, leftPositions), rightPositions = rightTermsEnum
						.DocsAndPositions(randomBits, rightPositions));
					// with freqs:
					AssertDocsEnumEquals(info, leftDocs = leftTermsEnum.Docs(null, leftDocs), rightDocs
						 = rightTermsEnum.Docs(null, rightDocs), true);
					AssertDocsEnumEquals(info, leftDocs = leftTermsEnum.Docs(randomBits, leftDocs), rightDocs
						 = rightTermsEnum.Docs(randomBits, rightDocs), true);
					// w/o freqs:
					AssertDocsEnumEquals(info, leftDocs = leftTermsEnum.Docs(null, leftDocs, DocsEnum
						.FLAG_NONE), rightDocs = rightTermsEnum.Docs(null, rightDocs, DocsEnum.FLAG_NONE
						), false);
					AssertDocsEnumEquals(info, leftDocs = leftTermsEnum.Docs(randomBits, leftDocs, DocsEnum
						.FLAG_NONE), rightDocs = rightTermsEnum.Docs(randomBits, rightDocs, DocsEnum.FLAG_NONE
						), false);
					// with freqs:
					AssertDocsSkippingEquals(info, leftReader, leftTermsEnum.DocFreq(), leftDocs = leftTermsEnum
						.Docs(null, leftDocs), rightDocs = rightTermsEnum.Docs(null, rightDocs), true);
					AssertDocsSkippingEquals(info, leftReader, leftTermsEnum.DocFreq(), leftDocs = leftTermsEnum
						.Docs(randomBits, leftDocs), rightDocs = rightTermsEnum.Docs(randomBits, rightDocs
						), true);
					// w/o freqs:
					AssertDocsSkippingEquals(info, leftReader, leftTermsEnum.DocFreq(), leftDocs = leftTermsEnum
						.Docs(null, leftDocs, DocsEnum.FLAG_NONE), rightDocs = rightTermsEnum.Docs(null, 
						rightDocs, DocsEnum.FLAG_NONE), false);
					AssertDocsSkippingEquals(info, leftReader, leftTermsEnum.DocFreq(), leftDocs = leftTermsEnum
						.Docs(randomBits, leftDocs, DocsEnum.FLAG_NONE), rightDocs = rightTermsEnum.Docs
						(randomBits, rightDocs, DocsEnum.FLAG_NONE), false);
				}
			}
			NUnit.Framework.Assert.IsNull(info, rightTermsEnum.Next());
		}

		/// <summary>checks docs + freqs + positions + payloads, sequentially</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AssertDocsAndPositionsEnumEquals(string info, DocsAndPositionsEnum
			 leftDocs, DocsAndPositionsEnum rightDocs)
		{
			if (leftDocs == null || rightDocs == null)
			{
				NUnit.Framework.Assert.IsNull(leftDocs);
				NUnit.Framework.Assert.IsNull(rightDocs);
				return;
			}
			NUnit.Framework.Assert.AreEqual(info, -1, leftDocs.DocID());
			NUnit.Framework.Assert.AreEqual(info, -1, rightDocs.DocID());
			int docid;
			while ((docid = leftDocs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
			{
				NUnit.Framework.Assert.AreEqual(info, docid, rightDocs.NextDoc());
				int freq = leftDocs.Freq();
				NUnit.Framework.Assert.AreEqual(info, freq, rightDocs.Freq());
				for (int i = 0; i < freq; i++)
				{
					NUnit.Framework.Assert.AreEqual(info, leftDocs.NextPosition(), rightDocs.NextPosition
						());
					NUnit.Framework.Assert.AreEqual(info, leftDocs.GetPayload(), rightDocs.GetPayload
						());
					NUnit.Framework.Assert.AreEqual(info, leftDocs.StartOffset(), rightDocs.StartOffset
						());
					NUnit.Framework.Assert.AreEqual(info, leftDocs.EndOffset(), rightDocs.EndOffset()
						);
				}
			}
			NUnit.Framework.Assert.AreEqual(info, DocIdSetIterator.NO_MORE_DOCS, rightDocs.NextDoc
				());
		}

		/// <summary>checks docs + freqs, sequentially</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AssertDocsEnumEquals(string info, DocsEnum leftDocs, DocsEnum
			 rightDocs, bool hasFreqs)
		{
			if (leftDocs == null)
			{
				NUnit.Framework.Assert.IsNull(rightDocs);
				return;
			}
			NUnit.Framework.Assert.AreEqual(info, -1, leftDocs.DocID());
			NUnit.Framework.Assert.AreEqual(info, -1, rightDocs.DocID());
			int docid;
			while ((docid = leftDocs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
			{
				NUnit.Framework.Assert.AreEqual(info, docid, rightDocs.NextDoc());
				if (hasFreqs)
				{
					NUnit.Framework.Assert.AreEqual(info, leftDocs.Freq(), rightDocs.Freq());
				}
			}
			NUnit.Framework.Assert.AreEqual(info, DocIdSetIterator.NO_MORE_DOCS, rightDocs.NextDoc
				());
		}

		/// <summary>checks advancing docs</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AssertDocsSkippingEquals(string info, IndexReader leftReader, 
			int docFreq, DocsEnum leftDocs, DocsEnum rightDocs, bool hasFreqs)
		{
			if (leftDocs == null)
			{
				NUnit.Framework.Assert.IsNull(rightDocs);
				return;
			}
			int docid = -1;
			int averageGap = leftReader.MaxDoc() / (1 + docFreq);
			int skipInterval = 16;
			while (true)
			{
				if (Random().NextBoolean())
				{
					// nextDoc()
					docid = leftDocs.NextDoc();
					NUnit.Framework.Assert.AreEqual(info, docid, rightDocs.NextDoc());
				}
				else
				{
					// advance()
					int skip = docid + (int)Math.Ceil(Math.Abs(skipInterval + Random().NextGaussian()
						 * averageGap));
					docid = leftDocs.Advance(skip);
					NUnit.Framework.Assert.AreEqual(info, docid, rightDocs.Advance(skip));
				}
				if (docid == DocIdSetIterator.NO_MORE_DOCS)
				{
					return;
				}
				if (hasFreqs)
				{
					NUnit.Framework.Assert.AreEqual(info, leftDocs.Freq(), rightDocs.Freq());
				}
			}
		}

		/// <summary>checks advancing docs + positions</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AssertPositionsSkippingEquals(string info, IndexReader leftReader
			, int docFreq, DocsAndPositionsEnum leftDocs, DocsAndPositionsEnum rightDocs)
		{
			if (leftDocs == null || rightDocs == null)
			{
				NUnit.Framework.Assert.IsNull(leftDocs);
				NUnit.Framework.Assert.IsNull(rightDocs);
				return;
			}
			int docid = -1;
			int averageGap = leftReader.MaxDoc() / (1 + docFreq);
			int skipInterval = 16;
			while (true)
			{
				if (Random().NextBoolean())
				{
					// nextDoc()
					docid = leftDocs.NextDoc();
					NUnit.Framework.Assert.AreEqual(info, docid, rightDocs.NextDoc());
				}
				else
				{
					// advance()
					int skip = docid + (int)Math.Ceil(Math.Abs(skipInterval + Random().NextGaussian()
						 * averageGap));
					docid = leftDocs.Advance(skip);
					NUnit.Framework.Assert.AreEqual(info, docid, rightDocs.Advance(skip));
				}
				if (docid == DocIdSetIterator.NO_MORE_DOCS)
				{
					return;
				}
				int freq = leftDocs.Freq();
				NUnit.Framework.Assert.AreEqual(info, freq, rightDocs.Freq());
				for (int i = 0; i < freq; i++)
				{
					NUnit.Framework.Assert.AreEqual(info, leftDocs.NextPosition(), rightDocs.NextPosition
						());
					NUnit.Framework.Assert.AreEqual(info, leftDocs.GetPayload(), rightDocs.GetPayload
						());
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AssertTermsSeekingEquals(string info, Terms leftTerms, Terms rightTerms
			)
		{
			TermsEnum leftEnum = null;
			TermsEnum rightEnum = null;
			// just an upper bound
			int numTests = AtLeast(20);
			Random random = Random();
			// collect this number of terms from the left side
			HashSet<BytesRef> tests = new HashSet<BytesRef>();
			int numPasses = 0;
			while (numPasses < 10 && tests.Count < numTests)
			{
				leftEnum = leftTerms.Iterator(leftEnum);
				BytesRef term = null;
				while ((term = leftEnum.Next()) != null)
				{
					int code = random.Next(10);
					if (code == 0)
					{
						// the term
						tests.AddItem(BytesRef.DeepCopyOf(term));
					}
					else
					{
						if (code == 1)
						{
							// truncated subsequence of term
							term = BytesRef.DeepCopyOf(term);
							if (term.length > 0)
							{
								// truncate it
								term.length = random.Next(term.length);
							}
						}
						else
						{
							if (code == 2)
							{
								// term, but ensure a non-zero offset
								byte[] newbytes = new byte[term.length + 5];
								System.Array.Copy(term.bytes, term.offset, newbytes, 5, term.length);
								tests.AddItem(new BytesRef(newbytes, 5, term.length));
							}
							else
							{
								if (code == 3)
								{
									switch (Random().Next(3))
									{
										case 0:
										{
											tests.AddItem(new BytesRef());
											// before the first term
											break;
										}

										case 1:
										{
											tests.AddItem(new BytesRef(new byte[] { unchecked((byte)unchecked((int)(0xFF))), 
												unchecked((byte)unchecked((int)(0xFF))) }));
											// past the last term
											break;
										}

										case 2:
										{
											tests.AddItem(new BytesRef(TestUtil.RandomSimpleString(Random())));
											// random term
											break;
										}

										default:
										{
											throw new Exception();
										}
									}
								}
							}
						}
					}
				}
				numPasses++;
			}
			rightEnum = rightTerms.Iterator(rightEnum);
			AList<BytesRef> shuffledTests = new AList<BytesRef>(tests);
			Sharpen.Collections.Shuffle(shuffledTests, random);
			foreach (BytesRef b in shuffledTests)
			{
				if (Rarely())
				{
					// reuse the enums
					leftEnum = leftTerms.Iterator(leftEnum);
					rightEnum = rightTerms.Iterator(rightEnum);
				}
				bool seekExact = Random().NextBoolean();
				if (seekExact)
				{
					NUnit.Framework.Assert.AreEqual(info, leftEnum.SeekExact(b), rightEnum.SeekExact(
						b));
				}
				else
				{
					TermsEnum.SeekStatus leftStatus = leftEnum.SeekCeil(b);
					TermsEnum.SeekStatus rightStatus = rightEnum.SeekCeil(b);
					NUnit.Framework.Assert.AreEqual(info, leftStatus, rightStatus);
					if (leftStatus != TermsEnum.SeekStatus.END)
					{
						NUnit.Framework.Assert.AreEqual(info, leftEnum.Term(), rightEnum.Term());
						AssertTermStatsEquals(info, leftEnum, rightEnum);
					}
				}
			}
		}

		/// <summary>checks term-level statistics</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AssertTermStatsEquals(string info, TermsEnum leftTermsEnum, TermsEnum
			 rightTermsEnum)
		{
			NUnit.Framework.Assert.AreEqual(info, leftTermsEnum.DocFreq(), rightTermsEnum.DocFreq
				());
			if (leftTermsEnum.TotalTermFreq() != -1 && rightTermsEnum.TotalTermFreq() != -1)
			{
				NUnit.Framework.Assert.AreEqual(info, leftTermsEnum.TotalTermFreq(), rightTermsEnum
					.TotalTermFreq());
			}
		}

		/// <summary>checks that norms are the same across all fields</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AssertNormsEquals(string info, IndexReader leftReader, IndexReader
			 rightReader)
		{
			Fields leftFields = MultiFields.GetFields(leftReader);
			Fields rightFields = MultiFields.GetFields(rightReader);
			// Fields could be null if there are no postings,
			// but then it must be null for both
			if (leftFields == null || rightFields == null)
			{
				NUnit.Framework.Assert.IsNull(info, leftFields);
				NUnit.Framework.Assert.IsNull(info, rightFields);
				return;
			}
			foreach (string field in leftFields)
			{
				NumericDocValues leftNorms = MultiDocValues.GetNormValues(leftReader, field);
				NumericDocValues rightNorms = MultiDocValues.GetNormValues(rightReader, field);
				if (leftNorms != null && rightNorms != null)
				{
					AssertDocValuesEquals(info, leftReader.MaxDoc(), leftNorms, rightNorms);
				}
				else
				{
					NUnit.Framework.Assert.IsNull(info, leftNorms);
					NUnit.Framework.Assert.IsNull(info, rightNorms);
				}
			}
		}

		/// <summary>checks that stored fields of all documents are the same</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AssertStoredFieldsEquals(string info, IndexReader leftReader, 
			IndexReader rightReader)
		{
			//HM:revisit 
			//assert leftReader.maxDoc() == rightReader.maxDoc();
			for (int i = 0; i < leftReader.MaxDoc(); i++)
			{
				Org.Apache.Lucene.Document.Document leftDoc = leftReader.Document(i);
				Org.Apache.Lucene.Document.Document rightDoc = rightReader.Document(i);
				// TODO: I think this is bogus because we don't document what the order should be
				// from these iterators, etc. I think the codec/IndexReader should be free to order this stuff
				// in whatever way it wants (e.g. maybe it packs related fields together or something)
				// To fix this, we sort the fields in both documents by name, but
				// we still assume that all instances with same name are in order:
				IComparer<IndexableField> comp = new _IComparer_2103();
				leftDoc.GetFields().Sort(comp);
				rightDoc.GetFields().Sort(comp);
				Iterator<IndexableField> leftIterator = leftDoc.Iterator();
				Iterator<IndexableField> rightIterator = rightDoc.Iterator();
				while (leftIterator.HasNext())
				{
					NUnit.Framework.Assert.IsTrue(info, rightIterator.HasNext());
					AssertStoredFieldEquals(info, leftIterator.Next(), rightIterator.Next());
				}
				NUnit.Framework.Assert.IsFalse(info, rightIterator.HasNext());
			}
		}

		private sealed class _IComparer_2103 : IComparer<IndexableField>
		{
			public _IComparer_2103()
			{
			}

			public int Compare(IndexableField arg0, IndexableField arg1)
			{
				return Sharpen.Runtime.CompareOrdinal(arg0.Name(), arg1.Name());
			}
		}

		/// <summary>checks that two stored fields are equivalent</summary>
		public virtual void AssertStoredFieldEquals(string info, IndexableField leftField
			, IndexableField rightField)
		{
			NUnit.Framework.Assert.AreEqual(info, leftField.Name(), rightField.Name());
			NUnit.Framework.Assert.AreEqual(info, leftField.BinaryValue(), rightField.BinaryValue
				());
			NUnit.Framework.Assert.AreEqual(info, leftField.StringValue(), rightField.StringValue
				());
			NUnit.Framework.Assert.AreEqual(info, leftField.NumericValue(), rightField.NumericValue
				());
		}

		// TODO: should we check the FT at all?
		/// <summary>checks that term vectors across all fields are equivalent</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AssertTermVectorsEquals(string info, IndexReader leftReader, 
			IndexReader rightReader)
		{
			//HM:revisit 
			//assert leftReader.maxDoc() == rightReader.maxDoc();
			for (int i = 0; i < leftReader.MaxDoc(); i++)
			{
				Fields leftFields = leftReader.GetTermVectors(i);
				Fields rightFields = rightReader.GetTermVectors(i);
				AssertFieldsEquals(info, leftReader, leftFields, rightFields, Rarely());
			}
		}

		private static ICollection<string> GetDVFields(IndexReader reader)
		{
			ICollection<string> fields = new HashSet<string>();
			foreach (FieldInfo fi in MultiFields.GetMergedFieldInfos(reader))
			{
				if (fi.HasDocValues())
				{
					fields.AddItem(fi.name);
				}
			}
			return fields;
		}

		/// <summary>checks that docvalues across all fields are equivalent</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AssertDocValuesEquals(string info, IndexReader leftReader, IndexReader
			 rightReader)
		{
			ICollection<string> leftFields = GetDVFields(leftReader);
			ICollection<string> rightFields = GetDVFields(rightReader);
			NUnit.Framework.Assert.AreEqual(info, leftFields, rightFields);
			foreach (string field in leftFields)
			{
				{
					// TODO: clean this up... very messy
					NumericDocValues leftValues = MultiDocValues.GetNumericValues(leftReader, field);
					NumericDocValues rightValues = MultiDocValues.GetNumericValues(rightReader, field
						);
					if (leftValues != null && rightValues != null)
					{
						AssertDocValuesEquals(info, leftReader.MaxDoc(), leftValues, rightValues);
					}
					else
					{
						NUnit.Framework.Assert.IsNull(info, leftValues);
						NUnit.Framework.Assert.IsNull(info, rightValues);
					}
				}
				{
					BinaryDocValues leftValues = MultiDocValues.GetBinaryValues(leftReader, field);
					BinaryDocValues rightValues = MultiDocValues.GetBinaryValues(rightReader, field);
					if (leftValues != null && rightValues != null)
					{
						BytesRef scratchLeft = new BytesRef();
						BytesRef scratchRight = new BytesRef();
						for (int docID = 0; docID < leftReader.MaxDoc(); docID++)
						{
							leftValues.Get(docID, scratchLeft);
							rightValues.Get(docID, scratchRight);
							NUnit.Framework.Assert.AreEqual(info, scratchLeft, scratchRight);
						}
					}
					else
					{
						NUnit.Framework.Assert.IsNull(info, leftValues);
						NUnit.Framework.Assert.IsNull(info, rightValues);
					}
				}
				{
					SortedDocValues leftValues = MultiDocValues.GetSortedValues(leftReader, field);
					SortedDocValues rightValues = MultiDocValues.GetSortedValues(rightReader, field);
					if (leftValues != null && rightValues != null)
					{
						// numOrds
						NUnit.Framework.Assert.AreEqual(info, leftValues.GetValueCount(), rightValues.GetValueCount
							());
						// ords
						BytesRef scratchLeft = new BytesRef();
						BytesRef scratchRight = new BytesRef();
						for (int i = 0; i < leftValues.GetValueCount(); i++)
						{
							leftValues.LookupOrd(i, scratchLeft);
							rightValues.LookupOrd(i, scratchRight);
							NUnit.Framework.Assert.AreEqual(info, scratchLeft, scratchRight);
						}
						// bytes
						for (int docID = 0; docID < leftReader.MaxDoc(); docID++)
						{
							leftValues.Get(docID, scratchLeft);
							rightValues.Get(docID, scratchRight);
							NUnit.Framework.Assert.AreEqual(info, scratchLeft, scratchRight);
						}
					}
					else
					{
						NUnit.Framework.Assert.IsNull(info, leftValues);
						NUnit.Framework.Assert.IsNull(info, rightValues);
					}
				}
				{
					SortedSetDocValues leftValues = MultiDocValues.GetSortedSetValues(leftReader, field
						);
					SortedSetDocValues rightValues = MultiDocValues.GetSortedSetValues(rightReader, field
						);
					if (leftValues != null && rightValues != null)
					{
						// numOrds
						NUnit.Framework.Assert.AreEqual(info, leftValues.GetValueCount(), rightValues.GetValueCount
							());
						// ords
						BytesRef scratchLeft = new BytesRef();
						BytesRef scratchRight = new BytesRef();
						for (int i = 0; i < leftValues.GetValueCount(); i++)
						{
							leftValues.LookupOrd(i, scratchLeft);
							rightValues.LookupOrd(i, scratchRight);
							NUnit.Framework.Assert.AreEqual(info, scratchLeft, scratchRight);
						}
						// ord lists
						for (int docID = 0; docID < leftReader.MaxDoc(); docID++)
						{
							leftValues.SetDocument(docID);
							rightValues.SetDocument(docID);
							long ord;
							while ((ord = leftValues.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
							{
								NUnit.Framework.Assert.AreEqual(info, ord, rightValues.NextOrd());
							}
							NUnit.Framework.Assert.AreEqual(info, SortedSetDocValues.NO_MORE_ORDS, rightValues
								.NextOrd());
						}
					}
					else
					{
						NUnit.Framework.Assert.IsNull(info, leftValues);
						NUnit.Framework.Assert.IsNull(info, rightValues);
					}
				}
				{
					Bits leftBits = MultiDocValues.GetDocsWithField(leftReader, field);
					Bits rightBits = MultiDocValues.GetDocsWithField(rightReader, field);
					if (leftBits != null && rightBits != null)
					{
						NUnit.Framework.Assert.AreEqual(info, leftBits.Length(), rightBits.Length());
						for (int i = 0; i < leftBits.Length(); i++)
						{
							NUnit.Framework.Assert.AreEqual(info, leftBits.Get(i), rightBits.Get(i));
						}
					}
					else
					{
						NUnit.Framework.Assert.IsNull(info, leftBits);
						NUnit.Framework.Assert.IsNull(info, rightBits);
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AssertDocValuesEquals(string info, int num, NumericDocValues 
			leftDocValues, NumericDocValues rightDocValues)
		{
			NUnit.Framework.Assert.IsNotNull(info, leftDocValues);
			NUnit.Framework.Assert.IsNotNull(info, rightDocValues);
			for (int docID = 0; docID < num; docID++)
			{
				NUnit.Framework.Assert.AreEqual(leftDocValues.Get(docID), rightDocValues.Get(docID
					));
			}
		}

		// TODO: this is kinda stupid, we don't delete documents in the test.
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AssertDeletedDocsEquals(string info, IndexReader leftReader, 
			IndexReader rightReader)
		{
			//HM:revisit 
			//assert leftReader.numDeletedDocs() == rightReader.numDeletedDocs();
			Bits leftBits = MultiFields.GetLiveDocs(leftReader);
			Bits rightBits = MultiFields.GetLiveDocs(rightReader);
			if (leftBits == null || rightBits == null)
			{
				NUnit.Framework.Assert.IsNull(info, leftBits);
				NUnit.Framework.Assert.IsNull(info, rightBits);
				return;
			}
			//HM:revisit 
			//assert leftReader.maxDoc() == rightReader.maxDoc();
			NUnit.Framework.Assert.AreEqual(info, leftBits.Length(), rightBits.Length());
			for (int i = 0; i < leftReader.MaxDoc(); i++)
			{
				NUnit.Framework.Assert.AreEqual(info, leftBits.Get(i), rightBits.Get(i));
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void AssertFieldInfosEquals(string info, IndexReader leftReader, IndexReader
			 rightReader)
		{
			FieldInfos leftInfos = MultiFields.GetMergedFieldInfos(leftReader);
			FieldInfos rightInfos = MultiFields.GetMergedFieldInfos(rightReader);
			// TODO: would be great to verify more than just the names of the fields!
			TreeSet<string> left = new TreeSet<string>();
			TreeSet<string> right = new TreeSet<string>();
			foreach (FieldInfo fi in leftInfos)
			{
				left.AddItem(fi.name);
			}
			foreach (FieldInfo fi_1 in rightInfos)
			{
				right.AddItem(fi_1.name);
			}
			NUnit.Framework.Assert.AreEqual(info, left, right);
		}

		/// <summary>
		/// Returns true if the file exists (can be opened), false
		/// if it cannot be opened, and (unlike Java's
		/// File.exists) throws IOException if there's some
		/// unexpected error.
		/// </summary>
		/// <remarks>
		/// Returns true if the file exists (can be opened), false
		/// if it cannot be opened, and (unlike Java's
		/// File.exists) throws IOException if there's some
		/// unexpected error.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public static bool SlowFileExists(Directory dir, string fileName)
		{
			try
			{
				dir.OpenInput(fileName, IOContext.DEFAULT).Close();
				return true;
			}
			catch (IOException)
			{
				return false;
			}
		}

		/// <summary>A base location for temporary files of a given test.</summary>
		/// <remarks>
		/// A base location for temporary files of a given test. Helps in figuring out
		/// which tests left which files and where.
		/// </remarks>
		private static FilePath tempDirBase;

		/// <summary>Retry to create temporary file name this many times.</summary>
		/// <remarks>Retry to create temporary file name this many times.</remarks>
		private const int TEMP_NAME_RETRY_THRESHOLD = 9999;

		/// <summary>This method is deprecated for a reason.</summary>
		/// <remarks>
		/// This method is deprecated for a reason. Do not use it. Call
		/// <see cref="CreateTempDir()">CreateTempDir()</see>
		/// or
		/// <see cref="CreateTempDir(string)">CreateTempDir(string)</see>
		/// or
		/// <see cref="CreateTempFile(string, string)">CreateTempFile(string, string)</see>
		/// .
		/// </remarks>
		[Obsolete]
		public static FilePath GetBaseTempDirForTestClass()
		{
			lock (typeof(LuceneTestCase))
			{
				if (tempDirBase == null)
				{
					FilePath directory = new FilePath(Runtime.GetProperty("tempDir", Runtime.GetProperty
						("java.io.tmpdir")));
					//HM:revisit 
					//assert directory.exists() && 
					RandomizedContext ctx = RandomizedContext.Current();
					Type clazz = ctx.GetTargetClass();
					string prefix = clazz.FullName;
					prefix = prefix.ReplaceFirst("^org.apache.lucene.", "lucene.");
					prefix = prefix.ReplaceFirst("^org.apache.solr.", "solr.");
					int attempt = 0;
					FilePath f;
					do
					{
						if (attempt++ >= TEMP_NAME_RETRY_THRESHOLD)
						{
							throw new RuntimeException("Failed to get a temporary name too many times, check your temp directory and consider manually cleaning it: "
								 + directory.GetAbsolutePath());
						}
						f = new FilePath(directory, prefix + "-" + ctx.GetRunnerSeedAsString() + "-" + string
							.Format(Sharpen.Extensions.GetEnglishCulture(), "%03d", attempt));
					}
					while (!f.Mkdirs());
					tempDirBase = f;
					RegisterToRemoveAfterSuite(tempDirBase);
				}
			}
			return tempDirBase;
		}

		/// <summary>Creates an empty, temporary folder (when the name of the folder is of no importance).
		/// 	</summary>
		/// <remarks>Creates an empty, temporary folder (when the name of the folder is of no importance).
		/// 	</remarks>
		/// <seealso cref="CreateTempDir(string)">CreateTempDir(string)</seealso>
		public static FilePath CreateTempDir()
		{
			return CreateTempDir("tempDir");
		}

		/// <summary>
		/// Creates an empty, temporary folder with the given name prefix under the
		/// test class's
		/// <see cref="GetBaseTempDirForTestClass()">GetBaseTempDirForTestClass()</see>
		/// .
		/// <p>The folder will be automatically removed after the
		/// test class completes successfully. The test should close any file handles that would prevent
		/// the folder from being removed.
		/// </summary>
		public static FilePath CreateTempDir(string prefix)
		{
			FilePath @base = GetBaseTempDirForTestClass();
			int attempt = 0;
			FilePath f;
			do
			{
				if (attempt++ >= TEMP_NAME_RETRY_THRESHOLD)
				{
					throw new RuntimeException("Failed to get a temporary name too many times, check your temp directory and consider manually cleaning it: "
						 + @base.GetAbsolutePath());
				}
				f = new FilePath(@base, prefix + "-" + string.Format(Sharpen.Extensions.GetEnglishCulture()
					, "%03d", attempt));
			}
			while (!f.Mkdirs());
			RegisterToRemoveAfterSuite(f);
			return f;
		}

		/// <summary>
		/// Creates an empty file with the given prefix and suffix under the
		/// test class's
		/// <see cref="GetBaseTempDirForTestClass()">GetBaseTempDirForTestClass()</see>
		/// .
		/// <p>The file will be automatically removed after the
		/// test class completes successfully. The test should close any file handles that would prevent
		/// the folder from being removed.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public static FilePath CreateTempFile(string prefix, string suffix)
		{
			FilePath @base = GetBaseTempDirForTestClass();
			int attempt = 0;
			FilePath f;
			do
			{
				if (attempt++ >= TEMP_NAME_RETRY_THRESHOLD)
				{
					throw new RuntimeException("Failed to get a temporary name too many times, check your temp directory and consider manually cleaning it: "
						 + @base.GetAbsolutePath());
				}
				f = new FilePath(@base, prefix + "-" + string.Format(Sharpen.Extensions.GetEnglishCulture()
					, "%03d", attempt) + suffix);
			}
			while (!f.CreateNewFile());
			RegisterToRemoveAfterSuite(f);
			return f;
		}

		/// <summary>Creates an empty temporary file.</summary>
		/// <remarks>Creates an empty temporary file.</remarks>
		/// <seealso cref="CreateTempFile(string, string)"></seealso>
		/// <exception cref="System.IO.IOException"></exception>
		public static FilePath CreateTempFile()
		{
			return CreateTempFile("tempFile", ".tmp");
		}

		/// <summary>
		/// A queue of temporary resources to be removed after the
		/// suite completes.
		/// </summary>
		/// <remarks>
		/// A queue of temporary resources to be removed after the
		/// suite completes.
		/// </remarks>
		/// <seealso cref="RegisterToRemoveAfterSuite(Sharpen.FilePath)">RegisterToRemoveAfterSuite(Sharpen.FilePath)
		/// 	</seealso>
		private static readonly IList<FilePath> cleanupQueue = new AList<FilePath>();

		/// <summary>Register temporary folder for removal after the suite completes.</summary>
		/// <remarks>Register temporary folder for removal after the suite completes.</remarks>
		private static void RegisterToRemoveAfterSuite(FilePath f)
		{
			//HM:revisit 
			//assert f != null;
			if (LuceneTestCase.LEAVE_TEMPORARY)
			{
				System.Console.Error.WriteLine("INFO: Will leave temporary file: " + f.GetAbsolutePath
					());
				return;
			}
			lock (cleanupQueue)
			{
				cleanupQueue.AddItem(f);
			}
		}

		private class TemporaryFilesCleanupRule : TestRuleAdapter
		{
			/// <exception cref="System.Exception"></exception>
			protected override void Before()
			{
				base.Before();
			}

			//HM:revisit 
			//assert tempDirBase == null;
			/// <exception cref="System.Exception"></exception>
			protected override void AfterAlways(IList<Exception> errors)
			{
				// Drain cleanup queue and clear it.
				FilePath[] everything;
				string tempDirBasePath;
				lock (cleanupQueue)
				{
					tempDirBasePath = (tempDirBase != null ? tempDirBase.GetAbsolutePath() : null);
					tempDirBase = null;
					Sharpen.Collections.Reverse(cleanupQueue);
					everything = new FilePath[cleanupQueue.Count];
					Sharpen.Collections.ToArray(cleanupQueue, everything);
					cleanupQueue.Clear();
				}
				// Only check and throw an IOException on un-removable files if the test
				// was successful. Otherwise just report the path of temporary files
				// and leave them there.
				if (LuceneTestCase.suiteFailureMarker.WasSuccessful())
				{
					try
					{
						TestUtil.Rm(everything);
					}
					catch (IOException e)
					{
						Type suiteClass = RandomizedContext.Current().GetTargetClass();
						if (suiteClass.IsAnnotationPresent(typeof(LuceneTestCase.SuppressTempFileChecks)))
						{
							System.Console.Error.WriteLine("WARNING: Leftover undeleted temporary files (bugUrl: "
								);
							//HM:revisit. line below throwing an exception
							return;
						}
						throw;
					}
				}
				else
				{
					if (tempDirBasePath != null)
					{
						System.Console.Error.WriteLine("NOTE: leaving temporary files on disk at: " + tempDirBasePath
							);
					}
				}
			}
		}
	}
}
