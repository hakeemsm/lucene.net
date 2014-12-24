/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Text;
using Lucene.Net.Test.Analysis;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	public class TestDocsAndPositions : LuceneTestCase
	{
		private string fieldName;

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			fieldName = "field" + Random().Next();
		}

		/// <summary>
		/// Simple testcase for
		/// <see cref="DocsAndPositionsEnum">DocsAndPositionsEnum</see>
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestPositionsSimple()
		{
			Directory directory = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), directory, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
			for (int i = 0; i < 39; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
				customType.SetOmitNorms(true);
				doc.Add(NewField(fieldName, "1 2 3 4 5 6 7 8 9 10 " + "1 2 3 4 5 6 7 8 9 10 " + "1 2 3 4 5 6 7 8 9 10 "
					 + "1 2 3 4 5 6 7 8 9 10", customType));
				writer.AddDocument(doc);
			}
			IndexReader reader = writer.GetReader();
			writer.Close();
			int num = AtLeast(13);
			for (int i_1 = 0; i_1 < num; i_1++)
			{
				BytesRef bytes = new BytesRef("1");
				IndexReaderContext topReaderContext = reader.GetContext();
				foreach (AtomicReaderContext atomicReaderContext in topReaderContext.Leaves())
				{
					DocsAndPositionsEnum docsAndPosEnum = GetDocsAndPositions(((AtomicReader)atomicReaderContext
						.Reader()), bytes, null);
					IsNotNull(docsAndPosEnum);
					if (((AtomicReader)atomicReaderContext.Reader()).MaxDoc == 0)
					{
						continue;
					}
					int advance = docsAndPosEnum.Advance(Random().Next(((AtomicReader)atomicReaderContext
						.Reader()).MaxDoc));
					do
					{
						string msg = "Advanced to: " + advance + " current doc: " + docsAndPosEnum.DocID(
							);
						// TODO: + " usePayloads: " + usePayload;
						AreEqual(msg, 4, docsAndPosEnum.Freq);
						AreEqual(msg, 0, docsAndPosEnum.NextPosition());
						AreEqual(msg, 4, docsAndPosEnum.Freq);
						AreEqual(msg, 10, docsAndPosEnum.NextPosition());
						AreEqual(msg, 4, docsAndPosEnum.Freq);
						AreEqual(msg, 20, docsAndPosEnum.NextPosition());
						AreEqual(msg, 4, docsAndPosEnum.Freq);
						AreEqual(msg, 30, docsAndPosEnum.NextPosition());
					}
					while (docsAndPosEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
				}
			}
			reader.Close();
			directory.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual DocsAndPositionsEnum GetDocsAndPositions(AtomicReader reader, BytesRef
			 bytes, Bits liveDocs)
		{
			Terms terms = reader.Terms(fieldName);
			if (terms != null)
			{
				TermsEnum te = terms.Iterator(null);
				if (te.SeekExact(bytes))
				{
					return te.DocsAndPositions(liveDocs, null);
				}
			}
			return null;
		}

		/// <summary>
		/// this test indexes random numbers within a range into a field and checks
		/// their occurrences by searching for a number from that range selected at
		/// random.
		/// </summary>
		/// <remarks>
		/// this test indexes random numbers within a range into a field and checks
		/// their occurrences by searching for a number from that range selected at
		/// random. All positions for that number are saved up front and compared to
		/// the enums positions.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestRandomPositions()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy
				()));
			int numDocs = AtLeast(47);
			int max = 1051;
			int term = Random().Next(max);
			int[][] positionsInDoc = new int[numDocs][];
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.SetOmitNorms(true);
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				AList<int> positions = new AList<int>();
				StringBuilder builder = new StringBuilder();
				int num = AtLeast(131);
				for (int j = 0; j < num; j++)
				{
					int nextInt = Random().Next(max);
					builder.Append(nextInt).Append(" ");
					if (nextInt == term)
					{
						positions.AddItem(Sharpen.Extensions.ValueOf(j));
					}
				}
				if (positions.Count == 0)
				{
					builder.Append(term);
					positions.AddItem(num);
				}
				doc.Add(NewField(fieldName, builder.ToString(), customType));
				positionsInDoc[i] = Sharpen.Collections.ToArray(positions, new int[0]);
				writer.AddDocument(doc);
			}
			IndexReader reader = writer.GetReader();
			writer.Close();
			int num_1 = AtLeast(13);
			for (int i_1 = 0; i_1 < num_1; i_1++)
			{
				BytesRef bytes = new BytesRef(string.Empty + term);
				IndexReaderContext topReaderContext = reader.GetContext();
				foreach (AtomicReaderContext atomicReaderContext in topReaderContext.Leaves())
				{
					DocsAndPositionsEnum docsAndPosEnum = GetDocsAndPositions(((AtomicReader)atomicReaderContext
						.Reader()), bytes, null);
					IsNotNull(docsAndPosEnum);
					int initDoc = 0;
					int maxDoc = ((AtomicReader)atomicReaderContext.Reader()).MaxDoc;
					// initially advance or do next doc
					if (Random().NextBoolean())
					{
						initDoc = docsAndPosEnum.NextDoc();
					}
					else
					{
						initDoc = docsAndPosEnum.Advance(Random().Next(maxDoc));
					}
					do
					{
						// now run through the scorer and check if all positions are there...
						int docID = docsAndPosEnum.DocID;
						if (docID == DocIdSetIterator.NO_MORE_DOCS)
						{
							break;
						}
						int[] pos = positionsInDoc[atomicReaderContext.docBase + docID];
						AreEqual(pos.Length, docsAndPosEnum.Freq);
						// number of positions read should be random - don't read all of them
						// allways
						int howMany = Random().Next(20) == 0 ? pos.Length - Random().Next(pos.Length) : pos
							.Length;
						for (int j = 0; j < howMany; j++)
						{
							AreEqual("iteration: " + i_1 + " initDoc: " + initDoc + " doc: "
								 + docID + " base: " + atomicReaderContext.docBase + " positions: " + Arrays.ToString
								(pos), pos[j], docsAndPosEnum.NextPosition());
						}
						if (Random().Next(10) == 0)
						{
							// once is a while advance
							if (docsAndPosEnum.Advance(docID + 1 + Random().Next((maxDoc - docID))) == DocIdSetIterator
								.NO_MORE_DOCS)
							{
								break;
							}
						}
					}
					while (docsAndPosEnum.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
				}
			}
			reader.Close();
			dir.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestRandomDocs()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())).SetMergePolicy(NewLogMergePolicy
				()));
			int numDocs = AtLeast(49);
			int max = 15678;
			int term = Random().Next(max);
			int[] freqInDoc = new int[numDocs];
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.SetOmitNorms(true);
			for (int i = 0; i < numDocs; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				StringBuilder builder = new StringBuilder();
				for (int j = 0; j < 199; j++)
				{
					int nextInt = Random().Next(max);
					builder.Append(nextInt).Append(' ');
					if (nextInt == term)
					{
						freqInDoc[i]++;
					}
				}
				doc.Add(NewField(fieldName, builder.ToString(), customType));
				writer.AddDocument(doc);
			}
			IndexReader reader = writer.GetReader();
			writer.Close();
			int num = AtLeast(13);
			for (int i_1 = 0; i_1 < num; i_1++)
			{
				BytesRef bytes = new BytesRef(string.Empty + term);
				IndexReaderContext topReaderContext = reader.GetContext();
				foreach (AtomicReaderContext context in topReaderContext.Leaves())
				{
					int maxDoc = ((AtomicReader)context.Reader()).MaxDoc;
					DocsEnum docsEnum = TestUtil.Docs(Random(), ((AtomicReader)context.Reader()), fieldName
						, bytes, null, null, DocsEnum.FLAG_FREQS);
					if (FindNext(freqInDoc, context.docBase, context.docBase + maxDoc) == int.MaxValue)
					{
						IsNull(docsEnum);
						continue;
					}
					IsNotNull(docsEnum);
					docsEnum.NextDoc();
					for (int j = 0; j < maxDoc; j++)
					{
						if (freqInDoc[context.docBase + j] != 0)
						{
							AreEqual(j, docsEnum.DocID);
							AreEqual(docsEnum.Freq, freqInDoc[context.docBase + j]);
							if (i_1 % 2 == 0 && Random().Next(10) == 0)
							{
								int next = FindNext(freqInDoc, context.docBase + j + 1, context.docBase + maxDoc)
									 - context.docBase;
								int advancedTo = docsEnum.Advance(next);
								if (next >= maxDoc)
								{
									AreEqual(DocIdSetIterator.NO_MORE_DOCS, advancedTo);
								}
								else
								{
									IsTrue("advanced to: " + advancedTo + " but should be <= "
										 + next, next >= advancedTo);
								}
							}
							else
							{
								docsEnum.NextDoc();
							}
						}
					}
					AreEqual("docBase: " + context.docBase + " maxDoc: " + maxDoc
						 + " " + docsEnum.GetType(), DocIdSetIterator.NO_MORE_DOCS, docsEnum.DocID);
				}
			}
			reader.Close();
			dir.Close();
		}

		private static int FindNext(int[] docs, int pos, int max)
		{
			for (int i = pos; i < max; i++)
			{
				if (docs[i] != 0)
				{
					return i;
				}
			}
			return int.MaxValue;
		}

		/// <summary>
		/// tests retrieval of positions for terms that have a large number of
		/// occurrences to force test of buffer refill during positions iteration.
		/// </summary>
		/// <remarks>
		/// tests retrieval of positions for terms that have a large number of
		/// occurrences to force test of buffer refill during positions iteration.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestLargeNumberOfPositions()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, NewIndexWriterConfig
				(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
			int howMany = 1000;
			FieldType customType = new FieldType(TextField.TYPE_NOT_STORED);
			customType.SetOmitNorms(true);
			for (int i = 0; i < 39; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
					();
				StringBuilder builder = new StringBuilder();
				for (int j = 0; j < howMany; j++)
				{
					if (j % 2 == 0)
					{
						builder.Append("even ");
					}
					else
					{
						builder.Append("odd ");
					}
				}
				doc.Add(NewField(fieldName, builder.ToString(), customType));
				writer.AddDocument(doc);
			}
			// now do searches
			IndexReader reader = writer.GetReader();
			writer.Close();
			int num = AtLeast(13);
			for (int i_1 = 0; i_1 < num; i_1++)
			{
				BytesRef bytes = new BytesRef("even");
				IndexReaderContext topReaderContext = reader.GetContext();
				foreach (AtomicReaderContext atomicReaderContext in topReaderContext.Leaves())
				{
					DocsAndPositionsEnum docsAndPosEnum = GetDocsAndPositions(((AtomicReader)atomicReaderContext
						.Reader()), bytes, null);
					IsNotNull(docsAndPosEnum);
					int initDoc = 0;
					int maxDoc = ((AtomicReader)atomicReaderContext.Reader()).MaxDoc;
					// initially advance or do next doc
					if (Random().NextBoolean())
					{
						initDoc = docsAndPosEnum.NextDoc();
					}
					else
					{
						initDoc = docsAndPosEnum.Advance(Random().Next(maxDoc));
					}
					string msg = "Iteration: " + i_1 + " initDoc: " + initDoc;
					// TODO: + " payloads: " + usePayload;
					AreEqual(howMany / 2, docsAndPosEnum.Freq);
					for (int j = 0; j < howMany; j += 2)
					{
						AreEqual("position missmatch index: " + j + " with freq: "
							 + docsAndPosEnum.Freq + " -- " + msg, j, docsAndPosEnum.NextPosition());
					}
				}
			}
			reader.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDocsEnumStart()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewStringField("foo", "bar", Field.Store.NO));
			writer.AddDocument(doc);
			DirectoryReader reader = writer.GetReader();
			AtomicReader r = GetOnlySegmentReader(reader);
			DocsEnum disi = TestUtil.Docs(Random(), r, "foo", new BytesRef("bar"), null, null
				, DocsEnum.FLAG_NONE);
			int docid = disi.DocID;
			AreEqual(-1, docid);
			IsTrue(disi.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			// now reuse and check again
			TermsEnum te = r.Terms("foo").Iterator(null);
			IsTrue(te.SeekExact(new BytesRef("bar")));
			disi = TestUtil.Docs(Random(), te, null, disi, DocsEnum.FLAG_NONE);
			docid = disi.DocID;
			AreEqual(-1, docid);
			IsTrue(disi.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			writer.Close();
			r.Close();
			dir.Close();
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDocsAndPositionsEnumStart()
		{
			Directory dir = NewDirectory();
			RandomIndexWriter writer = new RandomIndexWriter(Random(), dir);
			Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document
				();
			doc.Add(NewTextField("foo", "bar", Field.Store.NO));
			writer.AddDocument(doc);
			DirectoryReader reader = writer.GetReader();
			AtomicReader r = GetOnlySegmentReader(reader);
			DocsAndPositionsEnum disi = r.TermPositionsEnum(new Term("foo", "bar"));
			int docid = disi.DocID;
			AreEqual(-1, docid);
			IsTrue(disi.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			// now reuse and check again
			TermsEnum te = r.Terms("foo").Iterator(null);
			IsTrue(te.SeekExact(new BytesRef("bar")));
			disi = te.DocsAndPositions(null, disi);
			docid = disi.DocID;
			AreEqual(-1, docid);
			IsTrue(disi.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);
			writer.Close();
			r.Close();
			dir.Close();
		}
	}
}
