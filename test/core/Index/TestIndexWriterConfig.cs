/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;
using Sharpen.Reflect;

namespace Lucene.Net.Index
{
	public class TestIndexWriterConfig : LuceneTestCase
	{
		private sealed class MySimilarity : DefaultSimilarity
		{
			// Does not implement anything - used only for type checking on IndexWriterConfig.
		}

		private sealed class MyIndexingChain : DocumentsWriterPerThread.IndexingChain
		{
			// Does not implement anything - used only for type checking on IndexWriterConfig.
			internal override DocConsumer GetChain(DocumentsWriterPerThread documentsWriter)
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
			AreEqual(typeof(MockAnalyzer), conf.GetAnalyzer().GetType(
				));
			IsNull(conf.GetIndexCommit());
			AreEqual(typeof(KeepOnlyLastCommitDeletionPolicy), conf.GetIndexDeletionPolicy
				().GetType());
			AreEqual(typeof(ConcurrentMergeScheduler), conf.GetMergeScheduler
				().GetType());
			AreEqual(IndexWriterConfig.OpenMode.CREATE_OR_APPEND, conf
				.GetOpenMode());
			// we don't need to 
			//HM:revisit 
			//assert this, it should be unspecified
			IsTrue(IndexSearcher.GetDefaultSimilarity() == conf.GetSimilarity
				());
			AreEqual(IndexWriterConfig.DEFAULT_TERM_INDEX_INTERVAL, conf
				.GetTermIndexInterval());
			AreEqual(IndexWriterConfig.GetDefaultWriteLockTimeout(), conf
				.GetWriteLockTimeout());
			AreEqual(IndexWriterConfig.WRITE_LOCK_TIMEOUT, IndexWriterConfig
				.GetDefaultWriteLockTimeout());
			AreEqual(IndexWriterConfig.DEFAULT_MAX_BUFFERED_DELETE_TERMS
				, conf.GetMaxBufferedDeleteTerms());
			AreEqual(IndexWriterConfig.DEFAULT_RAM_BUFFER_SIZE_MB, conf
				.GetRAMBufferSizeMB(), 0.0);
			AreEqual(IndexWriterConfig.DEFAULT_MAX_BUFFERED_DOCS, conf
				.GetMaxBufferedDocs());
			AreEqual(IndexWriterConfig.DEFAULT_READER_POOLING, conf.GetReaderPooling
				());
			IsTrue(DocumentsWriterPerThread.defaultIndexingChain == conf
				.GetIndexingChain());
			IsNull(conf.GetMergedSegmentWarmer());
			AreEqual(IndexWriterConfig.DEFAULT_READER_TERMS_INDEX_DIVISOR
				, conf.GetReaderTermsIndexDivisor());
			AreEqual(typeof(TieredMergePolicy), conf.GetMergePolicy().
				GetType());
			AreEqual(typeof(DocumentsWriterPerThreadPool), conf.GetIndexerThreadPool
				().GetType());
			AreEqual(typeof(FlushByRamOrCountsPolicy), conf.GetFlushPolicy
				().GetType());
			AreEqual(IndexWriterConfig.DEFAULT_RAM_PER_THREAD_HARD_LIMIT_MB
				, conf.GetRAMPerThreadHardLimitMB());
			AreEqual(Codec.GetDefault(), conf.Codec);
			AreEqual(InfoStream.GetDefault(), conf.GetInfoStream());
			AreEqual(IndexWriterConfig.DEFAULT_USE_COMPOUND_FILE_SYSTEM
				, conf.GetUseCompoundFile());
			// Sanity check - validate that all getters are covered.
			ICollection<string> getters = new HashSet<string>();
			getters.AddItem("getAnalyzer");
			getters.AddItem("getIndexCommit");
			getters.AddItem("getIndexDeletionPolicy");
			getters.AddItem("getMaxFieldLength");
			getters.AddItem("getMergeScheduler");
			getters.AddItem("getOpenMode");
			getters.AddItem("getSimilarity");
			getters.AddItem("getTermIndexInterval");
			getters.AddItem("getWriteLockTimeout");
			getters.AddItem("getDefaultWriteLockTimeout");
			getters.AddItem("getMaxBufferedDeleteTerms");
			getters.AddItem("getRAMBufferSizeMB");
			getters.AddItem("getMaxBufferedDocs");
			getters.AddItem("getIndexingChain");
			getters.AddItem("getMergedSegmentWarmer");
			getters.AddItem("getMergePolicy");
			getters.AddItem("getMaxThreadStates");
			getters.AddItem("getReaderPooling");
			getters.AddItem("getIndexerThreadPool");
			getters.AddItem("getReaderTermsIndexDivisor");
			getters.AddItem("getFlushPolicy");
			getters.AddItem("getRAMPerThreadHardLimitMB");
			getters.AddItem("getCodec");
			getters.AddItem("getInfoStream");
			getters.AddItem("getUseCompoundFile");
			foreach (MethodInfo m in Sharpen.Runtime.GetDeclaredMethods(typeof(IndexWriterConfig
				)))
			{
				if (m.DeclaringType == typeof(IndexWriterConfig) && m.Name.StartsWith("get"))
				{
					IsTrue("method " + m.Name + " is not tested for defaults", 
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
			foreach (MethodInfo m in Sharpen.Runtime.GetDeclaredMethods(typeof(IndexWriterConfig
				)))
			{
				if (m.Name.StartsWith("set") && !Modifier.IsStatic(m.GetModifiers()))
				{
					allSetters.AddItem(m.Name);
					// setters overridden from LiveIndexWriterConfig are returned twice, once with 
					// IndexWriterConfig return type and second with LiveIndexWriterConfig. The ones
					// from LiveIndexWriterConfig are marked 'synthetic', so just collect them and
					// 
					//HM:revisit 
					//assert in the end that we also received them from IWC.
					if (m.IsSynthetic())
					{
						liveSetters.AddItem(m.Name);
					}
					else
					{
						AreEqual("method " + m.Name + " does not return IndexWriterConfig"
							, typeof(IndexWriterConfig), m.ReturnType);
					}
				}
			}
			foreach (string setter in liveSetters)
			{
				IsTrue("setter method not overridden by IndexWriterConfig: "
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
			catch (SetOnce.AlreadySetException)
			{
			}
			// expected
			// also cloning it won't help, after it has been used already
			try
			{
				IsNotNull(new RandomIndexWriter(Random(), dir, conf.Clone(
					)));
				Fail("should have hit AlreadySetException");
			}
			catch (SetOnce.AlreadySetException)
			{
			}
			// expected
			// if it's cloned in advance, it should be ok
			conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
			new RandomIndexWriter(Random(), dir, conf.Clone()).Close();
			new RandomIndexWriter(Random(), dir, conf.Clone()).Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestOverrideGetters()
		{
			// Test that IndexWriterConfig overrides all getters, so that javadocs
			// contain all methods for the users. Also, ensures that IndexWriterConfig
			// doesn't declare getters that are not declared on LiveIWC.
			HashSet<string> liveGetters = new HashSet<string>();
			foreach (MethodInfo m in Sharpen.Runtime.GetDeclaredMethods(typeof(LiveIndexWriterConfig
				)))
			{
				if (m.Name.StartsWith("get") && !Modifier.IsStatic(m.GetModifiers()))
				{
					liveGetters.AddItem(m.Name);
				}
			}
			foreach (MethodInfo m_1 in Sharpen.Runtime.GetDeclaredMethods(typeof(IndexWriterConfig
				)))
			{
				if (m_1.Name.StartsWith("get") && !Modifier.IsStatic(m_1.GetModifiers()))
				{
					AreEqual("method " + m_1.Name + " not overrided by IndexWriterConfig"
						, typeof(IndexWriterConfig), m_1.DeclaringType);
					IsTrue("method " + m_1.Name + " not declared on LiveIndexWriterConfig"
						, liveGetters.Contains(m_1.Name));
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestConstants()
		{
			// Tests that the values of the constants does not change
			AreEqual(1000, IndexWriterConfig.WRITE_LOCK_TIMEOUT);
			AreEqual(32, IndexWriterConfig.DEFAULT_TERM_INDEX_INTERVAL
				);
			AreEqual(-1, IndexWriterConfig.DISABLE_AUTO_FLUSH);
			AreEqual(IndexWriterConfig.DISABLE_AUTO_FLUSH, IndexWriterConfig
				.DEFAULT_MAX_BUFFERED_DELETE_TERMS);
			AreEqual(IndexWriterConfig.DISABLE_AUTO_FLUSH, IndexWriterConfig
				.DEFAULT_MAX_BUFFERED_DOCS);
			AreEqual(16.0, IndexWriterConfig.DEFAULT_RAM_BUFFER_SIZE_MB
				, 0.0);
			AreEqual(false, IndexWriterConfig.DEFAULT_READER_POOLING);
			AreEqual(true, IndexWriterConfig.DEFAULT_USE_COMPOUND_FILE_SYSTEM
				);
			AreEqual(DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, IndexWriterConfig
				.DEFAULT_READER_TERMS_INDEX_DIVISOR);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestToString()
		{
			string str = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(
				))).ToString();
			foreach (FieldInfo f in Sharpen.Runtime.GetDeclaredFields(typeof(IndexWriterConfig
				)))
			{
				int modifiers = f.GetModifiers();
				if (Modifier.IsStatic(modifiers) && Modifier.IsFinal(modifiers))
				{
					// Skip static final fields, they are only constants
					continue;
				}
				else
				{
					if ("indexingChain".Equals(f.Name))
					{
						// indexingChain is a package-private setting and thus is not output by
						// toString.
						continue;
					}
				}
				if (f.Name.Equals("inUseByIndexWriter"))
				{
					continue;
				}
				IsTrue(f.Name + " not found in toString", str.IndexOf(f.Name
					) != -1);
			}
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestClone()
		{
			IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			IndexWriterConfig clone = conf.Clone();
			// Make sure parameters that can't be reused are cloned
			IndexDeletionPolicy delPolicy = conf.delPolicy;
			IndexDeletionPolicy delPolicyClone = clone.delPolicy;
			IsTrue(delPolicy.GetType() == delPolicyClone.GetType() && 
				(delPolicy != delPolicyClone || delPolicy.Clone() == delPolicyClone.Clone()));
			FlushPolicy flushPolicy = conf.flushPolicy;
			FlushPolicy flushPolicyClone = clone.flushPolicy;
			IsTrue(flushPolicy.GetType() == flushPolicyClone.GetType()
				 && (flushPolicy != flushPolicyClone || flushPolicy.Clone() == flushPolicyClone.
				Clone()));
			DocumentsWriterPerThreadPool pool = conf.indexerThreadPool;
			DocumentsWriterPerThreadPool poolClone = clone.indexerThreadPool;
			IsTrue(pool.GetType() == poolClone.GetType() && (pool != poolClone
				 || pool.Clone() == poolClone.Clone()));
			MergePolicy mergePolicy = conf.mergePolicy;
			MergePolicy mergePolicyClone = clone.mergePolicy;
			IsTrue(mergePolicy.GetType() == mergePolicyClone.GetType()
				 && (mergePolicy != mergePolicyClone || mergePolicy.Clone() == mergePolicyClone.
				Clone()));
			MergeScheduler mergeSched = conf.mergeScheduler;
			MergeScheduler mergeSchedClone = clone.mergeScheduler;
			IsTrue(mergeSched.GetType() == mergeSchedClone.GetType() &&
				 (mergeSched != mergeSchedClone || mergeSched.Clone() == mergeSchedClone.Clone()
				));
			conf.SetMergeScheduler(new SerialMergeScheduler());
			AreEqual(typeof(ConcurrentMergeScheduler), clone.GetMergeScheduler
				().GetType());
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.Test]
		public virtual void TestInvalidValues()
		{
			IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			// Test IndexDeletionPolicy
			AreEqual(typeof(KeepOnlyLastCommitDeletionPolicy), conf.GetIndexDeletionPolicy
				().GetType());
			conf.SetIndexDeletionPolicy(new SnapshotDeletionPolicy(null));
			AreEqual(typeof(SnapshotDeletionPolicy), conf.GetIndexDeletionPolicy
				().GetType());
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
			AreEqual(typeof(ConcurrentMergeScheduler), conf.GetMergeScheduler
				().GetType());
			conf.SetMergeScheduler(new SerialMergeScheduler());
			AreEqual(typeof(SerialMergeScheduler), conf.GetMergeScheduler
				().GetType());
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
			//HM:revisit 
			//assert what the default is, just that its not null.
			IsTrue(IndexSearcher.GetDefaultSimilarity() == conf.GetSimilarity
				());
			conf.SetSimilarity(new TestIndexWriterConfig.MySimilarity());
			AreEqual(typeof(TestIndexWriterConfig.MySimilarity), conf.
				GetSimilarity().GetType());
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
			IsTrue(DocumentsWriterPerThread.defaultIndexingChain == conf
				.GetIndexingChain());
			conf.SetIndexingChain(new TestIndexWriterConfig.MyIndexingChain());
			AreEqual(typeof(TestIndexWriterConfig.MyIndexingChain), conf
				.GetIndexingChain().GetType());
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
			AreEqual(typeof(TieredMergePolicy), conf.GetMergePolicy().
				GetType());
			conf.SetMergePolicy(new LogDocMergePolicy());
			AreEqual(typeof(LogDocMergePolicy), conf.GetMergePolicy().
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
		/// <exception cref="System.Exception"></exception>
		public virtual void TestLiveChangeToCFS()
		{
			Directory dir = NewDirectory();
			IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwc.SetMergePolicy(NewLogMergePolicy(true));
			// Start false:
			iwc.SetUseCompoundFile(false);
			iwc.GetMergePolicy().SetNoCFSRatio(0.0d);
			IndexWriter w = new IndexWriter(dir, iwc);
			// Change to true:
			w.GetConfig().SetUseCompoundFile(true);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("field", "foo", Field.Store.NO));
			w.AddDocument(doc);
			w.Commit();
			IsTrue("Expected CFS after commit", w.NewestSegment().info
				.GetUseCompoundFile());
			doc.Add(NewStringField("field", "foo", Field.Store.NO));
			w.AddDocument(doc);
			w.Commit();
			w.ForceMerge(1);
			w.Commit();
			// no compound files after merge
			IsFalse("Expected Non-CFS after merge", w.NewestSegment().
				info.GetUseCompoundFile());
			MergePolicy lmp = w.GetConfig().GetMergePolicy();
			lmp.SetNoCFSRatio(1.0);
			lmp.SetMaxCFSSegmentSizeMB(double.PositiveInfinity);
			w.AddDocument(doc);
			w.ForceMerge(1);
			w.Commit();
			IsTrue("Expected CFS after merge", w.NewestSegment().info.
				GetUseCompoundFile());
			w.Close();
			dir.Close();
		}
	}
}
