/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestSegmentMerger : LuceneTestCase
	{
		private Directory mergedDir;

		private string mergedSegment = "test";

		private Directory merge1Dir;

		private Lucene.Net.Document.Document doc1 = new Lucene.Net.Document.Document
			();

		private SegmentReader reader1 = null;

		private Directory merge2Dir;

		private Lucene.Net.Document.Document doc2 = new Lucene.Net.Document.Document
			();

		private SegmentReader reader2 = null;

		//The variables for the new merged segment
		//First segment to be merged
		//Second Segment to be merged
		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			mergedDir = NewDirectory();
			merge1Dir = NewDirectory();
			merge2Dir = NewDirectory();
			DocHelper.SetupDoc(doc1);
			SegmentCommitInfo info1 = DocHelper.WriteDoc(Random(), merge1Dir, doc1);
			DocHelper.SetupDoc(doc2);
			SegmentCommitInfo info2 = DocHelper.WriteDoc(Random(), merge2Dir, doc2);
			reader1 = new SegmentReader(info1, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, NewIOContext
				(Random()));
			reader2 = new SegmentReader(info2, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, NewIOContext
				(Random()));
		}

		/// <exception cref="System.Exception"></exception>
		public override void TearDown()
		{
			reader1.Close();
			reader2.Close();
			mergedDir.Close();
			merge1Dir.Close();
			merge2Dir.Close();
			base.TearDown();
		}

		public virtual void Test()
		{
			NUnit.Framework.Assert.IsTrue(mergedDir != null);
			NUnit.Framework.Assert.IsTrue(merge1Dir != null);
			NUnit.Framework.Assert.IsTrue(merge2Dir != null);
			NUnit.Framework.Assert.IsTrue(reader1 != null);
			NUnit.Framework.Assert.IsTrue(reader2 != null);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestMerge()
		{
			Codec codec = Codec.GetDefault();
			SegmentInfo si = new SegmentInfo(mergedDir, Constants.LUCENE_MAIN_VERSION, mergedSegment
				, -1, false, codec, null);
			SegmentMerger merger = new SegmentMerger(Arrays.AsList<AtomicReader>(reader1, reader2
				), si, InfoStream.GetDefault(), mergedDir, IndexWriterConfig.DEFAULT_TERM_INDEX_INTERVAL
				, MergeState.CheckAbort.NONE, new FieldInfos.FieldNumbers(), NewIOContext(Random
				()), true);
			MergeState mergeState = merger.Merge();
			int docsMerged = mergeState.segmentInfo.GetDocCount();
			NUnit.Framework.Assert.IsTrue(docsMerged == 2);
			//Should be able to open a new SegmentReader against the new directory
			SegmentReader mergedReader = new SegmentReader(new SegmentCommitInfo(new SegmentInfo
				(mergedDir, Constants.LUCENE_MAIN_VERSION, mergedSegment, docsMerged, false, codec
				, null), 0, -1L, -1L), DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, NewIOContext
				(Random()));
			NUnit.Framework.Assert.IsTrue(mergedReader != null);
			NUnit.Framework.Assert.IsTrue(mergedReader.NumDocs() == 2);
			Lucene.Net.Document.Document newDoc1 = mergedReader.Document(0);
			NUnit.Framework.Assert.IsTrue(newDoc1 != null);
			//There are 2 unstored fields on the document
			NUnit.Framework.Assert.IsTrue(DocHelper.NumFields(newDoc1) == DocHelper.NumFields
				(doc1) - DocHelper.unstored.Count);
			Lucene.Net.Document.Document newDoc2 = mergedReader.Document(1);
			NUnit.Framework.Assert.IsTrue(newDoc2 != null);
			NUnit.Framework.Assert.IsTrue(DocHelper.NumFields(newDoc2) == DocHelper.NumFields
				(doc2) - DocHelper.unstored.Count);
			DocsEnum termDocs = TestUtil.Docs(Random(), mergedReader, DocHelper.TEXT_FIELD_2_KEY
				, new BytesRef("field"), MultiFields.GetLiveDocs(mergedReader), null, 0);
			NUnit.Framework.Assert.IsTrue(termDocs != null);
			NUnit.Framework.Assert.IsTrue(termDocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS
				);
			int tvCount = 0;
			foreach (FieldInfo fieldInfo in mergedReader.GetFieldInfos())
			{
				if (fieldInfo.HasVectors())
				{
					tvCount++;
				}
			}
			//System.out.println("stored size: " + stored.size());
			NUnit.Framework.Assert.AreEqual("We do not have 3 fields that were indexed with term vector"
				, 3, tvCount);
			Terms vector = mergedReader.GetTermVectors(0).Terms(DocHelper.TEXT_FIELD_2_KEY);
			NUnit.Framework.Assert.IsNotNull(vector);
			NUnit.Framework.Assert.AreEqual(3, vector.Size());
			TermsEnum termsEnum = vector.Iterator(null);
			int i = 0;
			while (termsEnum.Next() != null)
			{
				string term = termsEnum.Term().Utf8ToString();
				int freq = (int)termsEnum.TotalTermFreq();
				//System.out.println("Term: " + term + " Freq: " + freq);
				NUnit.Framework.Assert.IsTrue(DocHelper.FIELD_2_TEXT.IndexOf(term) != -1);
				NUnit.Framework.Assert.IsTrue(DocHelper.FIELD_2_FREQS[i] == freq);
				i++;
			}
			TestSegmentReader.CheckNorms(mergedReader);
			mergedReader.Close();
		}

		private static bool Equals(MergeState.DocMap map1, MergeState.DocMap map2)
		{
			if (map1.MaxDoc() != map2.MaxDoc())
			{
				return false;
			}
			for (int i = 0; i < map1.MaxDoc(); ++i)
			{
				if (map1.Get(i) != map2.Get(i))
				{
					return false;
				}
			}
			return true;
		}

		public virtual void TestBuildDocMap()
		{
			int maxDoc = TestUtil.NextInt(Random(), 1, 128);
			int numDocs = TestUtil.NextInt(Random(), 0, maxDoc);
			int numDeletedDocs = maxDoc - numDocs;
			FixedBitSet liveDocs = new FixedBitSet(maxDoc);
			for (int i = 0; i < numDocs; ++i)
			{
				while (true)
				{
					int docID = Random().Next(maxDoc);
					if (!liveDocs.Get(docID))
					{
						liveDocs.Set(docID);
						break;
					}
				}
			}
			MergeState.DocMap docMap = MergeState.DocMap.Build(maxDoc, liveDocs);
			NUnit.Framework.Assert.AreEqual(maxDoc, docMap.MaxDoc());
			NUnit.Framework.Assert.AreEqual(numDocs, docMap.NumDocs());
			NUnit.Framework.Assert.AreEqual(numDeletedDocs, docMap.NumDeletedDocs());
			// 
			//HM:revisit 
			//assert the mapping is compact
			for (int i_1 = 0; i_1 < maxDoc; ++i_1)
			{
				if (!liveDocs.Get(i_1))
				{
					NUnit.Framework.Assert.AreEqual(-1, docMap.Get(i_1));
					++del;
				}
				else
				{
					NUnit.Framework.Assert.AreEqual(i_1 - del, docMap.Get(i_1));
				}
			}
		}
	}
}
