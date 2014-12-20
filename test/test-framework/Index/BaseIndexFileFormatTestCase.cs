/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;

namespace Lucene.Net.TestFramework.Index
{
	/// <summary>Common tests to all index formats.</summary>
	/// <remarks>Common tests to all index formats.</remarks>
	public abstract class BaseIndexFileFormatTestCase : LuceneTestCase
	{
		/// <summary>Returns the codec to run tests against</summary>
		protected internal abstract Codec GetCodec();

		private Codec savedCodec;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			// set the default codec, so adding test cases to this isn't fragile
			savedCodec = Codec.Default;
			Codec.Default = GetCodec();
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			Codec.Default = savedCodec;
			// restore
			base.TearDown();
		}

		/// <summary>Add random fields to the provided document.</summary>
		
		protected internal abstract void AddRandomFields(Document doc);

		/// <exception cref="System.IO.IOException"></exception>
		private IDictionary<string, long> BytesUsedByExtension(Directory d)
		{
			IDictionary<string, long> bytesUsedByExtension = new Dictionary<string, long>();
			foreach (string file in d.ListAll())
			{
				string ext = IndexFileNames.GetExtension(file);
				long previousLength = bytesUsedByExtension.ContainsKey(ext) ? bytesUsedByExtension[ext] : 0;
				bytesUsedByExtension[ext] = previousLength + d.FileLength(file);
			}
			bytesUsedByExtension.Keys.Clear();
			return bytesUsedByExtension;
		}

		/// <summary>
		/// Return the list of extensions that should be excluded from byte counts when
		/// comparing indices that store the same content.
		/// </summary>
		
		protected internal virtual ICollection<string> ExcludedExtensionsFromByteCounts()
		{
			return new HashSet<string>(Arrays.AsList(new string[] { "si", "lock" }));
		}

		// segment infos store various pieces of information that don't solely depend
		// on the content of the index in the diagnostics (such as a timestamp) so we
		// exclude this file from the bytes counts
		// lock files are 0 bytes (one directory in the test could be RAMDir, the other FSDir)
		/// <summary>The purpose of this test is to make sure that bulk merge doesn't accumulate useless data over runs.
		/// 	</summary>
		/// <remarks>The purpose of this test is to make sure that bulk merge doesn't accumulate useless data over runs.
		/// 	</remarks>
		/// <exception cref="System.Exception"></exception>
		public virtual void TestMergeStability()
		{
			Directory dir = NewDirectory();
			// do not use newMergePolicy that might return a MockMergePolicy that ignores the no-CFS ratio
			// do not use RIW which will change things up!
			MergePolicy mp = NewTieredMergePolicy();
			mp.SetNoCFSRatio(0);
			var cfg = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())) {UseCompoundFile = false};
		    cfg.SetMergePolicy(mp);
			var w = new IndexWriter(dir, cfg);
			int numDocs = AtLeast(500);
			for (int i = 0; i < numDocs; ++i)
			{
				Document d = new Document();
				AddRandomFields(d);
				w.AddDocument(d);
			}
			w.ForceMerge(1);
			w.Commit();
			w.Dispose();
			IndexReader reader = DirectoryReader.Open(dir);
			Directory dir2 = NewDirectory();
			mp = NewTieredMergePolicy();
			mp.SetNoCFSRatio(0);
            //This may not be needed since cfg was already initialized
            //cfg = ((IndexWriterConfig)new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
            //    (Random())).SetUseCompoundFile(false)).SetMergePolicy(mp);
			w = new IndexWriter(dir2, cfg);
			w.AddIndexes(reader);
			w.Commit();
			w.Dispose();
			AreEqual(BytesUsedByExtension(dir), BytesUsedByExtension(dir2));
			reader.Dispose();
			dir.Dispose();
			dir2.Dispose();
		}
	}
}
