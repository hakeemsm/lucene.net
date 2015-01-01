using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Store;
using Lucene.Net.TestFramework;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.Util;
using NUnit.Framework;
using FieldInfo = System.Reflection.FieldInfo;

namespace Lucene.Net.Test.Index
{
    [TestFixture]
	public class TestIndexWriterConfig : LuceneTestCase
	{
		private sealed class MySimilarity : DefaultSimilarity
		{
			// Does not implement anything - used only for type checking on IndexWriterConfig.
		}

		private sealed class MyIndexingChain : DocumentsWriterPerThread.IndexingChain
		{
			// Does not implement anything - used only for type checking on IndexWriterConfig.
		    public override DocConsumer GetChain(DocumentsWriterPerThread documentsWriter)
			{
				return null;
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestDefaults()
		{
			IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			AssertEquals(typeof(MockAnalyzer), conf.Analyzer.GetType());
			IsNull(conf.IndexCommit);
			AssertEquals(typeof(KeepOnlyLastCommitDeletionPolicy), conf.IndexDeletionPolicy.GetType());
			AssertEquals(typeof(ConcurrentMergeScheduler), conf.MergeScheduler.GetType());
			AssertEquals(IndexWriterConfig.OpenMode.CREATE_OR_APPEND, conf.OpenModeValue);
			// we don't need to 
			//HM:revisit 
			//assert this, it should be unspecified
			AssertTrue(IndexSearcher.DefaultSimilarity == conf.Similarity);
			AssertEquals(IndexWriterConfig.DEFAULT_TERM_INDEX_INTERVAL, conf.TermIndexInterval);
			AssertEquals(IndexWriterConfig.DefaultWriteLockTimeout, conf.WriteLockTimeout);
			AssertEquals(IndexWriterConfig.WRITE_LOCK_TIMEOUT, IndexWriterConfig.DefaultWriteLockTimeout);
			AssertEquals(IndexWriterConfig.DEFAULT_MAX_BUFFERED_DELETE_TERMS, conf.MaxBufferedDeleteTerms);
			AssertEquals(IndexWriterConfig.DEFAULT_RAM_BUFFER_SIZE_MB, conf.RAMBufferSizeMB, 0.0);
			AssertEquals(IndexWriterConfig.DEFAULT_MAX_BUFFERED_DOCS, conf.MaxBufferedDocs);
			AssertEquals(IndexWriterConfig.DEFAULT_READER_POOLING, conf.ReaderPooling);
			AssertTrue(DocumentsWriterPerThread.defaultIndexingChain == conf.IndexingChain);
			IsNull(conf.MergedSegmentWarmer);
			AssertEquals(IndexWriterConfig.DEFAULT_READER_TERMS_INDEX_DIVISOR
				, conf.ReaderTermsIndexDivisor);
			AssertEquals(typeof(TieredMergePolicy), conf.MergePolicy.
				GetType());
			AssertEquals(typeof(DocumentsWriterPerThreadPool), conf.IndexerThreadPool.GetType());
			AssertEquals(typeof(FlushByRamOrCountsPolicy), conf.FlushPolicy.GetType());
			AssertEquals(IndexWriterConfig.DEFAULT_RAM_PER_THREAD_HARD_LIMIT_MB, conf.RAMPerThreadHardLimitMB);
			AssertEquals(Codec.Default, conf.Codec);
			AssertEquals(InfoStream.Default, conf.InfoStream);
			AssertEquals(IndexWriterConfig.DEFAULT_USE_COMPOUND_FILE_SYSTEM
				, conf.UseCompoundFile);
			// Sanity check - validate that all getters are covered.
			ICollection<string> getters = new HashSet<string>();
			getters.Add("getAnalyzer");
			getters.Add("getIndexCommit");
			getters.Add("getIndexDeletionPolicy");
			getters.Add("getMaxFieldLength");
			getters.Add("getMergeScheduler");
			getters.Add("getOpenMode");
			getters.Add("getSimilarity");
			getters.Add("getTermIndexInterval");
			getters.Add("getWriteLockTimeout");
			getters.Add("getDefaultWriteLockTimeout");
			getters.Add("getMaxBufferedDeleteTerms");
			getters.Add("getRAMBufferSizeMB");
			getters.Add("getMaxBufferedDocs");
			getters.Add("getIndexingChain");
			getters.Add("getMergedSegmentWarmer");
			getters.Add("getMergePolicy");
			getters.Add("getMaxThreadStates");
			getters.Add("getReaderPooling");
			getters.Add("getIndexerThreadPool");
			getters.Add("getReaderTermsIndexDivisor");
			getters.Add("getFlushPolicy");
			getters.Add("getRAMPerThreadHardLimitMB");
			getters.Add("getCodec");
			getters.Add("getInfoStream");
			getters.Add("getUseCompoundFile");
			foreach (MethodInfo m in typeof(IndexWriterConfig).GetMethods())
			{
				if (m.DeclaringType == typeof(IndexWriterConfig) && m.Name.StartsWith("get"))
				{
					AssertTrue("method " + m.Name + " is not tested for defaults", 
						getters.Contains(m.Name));
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestSettersChaining()
		{
			// Ensures that every setter returns IndexWriterConfig to allow chaining.
			HashSet<string> liveSetters = new HashSet<string>();
			HashSet<string> allSetters = new HashSet<string>();
			foreach (var m in typeof(IndexWriterConfig).GetProperties(BindingFlags.SetProperty & BindingFlags.Instance))
			{
                
				
					allSetters.Add(m.Name);
					// setters overridden from LiveIndexWriterConfig are returned twice, once with 
					// IndexWriterConfig return type and second with LiveIndexWriterConfig. The ones
					// from LiveIndexWriterConfig are marked 'synthetic', so just collect them and
					// 
					
					//assert in the end that we also received them from IWC.
                //.NET Port. No synthetic methods. Setters are returned from the call above
					
						AssertEquals("method " + m.Name + " does not return IndexWriterConfig"
							, typeof(IndexWriterConfig), m.PropertyType);
					
				
			}
			foreach (string setter in liveSetters)
			{
				AssertTrue("setter method not overridden by IndexWriterConfig: "
					 + setter, allSetters.Contains(setter));
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestReuse()
		{
			Directory dir = NewDirectory();
			// test that IWC cannot be reused across two IWs
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
			new RandomIndexWriter(Random(), dir, conf).Close();
			// this should fail
			try
			{
				IsNotNull(new RandomIndexWriter(Random(), dir, conf));
				Fail("should have hit AlreadySetException");
			}
			catch (SetOnce<ArgumentException>.AlreadySetException)
			{
			}
			// expected
			// also cloning it won't help, after it has been used already
			try
			{
				IsNotNull(new RandomIndexWriter(Random(), dir, (IndexWriterConfig) conf.Clone()));
				Fail("should have hit AlreadySetException");
			}
			catch (SetOnce<ArgumentException>.AlreadySetException)
			{
			}
			// expected
			// if it's cloned in advance, it should be ok
			conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
			new RandomIndexWriter(Random(), dir, (IndexWriterConfig) conf.Clone()).Close();
			new RandomIndexWriter(Random(), dir, (IndexWriterConfig) conf.Clone()).Close();
			dir.Dispose();
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestOverrideGetters()
		{
			// Test that IndexWriterConfig overrides all getters, so that javadocs
			// contain all methods for the users. Also, ensures that IndexWriterConfig
			// doesn't declare getters that are not declared on LiveIWC.
			HashSet<string> liveGetters = new HashSet<string>();
			foreach (var m in typeof(LiveIndexWriterConfig).GetProperties(BindingFlags.GetProperty & BindingFlags.Instance))
			{
                liveGetters.Add(m.Name);
			}
			foreach (var m in typeof(IndexWriterConfig).GetProperties(BindingFlags.GetProperty & BindingFlags.Instance))
			{
					AssertEquals("method " + m.Name + " not overrided by IndexWriterConfig"
						, typeof(IndexWriterConfig), m.DeclaringType);
					AssertTrue("method " + m.Name + " not declared on LiveIndexWriterConfig"
						, liveGetters.Contains(m.Name));
				
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestConstants()
		{
			// Tests that the values of the constants does not change
			AssertEquals(1000, IndexWriterConfig.WRITE_LOCK_TIMEOUT);
			AssertEquals(32, IndexWriterConfig.DEFAULT_TERM_INDEX_INTERVAL
				);
			AssertEquals(-1, IndexWriterConfig.DISABLE_AUTO_FLUSH);
			AssertEquals(IndexWriterConfig.DISABLE_AUTO_FLUSH, IndexWriterConfig
				.DEFAULT_MAX_BUFFERED_DELETE_TERMS);
			AssertEquals(IndexWriterConfig.DISABLE_AUTO_FLUSH, IndexWriterConfig
				.DEFAULT_MAX_BUFFERED_DOCS);
			AssertEquals(16.0, IndexWriterConfig.DEFAULT_RAM_BUFFER_SIZE_MB
				, 0.0);
			AssertEquals(false, IndexWriterConfig.DEFAULT_READER_POOLING);
			AssertEquals(true, IndexWriterConfig.DEFAULT_USE_COMPOUND_FILE_SYSTEM
				);
			AssertEquals(DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, IndexWriterConfig
				.DEFAULT_READER_TERMS_INDEX_DIVISOR);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestToString()
		{
			string str = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(
				))).ToString();
			foreach (FieldInfo f in typeof(IndexWriterConfig).GetFields(BindingFlags.Instance))
			{
			    if ("indexingChain".Equals(f.Name))
			    {
			        // indexingChain is a package-private setting and thus is not output by
			        // toString.
			        continue;
			    }
			    if (f.Name.Equals("inUseByIndexWriter"))
				{
					continue;
				}
				AssertTrue(f.Name + " not found in toString", str.IndexOf(f.Name
					) != -1);
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestClone()
		{
			IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriterConfig clone = (IndexWriterConfig) conf.Clone();
			// Make sure parameters that can't be reused are cloned
			IndexDeletionPolicy delPolicy = conf.delPolicy;
			IndexDeletionPolicy delPolicyClone = clone.delPolicy;
			AssertTrue(delPolicy.GetType() == delPolicyClone.GetType() && 
				(delPolicy != delPolicyClone || delPolicy.Clone() == delPolicyClone.Clone()));
			FlushPolicy flushPolicy = conf.flushPolicy;
			FlushPolicy flushPolicyClone = clone.flushPolicy;
			AssertTrue(flushPolicy.GetType() == flushPolicyClone.GetType()
				 && (flushPolicy != flushPolicyClone || flushPolicy.Clone() == flushPolicyClone.
				Clone()));
			DocumentsWriterPerThreadPool pool = conf.indexerThreadPool;
			DocumentsWriterPerThreadPool poolClone = clone.indexerThreadPool;
			AssertTrue(pool.GetType() == poolClone.GetType() && (pool != poolClone
				 || pool.Clone() == poolClone.Clone()));
			MergePolicy mergePolicy = conf.mergePolicy;
			MergePolicy mergePolicyClone = clone.mergePolicy;
			AssertTrue(mergePolicy.GetType() == mergePolicyClone.GetType()
				 && (mergePolicy != mergePolicyClone || mergePolicy.Clone() == mergePolicyClone.
				Clone()));
			MergeScheduler mergeSched = conf.mergeScheduler;
			MergeScheduler mergeSchedClone = clone.mergeScheduler;
			AssertTrue(mergeSched.GetType() == mergeSchedClone.GetType() &&
				 (mergeSched != mergeSchedClone || mergeSched.Clone() == mergeSchedClone.Clone()
				));
			conf.SetMergeScheduler(new SerialMergeScheduler());
			AssertEquals(typeof(ConcurrentMergeScheduler), clone.MergeScheduler.GetType());
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestInvalidValues()
		{
			IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			// Test IndexDeletionPolicy
			AssertEquals(typeof(KeepOnlyLastCommitDeletionPolicy), conf.IndexDeletionPolicy.GetType());
			conf.SetIndexDeletionPolicy(new SnapshotDeletionPolicy(null));
			AssertEquals(typeof(SnapshotDeletionPolicy), conf.IndexDeletionPolicy.GetType());
			try
			{
				conf.SetIndexDeletionPolicy(null);
				Fail();
			}
			catch (ArgumentException)
			{
			}
			// ok
			// Test MergeScheduler
			AssertEquals(typeof(ConcurrentMergeScheduler), conf.MergeScheduler.GetType());
			conf.SetMergeScheduler(new SerialMergeScheduler());
			AssertEquals(typeof(SerialMergeScheduler), conf.MergeScheduler.GetType());
			try
			{
				conf.SetMergeScheduler(null);
				Fail();
			}
			catch (ArgumentException)
			{
			}
			// ok
			// Test Similarity: 
			// we shouldnt 
			
			//assert what the default is, just that its not null.
			AssertTrue(IndexSearcher.DefaultSimilarity == conf.Similarity);
			conf.SetSimilarity(new TestIndexWriterConfig.MySimilarity());
			AssertEquals(typeof(TestIndexWriterConfig.MySimilarity), conf.Similarity.GetType());
			try
			{
				conf.SetSimilarity(null);
				Fail();
			}
			catch (ArgumentException)
			{
			}
			// ok
			// Test IndexingChain
			AssertTrue(DocumentsWriterPerThread.defaultIndexingChain == conf.IndexingChain);
			conf.SetIndexingChain(new TestIndexWriterConfig.MyIndexingChain());
			AssertEquals(typeof(TestIndexWriterConfig.MyIndexingChain), conf.IndexingChain.GetType());
			try
			{
				conf.SetIndexingChain(null);
				Fail();
			}
			catch (ArgumentException)
			{
			}
			// ok
			try
			{
				conf.SetMaxBufferedDeleteTerms(0);
				Fail("should not have succeeded to set maxBufferedDeleteTerms to 0"
					);
			}
			catch (ArgumentException)
			{
			}
			// this is expected
			try
			{
				conf.SetMaxBufferedDocs(1);
				Fail("should not have succeeded to set maxBufferedDocs to 1"
					);
			}
			catch (ArgumentException)
			{
			}
			// this is expected
			try
			{
				// Disable both MAX_BUF_DOCS and RAM_SIZE_MB
				conf.SetMaxBufferedDocs(4);
				conf.SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
				conf.SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH);
				Fail("should not have succeeded to disable maxBufferedDocs when ramBufferSizeMB is disabled as well"
					);
			}
			catch (ArgumentException)
			{
			}
			// this is expected
			conf.SetRAMBufferSizeMB(IndexWriterConfig.DEFAULT_RAM_BUFFER_SIZE_MB);
			conf.SetMaxBufferedDocs(IndexWriterConfig.DEFAULT_MAX_BUFFERED_DOCS);
			try
			{
				conf.SetRAMBufferSizeMB(IndexWriterConfig.DISABLE_AUTO_FLUSH);
				Fail("should not have succeeded to disable ramBufferSizeMB when maxBufferedDocs is disabled as well"
					);
			}
			catch (ArgumentException)
			{
			}
			// this is expected
			// Test setReaderTermsIndexDivisor
			try
			{
				conf.SetReaderTermsIndexDivisor(0);
				Fail("should not have succeeded to set termsIndexDivisor to 0"
					);
			}
			catch (ArgumentException)
			{
			}
			// this is expected
			// Setting to -1 is ok
			conf.SetReaderTermsIndexDivisor(-1);
			try
			{
				conf.SetReaderTermsIndexDivisor(-2);
				Fail("should not have succeeded to set termsIndexDivisor to < -1"
					);
			}
			catch (ArgumentException)
			{
			}
			// this is expected
			try
			{
				conf.SetRAMPerThreadHardLimitMB(2048);
				Fail("should not have succeeded to set RAMPerThreadHardLimitMB to >= 2048"
					);
			}
			catch (ArgumentException)
			{
			}
			// this is expected
			try
			{
				conf.SetRAMPerThreadHardLimitMB(0);
				Fail("should not have succeeded to set RAMPerThreadHardLimitMB to 0"
					);
			}
			catch (ArgumentException)
			{
			}
			// this is expected
			// Test MergePolicy
			AssertEquals(typeof(TieredMergePolicy), conf.MergePolicy.
				GetType());
			conf.SetMergePolicy(new LogDocMergePolicy());
			AssertEquals(typeof(LogDocMergePolicy), conf.MergePolicy.
				GetType());
			try
			{
				conf.SetMergePolicy(null);
				Fail();
			}
			catch (ArgumentException)
			{
			}
		}

		// ok
		[Test]
		public virtual void TestLiveChangeToCFS()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwc.SetMergePolicy(NewLogMergePolicy(true));
			// Start false:
			iwc.UseCompoundFile = (false);
			iwc.MergePolicy.SetNoCFSRatio(0.0d);
			IndexWriter w = new IndexWriter(dir, iwc);
			// Change to true:
			w.Config.UseCompoundFile = (true);
			var doc = new Lucene.Net.Documents.Document
			{
			    NewStringField("field", "foo", Field.Store.NO)
			};
		    w.AddDocument(doc);
			w.Commit();
			AssertTrue("Expected CFS after commit", w.NewestSegment.info.UseCompoundFile);
			doc.Add(NewStringField("field", "foo", Field.Store.NO));
			w.AddDocument(doc);
			w.Commit();
			w.ForceMerge(1);
			w.Commit();
			// no compound files after merge
			AssertFalse("Expected Non-CFS after merge", w.NewestSegment.info.UseCompoundFile);
			MergePolicy lmp = w.Config.MergePolicy;
			lmp.SetNoCFSRatio(1.0);
			lmp.SetMaxCFSSegmentSizeMB(double.PositiveInfinity);
			w.AddDocument(doc);
			w.ForceMerge(1);
			w.Commit();
			AssertTrue("Expected CFS after merge", w.NewestSegment.info.UseCompoundFile);
			w.Dispose();
			dir.Dispose();
		}
	}
}
