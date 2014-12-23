/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestSizeBoundedForceMerge : LuceneTestCase
	{
		/// <exception cref="System.IO.IOException"></exception>
		private void AddDocs(IndexWriter writer, int numDocs)
		{
			AddDocs(writer, numDocs, false);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AddDocs(IndexWriter writer, int numDocs, bool withID)
		{
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
					();
				if (withID)
				{
					doc.Add(new StringField("id", string.Empty + i, Field.Store.NO));
				}
				writer.AddDocument(doc);
			}
			writer.Commit();
		}

		private static IndexWriterConfig NewWriterConfig()
		{
			IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, null);
			conf.SetMaxBufferedDocs(IndexWriterConfig.DISABLE_AUTO_FLUSH);
			conf.SetRAMBufferSizeMB(IndexWriterConfig.DEFAULT_RAM_BUFFER_SIZE_MB);
			// prevent any merges by default.
			conf.SetMergePolicy(NoMergePolicy.COMPOUND_FILES);
			return conf;
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestByteSizeLimit()
		{
			// tests that the max merge size constraint is applied during forceMerge.
			Directory dir = new RAMDirectory();
			// Prepare an index w/ several small segments and a large one.
			IndexWriterConfig conf = NewWriterConfig();
			IndexWriter writer = new IndexWriter(dir, conf);
			int numSegments = 15;
			for (int i = 0; i < numSegments; i++)
			{
				int numDocs = i == 7 ? 30 : 1;
				AddDocs(writer, numDocs);
			}
			writer.Close();
			SegmentInfos sis = new SegmentInfos();
			sis.Read(dir);
			double min = sis.Info(0).SizeInBytes();
			conf = NewWriterConfig();
			LogByteSizeMergePolicy lmp = new LogByteSizeMergePolicy();
			lmp.SetMaxMergeMBForForcedMerge((min + 1) / (1 << 20));
			conf.SetMergePolicy(lmp);
			writer = new IndexWriter(dir, conf);
			writer.ForceMerge(1);
			writer.Close();
			// Should only be 3 segments in the index, because one of them exceeds the size limit
			sis = new SegmentInfos();
			sis.Read(dir);
			NUnit.Framework.Assert.AreEqual(3, sis.Size());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNumDocsLimit()
		{
			// tests that the max merge docs constraint is applied during forceMerge.
			Directory dir = new RAMDirectory();
			// Prepare an index w/ several small segments and a large one.
			IndexWriterConfig conf = NewWriterConfig();
			IndexWriter writer = new IndexWriter(dir, conf);
			AddDocs(writer, 3);
			AddDocs(writer, 3);
			AddDocs(writer, 5);
			AddDocs(writer, 3);
			AddDocs(writer, 3);
			AddDocs(writer, 3);
			AddDocs(writer, 3);
			writer.Close();
			conf = NewWriterConfig();
			LogMergePolicy lmp = new LogDocMergePolicy();
			lmp.SetMaxMergeDocs(3);
			conf.SetMergePolicy(lmp);
			writer = new IndexWriter(dir, conf);
			writer.ForceMerge(1);
			writer.Close();
			// Should only be 3 segments in the index, because one of them exceeds the size limit
			SegmentInfos sis = new SegmentInfos();
			sis.Read(dir);
			NUnit.Framework.Assert.AreEqual(3, sis.Size());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestLastSegmentTooLarge()
		{
			Directory dir = new RAMDirectory();
			IndexWriterConfig conf = NewWriterConfig();
			IndexWriter writer = new IndexWriter(dir, conf);
			AddDocs(writer, 3);
			AddDocs(writer, 3);
			AddDocs(writer, 3);
			AddDocs(writer, 5);
			writer.Close();
			conf = NewWriterConfig();
			LogMergePolicy lmp = new LogDocMergePolicy();
			lmp.SetMaxMergeDocs(3);
			conf.SetMergePolicy(lmp);
			writer = new IndexWriter(dir, conf);
			writer.ForceMerge(1);
			writer.Close();
			SegmentInfos sis = new SegmentInfos();
			sis.Read(dir);
			NUnit.Framework.Assert.AreEqual(2, sis.Size());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestFirstSegmentTooLarge()
		{
			Directory dir = new RAMDirectory();
			IndexWriterConfig conf = NewWriterConfig();
			IndexWriter writer = new IndexWriter(dir, conf);
			AddDocs(writer, 5);
			AddDocs(writer, 3);
			AddDocs(writer, 3);
			AddDocs(writer, 3);
			writer.Close();
			conf = NewWriterConfig();
			LogMergePolicy lmp = new LogDocMergePolicy();
			lmp.SetMaxMergeDocs(3);
			conf.SetMergePolicy(lmp);
			writer = new IndexWriter(dir, conf);
			writer.ForceMerge(1);
			writer.Close();
			SegmentInfos sis = new SegmentInfos();
			sis.Read(dir);
			NUnit.Framework.Assert.AreEqual(2, sis.Size());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestAllSegmentsSmall()
		{
			Directory dir = new RAMDirectory();
			IndexWriterConfig conf = NewWriterConfig();
			IndexWriter writer = new IndexWriter(dir, conf);
			AddDocs(writer, 3);
			AddDocs(writer, 3);
			AddDocs(writer, 3);
			AddDocs(writer, 3);
			writer.Close();
			conf = NewWriterConfig();
			LogMergePolicy lmp = new LogDocMergePolicy();
			lmp.SetMaxMergeDocs(3);
			conf.SetMergePolicy(lmp);
			writer = new IndexWriter(dir, conf);
			writer.ForceMerge(1);
			writer.Close();
			SegmentInfos sis = new SegmentInfos();
			sis.Read(dir);
			NUnit.Framework.Assert.AreEqual(1, sis.Size());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestAllSegmentsLarge()
		{
			Directory dir = new RAMDirectory();
			IndexWriterConfig conf = NewWriterConfig();
			IndexWriter writer = new IndexWriter(dir, conf);
			AddDocs(writer, 3);
			AddDocs(writer, 3);
			AddDocs(writer, 3);
			writer.Close();
			conf = NewWriterConfig();
			LogMergePolicy lmp = new LogDocMergePolicy();
			lmp.SetMaxMergeDocs(2);
			conf.SetMergePolicy(lmp);
			writer = new IndexWriter(dir, conf);
			writer.ForceMerge(1);
			writer.Close();
			SegmentInfos sis = new SegmentInfos();
			sis.Read(dir);
			NUnit.Framework.Assert.AreEqual(3, sis.Size());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestOneLargeOneSmall()
		{
			Directory dir = new RAMDirectory();
			IndexWriterConfig conf = NewWriterConfig();
			IndexWriter writer = new IndexWriter(dir, conf);
			AddDocs(writer, 3);
			AddDocs(writer, 5);
			AddDocs(writer, 3);
			AddDocs(writer, 5);
			writer.Close();
			conf = NewWriterConfig();
			LogMergePolicy lmp = new LogDocMergePolicy();
			lmp.SetMaxMergeDocs(3);
			conf.SetMergePolicy(lmp);
			writer = new IndexWriter(dir, conf);
			writer.ForceMerge(1);
			writer.Close();
			SegmentInfos sis = new SegmentInfos();
			sis.Read(dir);
			NUnit.Framework.Assert.AreEqual(4, sis.Size());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMergeFactor()
		{
			Directory dir = new RAMDirectory();
			IndexWriterConfig conf = NewWriterConfig();
			IndexWriter writer = new IndexWriter(dir, conf);
			AddDocs(writer, 3);
			AddDocs(writer, 3);
			AddDocs(writer, 3);
			AddDocs(writer, 3);
			AddDocs(writer, 5);
			AddDocs(writer, 3);
			AddDocs(writer, 3);
			writer.Close();
			conf = NewWriterConfig();
			LogMergePolicy lmp = new LogDocMergePolicy();
			lmp.SetMaxMergeDocs(3);
			lmp.SetMergeFactor(2);
			conf.SetMergePolicy(lmp);
			writer = new IndexWriter(dir, conf);
			writer.ForceMerge(1);
			writer.Close();
			// Should only be 4 segments in the index, because of the merge factor and
			// max merge docs settings.
			SegmentInfos sis = new SegmentInfos();
			sis.Read(dir);
			NUnit.Framework.Assert.AreEqual(4, sis.Size());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSingleMergeableSegment()
		{
			Directory dir = new RAMDirectory();
			IndexWriterConfig conf = NewWriterConfig();
			IndexWriter writer = new IndexWriter(dir, conf);
			AddDocs(writer, 3);
			AddDocs(writer, 5);
			AddDocs(writer, 3);
			// delete the last document, so that the last segment is merged.
			writer.DeleteDocuments(new Term("id", "10"));
			writer.Close();
			conf = NewWriterConfig();
			LogMergePolicy lmp = new LogDocMergePolicy();
			lmp.SetMaxMergeDocs(3);
			conf.SetMergePolicy(lmp);
			writer = new IndexWriter(dir, conf);
			writer.ForceMerge(1);
			writer.Close();
			// Verify that the last segment does not have deletions.
			SegmentInfos sis = new SegmentInfos();
			sis.Read(dir);
			NUnit.Framework.Assert.AreEqual(3, sis.Size());
			NUnit.Framework.Assert.IsFalse(sis.Info(2).HasDeletions());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSingleNonMergeableSegment()
		{
			Directory dir = new RAMDirectory();
			IndexWriterConfig conf = NewWriterConfig();
			IndexWriter writer = new IndexWriter(dir, conf);
			AddDocs(writer, 3, true);
			writer.Close();
			conf = NewWriterConfig();
			LogMergePolicy lmp = new LogDocMergePolicy();
			lmp.SetMaxMergeDocs(3);
			conf.SetMergePolicy(lmp);
			writer = new IndexWriter(dir, conf);
			writer.ForceMerge(1);
			writer.Close();
			// Verify that the last segment does not have deletions.
			SegmentInfos sis = new SegmentInfos();
			sis.Read(dir);
			NUnit.Framework.Assert.AreEqual(1, sis.Size());
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSingleMergeableTooLargeSegment()
		{
			Directory dir = new RAMDirectory();
			IndexWriterConfig conf = NewWriterConfig();
			IndexWriter writer = new IndexWriter(dir, conf);
			AddDocs(writer, 5, true);
			// delete the last document
			writer.DeleteDocuments(new Term("id", "4"));
			writer.Close();
			conf = NewWriterConfig();
			LogMergePolicy lmp = new LogDocMergePolicy();
			lmp.SetMaxMergeDocs(2);
			conf.SetMergePolicy(lmp);
			writer = new IndexWriter(dir, conf);
			writer.ForceMerge(1);
			writer.Close();
			// Verify that the last segment does not have deletions.
			SegmentInfos sis = new SegmentInfos();
			sis.Read(dir);
			NUnit.Framework.Assert.AreEqual(1, sis.Size());
			NUnit.Framework.Assert.IsTrue(sis.Info(0).HasDeletions());
		}
	}
}
