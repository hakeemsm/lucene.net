/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Test.Index
{
	public class TestPerSegmentDeletes : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestDeletes1()
		{
			//IndexWriter.debug2 = System.out;
			Directory dir = new MockDirectoryWrapper(new Random(Random().NextLong()), new RAMDirectory
				());
			IndexWriterConfig iwc = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer
				(Random()));
			iwc.SetMergeScheduler(new SerialMergeScheduler());
			iwc.SetMaxBufferedDocs(5000);
			iwc.SetRAMBufferSizeMB(100);
			TestPerSegmentDeletes.RangeMergePolicy fsmp = new TestPerSegmentDeletes.RangeMergePolicy
				(this, false);
			iwc.SetMergePolicy(fsmp);
			IndexWriter writer = new IndexWriter(dir, iwc);
			for (int x = 0; x < 5; x++)
			{
				writer.AddDocument(DocHelper.CreateDocument(x, "1", 2));
			}
			//System.out.println("numRamDocs(" + x + ")" + writer.numRamDocs());
			//System.out.println("commit1");
			writer.Commit();
			AreEqual(1, writer.segmentInfos.Size());
			for (int x_1 = 5; x_1 < 10; x_1++)
			{
				writer.AddDocument(DocHelper.CreateDocument(x_1, "2", 2));
			}
			//System.out.println("numRamDocs(" + x + ")" + writer.numRamDocs());
			//System.out.println("commit2");
			writer.Commit();
			AreEqual(2, writer.segmentInfos.Size());
			for (int x_2 = 10; x_2 < 15; x_2++)
			{
				writer.AddDocument(DocHelper.CreateDocument(x_2, "3", 2));
			}
			//System.out.println("numRamDocs(" + x + ")" + writer.numRamDocs());
			writer.DeleteDocuments(new Term("id", "1"));
			writer.DeleteDocuments(new Term("id", "11"));
			// flushing without applying deletes means
			// there will still be deletes in the segment infos
			writer.Flush(false, false);
			IsTrue(writer.bufferedUpdatesStream.Any());
			// get reader flushes pending deletes
			// so there should not be anymore
			IndexReader r1 = writer.Reader;
			IsFalse(writer.bufferedUpdatesStream.Any());
			r1.Dispose();
			// delete id:2 from the first segment
			// merge segments 0 and 1
			// which should apply the delete id:2
			writer.DeleteDocuments(new Term("id", "2"));
			writer.Flush(false, false);
			fsmp = (TestPerSegmentDeletes.RangeMergePolicy)writer.Config.GetMergePolicy(
				);
			fsmp.doMerge = true;
			fsmp.start = 0;
			fsmp.length = 2;
			writer.MaybeMerge();
			AreEqual(2, writer.segmentInfos.Size());
			// id:2 shouldn't exist anymore because
			// it's been applied in the merge and now it's gone
			IndexReader r2 = writer.Reader;
			int[] id2docs = ToDocsArray(new Term("id", "2"), null, r2);
			IsTrue(id2docs == null);
			r2.Dispose();
			// System.out.println("segdels2:"+writer.docWriter.segmentDeletes.toString());
			//System.out.println("close");
			writer.Dispose();
			dir.Dispose();
		}

		/// <summary>
		/// static boolean hasPendingDeletes(SegmentInfos infos) {
		/// for (SegmentInfo info : infos) {
		/// if (info.deletes.any()) {
		/// return true;
		/// }
		/// }
		/// return false;
		/// }
		/// </summary>
		/// <exception cref="System.Exception"></exception>
		internal virtual void Part2(IndexWriter writer, TestPerSegmentDeletes.RangeMergePolicy
			 fsmp)
		{
			for (int x = 20; x < 25; x++)
			{
				writer.AddDocument(DocHelper.CreateDocument(x, "5", 2));
			}
			//System.out.println("numRamDocs(" + x + ")" + writer.numRamDocs());
			writer.Flush(false, false);
			for (int x_1 = 25; x_1 < 30; x_1++)
			{
				writer.AddDocument(DocHelper.CreateDocument(x_1, "5", 2));
			}
			//System.out.println("numRamDocs(" + x + ")" + writer.numRamDocs());
			writer.Flush(false, false);
			//System.out.println("infos3:"+writer.segmentInfos);
			Term delterm = new Term("id", "8");
			writer.DeleteDocuments(delterm);
			//System.out.println("segdels3:" + writer.docWriter.deletesToString());
			fsmp.doMerge = true;
			fsmp.start = 1;
			fsmp.length = 2;
			writer.MaybeMerge();
		}

		// deletes for info1, the newly created segment from the
		// merge should have no deletes because they were applied in
		// the merge
		//SegmentInfo info1 = writer.segmentInfos.info(1);
		//assertFalse(exists(info1, writer.docWriter.segmentDeletes));
		//System.out.println("infos4:"+writer.segmentInfos);
		//System.out.println("segdels4:" + writer.docWriter.deletesToString());
		internal virtual bool SegThere(SegmentCommitInfo info, SegmentInfos infos)
		{
			foreach (SegmentCommitInfo si in infos)
			{
				if (si.info.name.Equals(info.info.name))
				{
					return true;
				}
			}
			return false;
		}

		public static void PrintDelDocs(Bits bits)
		{
			if (bits == null)
			{
				return;
			}
			for (int x = 0; x < bits.Length(); x++)
			{
				System.Console.Out.WriteLine(x + ":" + bits.Get(x));
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual int[] ToDocsArray(Term term, Bits bits, IndexReader reader)
		{
			Fields fields = MultiFields.GetFields(reader);
			Terms cterms = fields.Terms(term.field);
			TermsEnum ctermsEnum = cterms.Iterator(null);
			if (ctermsEnum.SeekExact(new BytesRef(term.Text())))
			{
				DocsEnum docsEnum = TestUtil.Docs(Random(), ctermsEnum, bits, null, DocsEnum.FLAG_NONE
					);
				return ToArray(docsEnum);
			}
			return null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public static int[] ToArray(DocsEnum docsEnum)
		{
			IList<int> docs = new List<int>();
			while (docsEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
			{
				int docID = docsEnum.DocID;
				docs.Add(docID);
			}
			return ArrayUtil.ToIntArray(docs);
		}

		public class RangeMergePolicy : MergePolicy
		{
			internal bool doMerge = false;

			internal int start;

			internal int length;

			private readonly bool useCompoundFile;

			private RangeMergePolicy(TestPerSegmentDeletes _enclosing, bool useCompoundFile)
			{
				this._enclosing = _enclosing;
				this.useCompoundFile = useCompoundFile;
			}

			public override void Close()
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override MergePolicy.MergeSpecification FindMerges(MergeTrigger mergeTrigger
				, SegmentInfos segmentInfos)
			{
				MergePolicy.MergeSpecification ms = new MergePolicy.MergeSpecification();
				if (this.doMerge)
				{
					MergePolicy.OneMerge om = new MergePolicy.OneMerge(segmentInfos.AsList().SubList(
						this.start, this.start + this.length));
					ms.Add(om);
					this.doMerge = false;
					return ms;
				}
				return null;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override MergePolicy.MergeSpecification FindForcedMerges(SegmentInfos segmentInfos
				, int maxSegmentCount, IDictionary<SegmentCommitInfo, bool> segmentsToMerge)
			{
				return null;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override MergePolicy.MergeSpecification FindForcedDeletesMerges(SegmentInfos
				 segmentInfos)
			{
				return null;
			}

			public override bool UseCompoundFile(SegmentInfos segments, SegmentCommitInfo newSegment
				)
			{
				return this.useCompoundFile;
			}

			private readonly TestPerSegmentDeletes _enclosing;
		}
	}
}
