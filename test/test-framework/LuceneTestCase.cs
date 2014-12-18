/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Globalization;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Randomized;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.Util;
using NUnit.Framework;

using ConcurrentMergeScheduler = Lucene.Net.Index.ConcurrentMergeScheduler;
using Insanity = Lucene.Net.Util.FieldCacheSanityChecker.Insanity;
using FieldCache = Lucene.Net.Search.FieldCache;
using Lucene.Net.TestFramework.Support;
using System.Collections.Generic;
using Lucene.Net.Search;

using Lucene.Net.TestFramework;
using Version = Lucene.Net.Util.Version;
using Lucene.Net.Store;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net
{

    /// <summary> Base class for all Lucene unit tests.  
    /// <p/>
    /// Currently the
    /// only added functionality over JUnit's TestCase is
    /// asserting that no unhandled exceptions occurred in
    /// threads launched by ConcurrentMergeScheduler and asserting sane
    /// FieldCache usage athe moment of tearDown.
    /// <p/>
    /// If you
    /// override either <c>setUp()</c> or
    /// <c>tearDown()</c> in your unit test, make sure you
    /// call <c>super.setUp()</c> and
    /// <c>super.tearDown()</c>
    /// <p/>
    /// </summary>
    /// <seealso cref="assertSaneFieldCaches">
    /// </seealso>
    [Serializable]
    [TestFixture]
    public abstract partial class LuceneTestCase : Assert
    {
        // --------------------------------------------------------------------
        // Test groups, system properties and other annotations modifying tests
        // --------------------------------------------------------------------

        public const string SYSPROP_NIGHTLY = "tests.nightly";
        public const string SYSPROP_WEEKLY = "tests.weekly";
        public const string SYSPROP_AWAITSFIX = "tests.awaitsfix";
        public const string SYSPROP_SLOW = "tests.slow";
        public const string SYSPROP_BADAPPLES = "tests.badapples";

        /** @see #ignoreAfterMaxFailures*/
        private const string SYSPROP_MAXFAILURES = "tests.maxfailures";

        /** @see #ignoreAfterMaxFailures*/
        private const string SYSPROP_FAILFAST = "tests.failfast";




		public static readonly Version TEST_VERSION_CURRENT = Version.LUCENE_48;

        public static readonly bool VERBOSE = RandomizedTest.SystemPropertyAsBoolean("tests.verbose", false);

        public static readonly bool INFOSTREAM = RandomizedTest.SystemPropertyAsBoolean("tests.infostream", VERBOSE);

        public static readonly int RANDOM_MULTIPLIER = RandomizedTest.SystemPropertyAsInt("tests.multiplier", 1);

        public static readonly string DEFAULT_LINE_DOCS_FILE = "europarl.lines.txt.gz";

        public static readonly string JENKINS_LARGE_LINE_DOCS_FILE = "enwiki.random.lines.txt";

        public static readonly string TEST_CODEC = SystemProperties.GetProperty("tests.codec", "random");

        public static readonly string TEST_DOCVALUESFORMAT = SystemProperties.GetProperty("tests.docvaluesformat",
            "random");

        public static readonly string TEST_DIRECTORY = SystemProperties.GetProperty("tests.directory", "random");

        public static readonly string TEST_LINE_DOCS_FILE = SystemProperties.GetProperty("tests.linedocsfile",
            DEFAULT_LINE_DOCS_FILE);

        public static readonly bool TEST_NIGHTLY = RandomizedTest.SystemPropertyAsBoolean(NightlyAttribute.KEY, false);

        public static readonly bool TEST_WEEKLY = RandomizedTest.SystemPropertyAsBoolean(WeeklyAttribute.KEY, false);

        public static readonly bool TEST_AWAITSFIX = RandomizedTest.SystemPropertyAsBoolean(AwaitsFixAttribute.KEY,
            false);

        public static readonly bool TEST_SLOW = RandomizedTest.SystemPropertyAsBoolean(SlowAttribute.KEY, false);

        //public static readonly MockDirectoryWrapper.Throttling TEST_THROTTLING = TEST_NIGHTLY ? MockDirectoryWrapper.Throttling.SOMETIMES : MockDirectoryWrapper.Throttling.NEVER;
		public static readonly MockDirectoryWrapper.Throttling TEST_THROTTLING = TEST_NIGHTLY
			 ? MockDirectoryWrapper.Throttling.SOMETIMES : MockDirectoryWrapper.Throttling.NEVER;

        public static readonly System.IO.DirectoryInfo TEMP_DIR;

        static LuceneTestCase()
        {
            String s = SystemProperties.GetProperty("tempDir", System.IO.Path.GetTempPath());
            if (s == null)
                throw new SystemException(
                    "To run tests, you need to define system property 'tempDir' or 'java.io.tmpdir'.");

            TEMP_DIR = new System.IO.DirectoryInfo(s);
            if (!TEMP_DIR.Exists) TEMP_DIR.Create();

            CORE_DIRECTORIES = new List<string>(FS_DIRECTORIES);
            CORE_DIRECTORIES.Add("RAMDirectory");


        }

        private static readonly string[] IGNORED_INVARIANT_PROPERTIES =
        {
            "user.timezone", "java.rmi.server.randomIDs"
        };

        private static readonly IList<String> FS_DIRECTORIES = new[]
                                                               {
                                                                   "SimpleFSDirectory",
                                                                   "NIOFSDirectory",
                                                                   "MMapDirectory"
                                                               };

        private static readonly IList<String> CORE_DIRECTORIES;

        // .NET Port: this Java code moved to static ctor above
        //static {
        //  CORE_DIRECTORIES = new ArrayList<String>(FS_DIRECTORIES);
        //  CORE_DIRECTORIES.add("RAMDirectory");
        //};

        protected static readonly ISet<String> doesntSupportOffsets = new HashSet<String>(new[]
                                                                                          {
                                                                                              "Lucene3x",
                                                                                              "MockFixedIntBlock",
                                                                                              "MockVariableIntBlock",
                                                                                              "MockSep",
                                                                                              "MockRandom"
                                                                                          });

        public void Test()
        {

        }

        public static bool PREFLEX_IMPERSONATION_IS_ACTIVE;

        //private static readonly TestRuleStoreClassName classNameRule;

        //internal static readonly TestRuleSetupAndRestoreClassEnv classEnvRule;

        //public static readonly TestRuleMarkFailure suiteFailureMarker =
        //    new TestRuleMarkFailure();

        //internal static readonly TestRuleIgnoreAfterMaxFailures ignoreAfterMaxFailures;

        private const long STATIC_LEAK_THRESHOLD = 10*1024*1024;

        //private static readonly ISet<String> STATIC_LEAK_IGNORED_TYPES =
        //    new HashSet<String>(new[] {
        //    "org.slf4j.Logger",
        //    "org.apache.solr.SolrLogFormatter",
        //    typeof(EnumSet).FullName});

        //    public static TestRule classRules = RuleChain
        //.outerRule(new TestRuleIgnoreTestSuites())
        //.around(ignoreAfterMaxFailures)
        //.around(suiteFailureMarker)
        //.around(new TestRuleAssertionsRequired())
        //.around(new StaticFieldsInvariantRule(STATIC_LEAK_THRESHOLD, true) {
        //  @Override
        //  protected boolean accept(java.lang.reflect.Field field) {
        //    // Don't count known classes that consume memory once.
        //    if (STATIC_LEAK_IGNORED_TYPES.contains(field.getType().getName())) {
        //      return false;
        //    }
        //    // Don't count references from ourselves, we're top-level.
        //    if (field.getDeclaringClass() == LuceneTestCase.class) {
        //      return false;
        //    }
        //    return super.accept(field);
        //  }
        //})
        //.around(new NoClassHooksShadowingRule())
        //.around(new NoInstanceHooksOverridesRule() {
        //  @Override
        //  protected boolean verify(Method key) {
        //    String name = key.getName();
        //    return !(name.equals("setUp") || name.equals("tearDown"));
        //  }
        //})
        //.around(new SystemPropertiesInvariantRule(IGNORED_INVARIANT_PROPERTIES))
        //.around(classNameRule = new TestRuleStoreClassName())
        //.around(classEnvRule = new TestRuleSetupAndRestoreClassEnv());

        //static {
        //  int maxFailures = systemPropertyAsInt(SYSPROP_MAXFAILURES, Integer.MAX_VALUE);
        //  boolean failFast = systemPropertyAsBoolean(SYSPROP_FAILFAST, false);

        //  if (failFast) {
        //    if (maxFailures == Integer.MAX_VALUE) {
        //      maxFailures = 1;
        //    } else {
        //      Logger.getLogger(LuceneTestCase.class.getSimpleName()).warning(
        //          "Property '" + SYSPROP_MAXFAILURES + "'=" + maxFailures + ", 'failfast' is" +
        //          " ignored.");
        //    }
        //  }

        //  ignoreAfterMaxFailures = new TestRuleIgnoreAfterMaxFailures(maxFailures);
        //}

        //bool allowDocsOutOfOrder = true;

        public LuceneTestCase()
            : base()
        {
        }



        public LuceneTestCase(System.String name)
        {
        }

        [SetUp]
        public virtual void SetUp()
        {
            //ConcurrentMergeScheduler.SetTestMode();
            //parentChainCallRule.setupCalled = true;
        }

        /// <summary> Forcible purges all cache entries from the FieldCache.
        /// <p/>
        /// This method will be called by tearDown to clean up FieldCache.DEFAULT.
        /// If a (poorly written) test has some expectation that the FieldCache
        /// will persist across test methods (ie: a static IndexReader) this 
        /// method can be overridden to do nothing.
        /// <p/>
        /// </summary>
        /// <seealso cref="FieldCache.PurgeAllCaches()">
        /// </seealso>
        protected internal virtual void PurgeFieldCache(IFieldCache fc)
        {
            fc.PurgeAllCaches();
        }

        protected internal virtual string GetTestLabel()
        {
            return NUnit.Framework.TestContext.CurrentContext.Test.FullName;
        }

        [TearDown]
        public virtual void TearDown()
        {
            try
            {
                // this isn't as useful as calling directly from the scope where the 
                // index readers are used, because they could be gc'ed just before
                // tearDown is called.
                // But it's better then nothing.
                AssertSaneFieldCaches(GetTestLabel());

                //if (ConcurrentMergeScheduler.AnyUnhandledExceptions())
                //{
                //    // Clear the failure so that we don't just keep
                //    // failing subsequent test cases
                //    ConcurrentMergeScheduler.ClearUnhandledExceptions();
                //    Assert.Fail("ConcurrentMergeScheduler hit unhandled exceptions");
                //}
            }
            finally
            {
                PurgeFieldCache(Lucene.Net.Search.FieldCache.DEFAULT);
            }

            //base.TearDown();  // {{Aroush-2.9}}
            this.seed = null;

            //parentChainCallRule.teardownCalled = true;
        }


		public virtual T CloseAfterTest<T>(T resource) where T:IDisposable
		{
			return RandomizedContext.Current.CloseAtEnd(resource, LifecycleScope.TEST);
		}

		public static SegmentReader GetOnlySegmentReader(DirectoryReader reader)
		{
			IList<AtomicReaderContext> subReaders = reader.Leaves;
			if (subReaders.Count != 1)
			{
				throw new ArgumentException(reader + " has " + subReaders.Count + " segments instead of exactly one"
					);
			}
			AtomicReader r = ((AtomicReader)subReaders[0].Reader);
			NUnit.Framework.Assert.IsTrue(r is SegmentReader);
			return (SegmentReader)r;
		}
		
        /// <summary> Asserts that FieldCacheSanityChecker does not detect any 
        /// problems with FieldCache.DEFAULT.
        /// <p/>
        /// If any problems are found, they are logged to System.err 
        /// (allong with the msg) when the Assertion is thrown.
        /// <p/>
        /// This method is called by tearDown after every test method, 
        /// however IndexReaders scoped inside test methods may be garbage 
        /// collected prior to this method being called, causing errors to 
        /// be overlooked. Tests are encouraged to keep their IndexReaders 
        /// scoped at the class level, or to explicitly call this method 
        /// directly in the same scope as the IndexReader.
        /// <p/>
        /// </summary>
        /// <seealso cref="FieldCacheSanityChecker">
        /// </seealso>
        protected internal virtual void AssertSaneFieldCaches(string msg)
        {
            FieldCache.CacheEntry[] entries = FieldCache.DEFAULT.GetCacheEntries();
            Insanity[] insanity = null;
            try
            {
                try
                {
                    insanity = FieldCacheSanityChecker.CheckSanity(entries);
                }
                catch (System.SystemException e)
                {
                    System.IO.StreamWriter temp_writer;
                    temp_writer = new StreamWriter(Console.OpenStandardError(),
                        Console.Error.Encoding) {AutoFlush = true};
                    DumpArray(msg + ": FieldCache", entries, temp_writer);
                    throw;
                }

                Assert.AreEqual(0, insanity.Length, msg + ": Insane FieldCache usage(s) found");
                insanity = null;
            }
            finally
            {

                // report this in the event of any exception/failure
                // if no failure, then insanity will be null anyway
                if (null != insanity)
                {
                    System.IO.StreamWriter temp_writer2;
                    temp_writer2 = new System.IO.StreamWriter(System.Console.OpenStandardError(),
                        System.Console.Error.Encoding);
                    temp_writer2.AutoFlush = true;
                    DumpArray(msg + ": Insane FieldCache usage(s)", insanity, temp_writer2);
                }
            }
        }

        /// <summary> Convinience method for logging an iterator.</summary>
        /// <param name="label">String logged before/after the items in the iterator
        /// </param>
        /// <param name="iter">Each next() is toString()ed and logged on it's own line. If iter is null this is logged differnetly then an empty iterator.
        /// </param>
        /// <param name="stream">Stream to log messages to.
        /// </param>
        public static void DumpIterator(System.String label, System.Collections.IEnumerator iter,
            System.IO.StreamWriter stream)
        {
            stream.WriteLine("*** BEGIN " + label + " ***");
            if (null == iter)
            {
                stream.WriteLine(" ... NULL ...");
            }
            else
            {
                while (iter.MoveNext())
                {
                    stream.WriteLine(iter.Current.ToString());
                }
            }
            stream.WriteLine("*** END " + label + " ***");
        }

        /// <summary> Convinience method for logging an array.  Wraps the array in an iterator and delegates</summary>
        /// <seealso cref="dumpIterator(String,Iterator,PrintStream)">
        /// </seealso>
        public static void DumpArray(System.String label, System.Object[] objs, System.IO.StreamWriter stream)
        {
            System.Collections.IEnumerator iter = (null == objs)
                ? null
                : new System.Collections.ArrayList(objs).GetEnumerator();
            DumpIterator(label, iter, stream);
        }


        /**
        * Returns true if something should happen rarely,
        * <p>
        * The actual number returned will be influenced by whether {@link #TEST_NIGHTLY}
        * is active and <see cref="RANDOM_MULTIPLIER"/>
        */
        public static bool Rarely(Random random)
        {
            int p = TEST_NIGHTLY ? 10 : 1;
            p += (p * (int)Math.Log((double)RANDOM_MULTIPLIER));
            int min = 100 - Math.Min(p, 50); // never more than 50
            return random.Next(100) >= min;
        }

        public static bool Rarely()
        {
            return Rarely(new Random());
        }

		public static bool Usually(Random random)
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
		public static IndexWriterConfig NewIndexWriterConfig(Version v, Analyzer a)
		{
			return NewIndexWriterConfig(Random(), v, a);
		}
		public static IndexWriterConfig NewIndexWriterConfig(Random r, Version v, Analyzer a)
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
				c.SetMergedSegmentWarmer(new SimpleMergedSegmentWarmer(c.InfoStream));
			}
			c.UseCompoundFile = r.NextBoolean();
			c.SetReaderPooling(r.NextBoolean());
			c.SetReaderTermsIndexDivisor(TestUtil.NextInt(r, 1, 4));
			c.SetCheckIntegrityAtMerge(r.NextBoolean());
			return c;
		}

		public static MergePolicy NewMergePolicy(Random r)
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

		public static AlcoholicMergePolicy NewAlcoholicMergePolicy(Random r, TimeZoneInfo
			 tz)
		{
			return new AlcoholicMergePolicy(tz, new Random(r.NextLong()));
		}

		public static LogMergePolicy NewLogMergePolicy(Random r)
		{
			LogMergePolicy logmp = r.NextBoolean() ? new LogDocMergePolicy() : new LogByteSizeMergePolicy();
			logmp.CalibrateSizeByDeletes = r.NextBoolean();
			if (Rarely(r))
			{
				logmp.MergeFactor = TestUtil.NextInt(r, 2, 9);
			}
			else
			{
				logmp.MergeFactor = TestUtil.NextInt(r, 10, 50);
			}
			ConfigureRandom(r, logmp);
			return logmp;
		}

		private static void ConfigureRandom(Random r, MergePolicy mergePolicy)
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

		public static TieredMergePolicy NewTieredMergePolicy(Random r)
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
			logmp.MergeFactor = mergeFactor;
			return logmp;
		}

		public static MergePolicy NewLogMergePolicy(int mergeFactor)
		{
			LogMergePolicy logmp = NewLogMergePolicy();
			logmp.MergeFactor = mergeFactor;
			return logmp;
		}

		// if you want it in LiveIndexWriterConfig: it must and will be tested here.
		public static void MaybeChangeLiveIndexWriterConfig(Random r, LiveIndexWriterConfig
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
					c.SetMergedSegmentWarmer(new SimpleMergedSegmentWarmer(c.InfoStream));
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
				c.UseCompoundFile = r.NextBoolean();
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
				MergeScheduler ms = c.MergeScheduler;
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
				MergePolicy mp = c.MergePolicy;
				ConfigureRandom(r, mp);
				if (mp is LogMergePolicy)
				{
					LogMergePolicy logmp = (LogMergePolicy)mp;
					logmp.CalibrateSizeByDeletes = r.NextBoolean();
					if (Rarely(r))
					{
						logmp.MergeFactor = TestUtil.NextInt(r, 2, 9);
					}
					else
					{
						logmp.MergeFactor = TestUtil.NextInt(r, 10, 50);
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
        public static BaseDirectoryWrapper NewDirectory()
        {
			return NewDirectory(Random());
        }

		public static BaseDirectoryWrapper NewDirectory(Random r)
		{
			return WrapDirectory(r, NewDirectoryImpl(r, TEST_DIRECTORY), Rarely(r));
		}

		public static MockDirectoryWrapper NewMockDirectory()
		{
			return NewMockDirectory(Random());
		}

		public static MockDirectoryWrapper NewMockDirectory(Random r)
		{
			return (MockDirectoryWrapper)WrapDirectory(r, NewDirectoryImpl(r, TEST_DIRECTORY)
				, false);
		}

		public static MockDirectoryWrapper NewMockFSDirectory(FileInfo f)
		{
			return (MockDirectoryWrapper)NewFSDirectory(f, null, false);
		}
        protected static void Verbose(string message)
        {
            if(LuceneTestCase.VERBOSE) {
                Console.WriteLine(message);
            }
        }

		public static BaseDirectoryWrapper NewFSDirectory(FileInfo f)
		{
			return NewFSDirectory(f, null);
		}
		public static BaseDirectoryWrapper NewFSDirectory(FileInfo f, LockFactory lf)
		{
			return NewFSDirectory(f, lf, Rarely());
		}
		private static BaseDirectoryWrapper NewFSDirectory(FileInfo f, LockFactory lf, bool
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
					wrapped.LockFactory = lf;
				}
				return wrapped;
			}
			catch (Exception e)
			{
				Thrower.Rethrow(e);
				throw null;
			}
		}
		public static BaseDirectoryWrapper NewDirectory(Random r, Directory d)
		{
			Directory impl = NewDirectoryImpl(r, TEST_DIRECTORY);
			foreach (string file in d.ListAll())
			{
				d.Copy(impl, file, file, NewIOContext(r));
			}
			return WrapDirectory(r, impl, Rarely(r));
		}
        private static BaseDirectoryWrapper WrapDirectory(Random random, Directory directory, bool bare) 
        {
            if (Rarely(random)) {
                directory = new NRTCachingDirectory(directory, random.NextDouble(), random.NextDouble());
            }

            if (Rarely(random))
            {
                double maxMBPerSec = 10 + 5 * (random.NextDouble() - 0.5);
                if (LuceneTestCase.VERBOSE)
                {
                    Verbose("LuceneTestCase: will rate limit output IndexOutput to " + maxMBPerSec + " MB/sec");
                }

                /*
                  RateLimitedDirectoryWrapper rateLimitedDirectoryWrapper = new RateLimitedDirectoryWrapper(directory);
                  switch (random.Next(10)) {
                    case 3: // sometimes rate limit on flush
                      rateLimitedDirectoryWrapper.SetMaxWriteMBPerSec(maxMBPerSec, Context.FLUSH);
                      break;
                    case 2: // sometimes rate limit flush & merge
                      rateLimitedDirectoryWrapper.SetMaxWriteMBPerSec(maxMBPerSec, Context.FLUSH);
                      rateLimitedDirectoryWrapper.SetMaxWriteMBPerSec(maxMBPerSec, Context.MERGE);
                      break;
                    default:
                      rateLimitedDirectoryWrapper.SetMaxWriteMBPerSec(maxMBPerSec, Context.MERGE);
                  }
                  directory =  rateLimitedDirectoryWrapper;
      
                }

                if (bare) {
                  var wrapper = new BaseDirectoryWrapper(directory);
                  closeAfterSuite(new CloseableDirectory(wrapper, suiteFailureMarker));
                  return wrapper;
                } else {
                  var mock = new MockDirectoryWrapper(random, directory);
      
                  mock.setThrottling(TEST_THROTTLING);
                  closeAfterSuite(new CloseableDirectory(mock, suiteFailureMarker));
                  return mock;
                }*/
            }
            return null;
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

		public static Field NewStringField(Random random, string name, string value
			, Field.Store stored)
		{
			return NewField(random, name, value, stored == Field.Store.YES ? StringField.TYPE_STORED
				 : StringField.TYPE_NOT_STORED);
		}

		public static Field NewTextField(Random random, string name, string value
			, Field.Store stored)
		{
			return NewField(random, name, value, stored == Field.Store.YES ? TextField.TYPE_STORED
				 : TextField.TYPE_NOT_STORED);
		}

		public static Field NewField(string name, string value, FieldType type)
		{
			return NewField(Random(), name, value, type);
		}

		public static Field NewField(Random random, string name, string value, FieldType
			 type)
		{
			
			if (Usually(random) || !type.Indexed)
			{
				// most of the time, don't modify the params
				return new Field(name, value, type);
			}
			// TODO: once all core & test codecs can index
			// offsets, sometimes randomly turn on offsets if we are
			// already indexing positions...
			FieldType newType = new FieldType(type);
			if (!newType.Stored && random.NextBoolean())
			{
				newType.Stored = true;
			}
			// randomly store it
			if (!newType.StoreTermVectors && random.NextBoolean())
			{
				newType.StoreTermVectors = true;
				if (!newType.StoreTermVectorOffsets)
				{
					newType.StoreTermVectorOffsets = random.NextBoolean();
				}
				if (!newType.StoreTermVectorPositions)
				{
					newType.StoreTermVectorPositions = random.NextBoolean();
					if (newType.StoreTermVectorPositions && !newType.StoreTermVectorPayloads && !
						OLD_FORMAT_IMPERSONATION_IS_ACTIVE)
					{
						newType.StoreTermVectorPayloads = random.NextBoolean();
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
		public static CultureInfo RandomLocale(Random random)
		{
			CultureInfo[] locales = CultureInfo.GetCultures(CultureTypes.AllCultures);
			return locales[random.Next(locales.Length)];
		}

		/// <summary>Return a random TimeZone from the available timezones on the system</summary>
		/// <seealso>"https://issues.apache.org/jira/browse/LUCENE-4020"</seealso>
		public static TimeZoneInfo RandomTimeZone(Random random)
		{
			string[] tzIds = TimeZoneInfo.GetAvailableIDs();
			return Extensions.GetTimeZone(tzIds[random.Next(tzIds.Length)]);
		}

		/// <summary>return a Locale object equivalent to its programmatic name</summary>
		public static CultureInfo LocaleForName(string localeName)
		{
			string[] elements = localeName.Split(new []{'_'});
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
			return !Codec.Default.Name.Equals("Lucene3x");
		}
		private static Directory NewFSDirectoryImpl(Type clazz, FileInfo file)
		{
			FSDirectory d = null;
			try
			{
				d = CommandLineUtil.NewFSDirectory(clazz, file.Directory);
			}
			catch (Exception e)
			{
				Thrower.Rethrow(e);
			}
			return d;
		}
		internal static Directory NewDirectoryImpl(Random random, string clazzName)
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
				Thrower.Rethrow(e);
				throw null;
			}
		}
		public static IndexReader MaybeWrapReader(IndexReader r)
		{
			Random random = Random();
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
							// Häckidy-Hick-Hack: a standard MultiReader will cause FC insanity, so we use
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
							Collections.Shuffle(allFields, random);
							int end = allFields.IsEmpty() ? 0 : random.Next(allFields.Count);
							ICollection<string> fields = new HashSet<string>(allFields.SubList(0, end));
							// will create no FC insanity as ParallelAtomicReader has own cache key:
							r = new ParallelAtomicReader(new FieldFilterAtomicReader(ar, fields, false), new 
								FieldFilterAtomicReader(ar, fields, true));
							break;
						}

						case 4:
						{
							// Häckidy-Hick-Hack: a standard Reader will cause FC insanity, so we use
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
		public static IOContext NewIOContext(Random random)
		{
			return NewIOContext(random, IOContext.DEFAULT);
		}
		public static IOContext NewIOContext(Random random, IOContext oldContext)
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
		public static IndexSearcher NewSearcher(IndexReader r)
		{
			return NewSearcher(r, true);
		}
		public static IndexSearcher NewSearcher(IndexReader r, bool maybeWrap)
		{
			return NewSearcher(r, maybeWrap, true);
		}
		public static IndexSearcher NewSearcher(IndexReader r, bool maybeWrap, bool wrapWithAssertions
			)
		{
			Random random = Random();
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
						Thrower.Rethrow(e);
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
						Thrower.Rethrow(e);
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
		public virtual void AssertFieldStatisticsEquals(string info, Fields leftFields, Fields
			 rightFields)
		{
			if (leftFields.Size() != -1 && rightFields.Size() != -1)
			{
				NUnit.Framework.Assert.AreEqual(info, leftFields.Size(), rightFields.Size());
			}
		}
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
			Collections.Shuffle(shuffledTests, random);
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
		public virtual void AssertStoredFieldsEquals(string info, IndexReader leftReader, 
			IndexReader rightReader)
		{
			//HM:revisit 
			//assert leftReader.maxDoc() == rightReader.maxDoc();
			for (int i = 0; i < leftReader.MaxDoc(); i++)
			{
				Lucene.NetDocument.Document leftDoc = leftReader.Document(i);
				Lucene.NetDocument.Document rightDoc = rightReader.Document(i);
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
        public class Disposable<T> : IDisposable
            where T:class
        {
            private T resource;

            public Disposable(T resource)
            {
                this.resource = resource;
            }

            public void Dispose()
            {
                GC.SuppressFinalize(this);
                this.Dispose(true);
            }

            protected void Dispose(bool dispose)
            {
                if(dispose)
                {
                    if(this.resource is IDisposable)
                    {
                        ((IDisposable)this.resource).Dispose();
                    }

                    this.resource = null;
                }
            }

            ~Disposable()
            {
                this.Dispose(false);
            }
        }

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
        public static Disposable<T> CloseAfterSuite<T>(T resource) where T:class
        {
            // maps to random context
            return new Disposable<T>(resource);
        }
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

        /// <summary> Returns a {@link Random} instance for generating random numbers during the test.
        /// The random seed is logged during test execution and printed to System.out on any failure
		private static FilePath tempDirBase;
		private const int TEMP_NAME_RETRY_THRESHOLD = 9999;
        /// for reproducing the test using {@link #NewRandom(long)} with the recorded seed
        /// .
        /// </summary>
        public virtual Random NewRandom()
        {
            if (this.seed != null)
            {
                throw new SystemException("please call LuceneTestCase.newRandom only once per test");
            }
            return NewRandom(seedRnd.Next(Int32.MinValue, Int32.MaxValue));
        }

        /// <summary> Returns a {@link Random} instance for generating random numbers during the test.
        /// If an error occurs in the test that is not reproducible, you can use this method to
        /// initialize the number generator with the seed that was printed out during the failing test.
        /// </summary>
        public virtual Random NewRandom(int seed)
        {
            if (this.seed != null)
            {
                throw new System.SystemException("please call LuceneTestCase.newRandom only once per test");
            }
            this.seed = seed;
            return new System.Random(seed);
        }

        // recorded seed
        [NonSerialized] protected internal int? seed = null;
        //protected internal bool seed_init = false;

        // static members
        [NonSerialized] private static readonly System.Random seedRnd = new System.Random();


        public static int AtLeast(Random random, int minimum)
        {
            int min = (TEST_NIGHTLY ? 2 * minimum : minimum) * RANDOM_MULTIPLIER;
            var max = min + (min / 2);
            return Randomized.Generators.RandomInts.NextIntBetween(random, min, max);
        }

        public static int AtLeast(int minimum)
        {
            return AtLeast(RandomizedContext.Current.Random, minimum);
        }

        /// <summary>
        /// Same as Assert.True, but shorter.
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="message"></param>
        protected static void Ok(bool condition, string message = null)
        {
            if (!string.IsNullOrWhiteSpace(message))
                Assert.True(condition, message);
            else
                Assert.True(condition);
        }
    }
}
