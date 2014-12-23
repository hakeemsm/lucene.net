/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Lucene3x;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene3x
{
	public class TestTermInfosReaderIndex : LuceneTestCase
	{
		private static int NUMBER_OF_DOCUMENTS;

		private static int NUMBER_OF_FIELDS;

		private static TermInfosReaderIndex index;

		private static Directory directory;

		private static SegmentTermEnum termEnum;

		private static int indexDivisor;

		private static int termIndexInterval;

		private static IndexReader reader;

		private static IList<Term> sampleTerms;

		/// <summary>we will manually instantiate preflex-rw here</summary>
		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.BeforeClass]
		public static void BeforeClass()
		{
			// NOTE: turn off compound file, this test will open some index files directly.
			LuceneTestCase.OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true;
			IndexWriterConfig config = ((IndexWriterConfig)NewIndexWriterConfig(TEST_VERSION_CURRENT
				, new MockAnalyzer(Random(), MockTokenizer.KEYWORD, false)).SetUseCompoundFile(false
				));
			termIndexInterval = config.GetTermIndexInterval();
			indexDivisor = TestUtil.NextInt(Random(), 1, 10);
			NUMBER_OF_DOCUMENTS = AtLeast(100);
			NUMBER_OF_FIELDS = AtLeast(Math.Max(10, 3 * termIndexInterval * indexDivisor / NUMBER_OF_DOCUMENTS
				));
			directory = NewDirectory();
			config.SetCodec(new PreFlexRWCodec());
			LogMergePolicy mp = NewLogMergePolicy();
			// NOTE: turn off compound file, this test will open some index files directly.
			mp.SetNoCFSRatio(0.0);
			config.SetMergePolicy(mp);
			Populate(directory, config);
			DirectoryReader r0 = IndexReader.Open(directory);
			SegmentReader r = LuceneTestCase.GetOnlySegmentReader(r0);
			string segment = r.GetSegmentName();
			r.Close();
			FieldInfosReader infosReader = new PreFlexRWCodec().FieldInfosFormat().GetFieldInfosReader
				();
			FieldInfos fieldInfos = infosReader.Read(directory, segment, string.Empty, IOContext
				.READONCE);
			string segmentFileName = IndexFileNames.SegmentFileName(segment, string.Empty, Lucene3xPostingsFormat
				.TERMS_INDEX_EXTENSION);
			long tiiFileLength = directory.FileLength(segmentFileName);
			IndexInput input = directory.OpenInput(segmentFileName, NewIOContext(Random()));
			termEnum = new SegmentTermEnum(directory.OpenInput(IndexFileNames.SegmentFileName
				(segment, string.Empty, Lucene3xPostingsFormat.TERMS_EXTENSION), NewIOContext(Random
				())), fieldInfos, false);
			int totalIndexInterval = termEnum.indexInterval * indexDivisor;
			SegmentTermEnum indexEnum = new SegmentTermEnum(input, fieldInfos, true);
			index = new TermInfosReaderIndex(indexEnum, indexDivisor, tiiFileLength, totalIndexInterval
				);
			indexEnum.Close();
			input.Close();
			reader = IndexReader.Open(directory);
			sampleTerms = Sample(Random(), reader, 1000);
		}

		/// <exception cref="System.Exception"></exception>
		[NUnit.Framework.AfterClass]
		public static void AfterClass()
		{
			termEnum.Close();
			reader.Close();
			directory.Close();
			termEnum = null;
			reader = null;
			directory = null;
			index = null;
			sampleTerms = null;
		}

		/// <exception cref="Lucene.Net.Index.CorruptIndexException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestSeekEnum()
		{
			int indexPosition = 3;
			SegmentTermEnum clone = termEnum.Clone();
			Term term = FindTermThatWouldBeAtIndex(clone, indexPosition);
			SegmentTermEnum enumerator = clone;
			index.SeekEnum(enumerator, indexPosition);
			NUnit.Framework.Assert.AreEqual(term, enumerator.Term());
			clone.Close();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestCompareTo()
		{
			Term term = new Term("field" + Random().Next(NUMBER_OF_FIELDS), GetText());
			for (int i = 0; i < index.Length(); i++)
			{
				Term t = index.GetTerm(i);
				int compareTo = term.CompareTo(t);
				NUnit.Framework.Assert.AreEqual(compareTo, index.CompareTo(term, i));
			}
		}

		/// <exception cref="Lucene.Net.Index.CorruptIndexException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void TestRandomSearchPerformance()
		{
			IndexSearcher searcher = new IndexSearcher(reader);
			foreach (Term t in sampleTerms)
			{
				TermQuery query = new TermQuery(t);
				TopDocs topDocs = searcher.Search(query, 10);
				NUnit.Framework.Assert.IsTrue(topDocs.totalHits > 0);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static IList<Term> Sample(Random random, IndexReader reader, int size)
		{
			IList<Term> sample = new AList<Term>();
			Fields fields = MultiFields.GetFields(reader);
			foreach (string field in fields)
			{
				Terms terms = fields.Terms(field);
				NUnit.Framework.Assert.IsNotNull(terms);
				TermsEnum termsEnum = terms.Iterator(null);
				while (termsEnum.Next() != null)
				{
					if (sample.Count >= size)
					{
						int pos = random.Next(size);
						sample.Set(pos, new Term(field, termsEnum.Term()));
					}
					else
					{
						sample.AddItem(new Term(field, termsEnum.Term()));
					}
				}
			}
			Sharpen.Collections.Shuffle(sample);
			return sample;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private Term FindTermThatWouldBeAtIndex(SegmentTermEnum termEnum, int index)
		{
			int termPosition = index * termIndexInterval * indexDivisor;
			for (int i = 0; i < termPosition; i++)
			{
				// TODO: this test just uses random terms, so this is always possible
				AssumeTrue("ran out of terms", termEnum.Next());
			}
			Term term = termEnum.Term();
			// An indexed term is only written when the term after
			// it exists, so, if the number of terms is 0 mod
			// termIndexInterval, the last index term will not be
			// written; so we require a term after this term
			// as well:
			AssumeTrue("ran out of terms", termEnum.Next());
			return term;
		}

		/// <exception cref="Lucene.Net.Index.CorruptIndexException"></exception>
		/// <exception cref="Lucene.Net.Store.LockObtainFailedException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private static void Populate(Directory directory, IndexWriterConfig config)
		{
			IndexWriter writer = new IndexWriter(directory, config);
			for (int i = 0; i < NUMBER_OF_DOCUMENTS; i++)
			{
				Lucene.Net.Document.Document document = new Lucene.Net.Document.Document
					();
				for (int f = 0; f < NUMBER_OF_FIELDS; f++)
				{
					document.Add(NewStringField("field" + f, GetText(), Field.Store.NO));
				}
				writer.AddDocument(document);
			}
			writer.ForceMerge(1);
			writer.Close();
		}

		private static string GetText()
		{
			return System.Convert.ToString(Random().NextLong(), char.MAX_RADIX);
		}
	}
}
