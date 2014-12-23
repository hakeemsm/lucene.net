/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene41
{
	/// <summary>Tests partial enumeration (only pulling a subset of the indexed data)</summary>
	public class TestBlockPostingsFormat3 : LuceneTestCase
	{
		internal const int MAXDOC = Lucene41PostingsFormat.BLOCK_SIZE * 20;

		// creates 8 fields with different options and does "duels" of fields against each other
		/// <exception cref="System.Exception"></exception>
		public virtual void Test()
		{
			Directory dir = NewDirectory();
			Analyzer analyzer = new _Analyzer_71(Analyzer.PER_FIELD_REUSE_STRATEGY);
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			iwc.SetCodec(TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat()));
			// TODO we could actually add more fields implemented with different PFs
			// or, just put this test into the usual rotation?
			RandomIndexWriter iw = new RandomIndexWriter(Random(), dir, iwc.Clone());
			Lucene.Net.Document.Document doc = new Lucene.Net.Document.Document
				();
			FieldType docsOnlyType = new FieldType(TextField.TYPE_NOT_STORED);
			// turn this on for a cross-check
			docsOnlyType.SetStoreTermVectors(true);
			docsOnlyType.SetIndexOptions(FieldInfo.IndexOptions.DOCS_ONLY);
			FieldType docsAndFreqsType = new FieldType(TextField.TYPE_NOT_STORED);
			// turn this on for a cross-check
			docsAndFreqsType.SetStoreTermVectors(true);
			docsAndFreqsType.SetIndexOptions(FieldInfo.IndexOptions.DOCS_AND_FREQS);
			FieldType positionsType = new FieldType(TextField.TYPE_NOT_STORED);
			// turn these on for a cross-check
			positionsType.SetStoreTermVectors(true);
			positionsType.SetStoreTermVectorPositions(true);
			positionsType.SetStoreTermVectorOffsets(true);
			positionsType.SetStoreTermVectorPayloads(true);
			FieldType offsetsType = new FieldType(positionsType);
			offsetsType.SetIndexOptions(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
				);
			Field field1 = new Field("field1docs", string.Empty, docsOnlyType);
			Field field2 = new Field("field2freqs", string.Empty, docsAndFreqsType);
			Field field3 = new Field("field3positions", string.Empty, positionsType);
			Field field4 = new Field("field4offsets", string.Empty, offsetsType);
			Field field5 = new Field("field5payloadsFixed", string.Empty, positionsType);
			Field field6 = new Field("field6payloadsVariable", string.Empty, positionsType);
			Field field7 = new Field("field7payloadsFixedOffsets", string.Empty, offsetsType);
			Field field8 = new Field("field8payloadsVariableOffsets", string.Empty, offsetsType
				);
			doc.Add(field1);
			doc.Add(field2);
			doc.Add(field3);
			doc.Add(field4);
			doc.Add(field5);
			doc.Add(field6);
			doc.Add(field7);
			doc.Add(field8);
			for (int i = 0; i < MAXDOC; i++)
			{
				string stringValue = Sharpen.Extensions.ToString(i) + " verycommon " + English.IntToEnglish
					(i).Replace('-', ' ') + " " + TestUtil.RandomSimpleString(Random());
				field1.SetStringValue(stringValue);
				field2.SetStringValue(stringValue);
				field3.SetStringValue(stringValue);
				field4.SetStringValue(stringValue);
				field5.SetStringValue(stringValue);
				field6.SetStringValue(stringValue);
				field7.SetStringValue(stringValue);
				field8.SetStringValue(stringValue);
				iw.AddDocument(doc);
			}
			iw.Close();
			Verify(dir);
			TestUtil.CheckIndex(dir);
			// for some extra coverage, checkIndex before we forceMerge
			iwc.SetOpenMode(IndexWriterConfig.OpenMode.APPEND);
			IndexWriter iw2 = new IndexWriter(dir, iwc.Clone());
			iw2.ForceMerge(1);
			iw2.Close();
			Verify(dir);
			dir.Close();
		}

		private sealed class _Analyzer_71 : Analyzer
		{
			public _Analyzer_71(Analyzer.ReuseStrategy baseArg1) : base(baseArg1)
			{
			}

			protected override Analyzer.TokenStreamComponents CreateComponents(string fieldName
				, StreamReader reader)
			{
				Tokenizer tokenizer = new MockTokenizer(reader);
				if (fieldName.Contains("payloadsFixed"))
				{
					TokenFilter filter = new MockFixedLengthPayloadFilter(new Random(0), tokenizer, 1
						);
					return new Analyzer.TokenStreamComponents(tokenizer, filter);
				}
				else
				{
					if (fieldName.Contains("payloadsVariable"))
					{
						TokenFilter filter = new MockVariableLengthPayloadFilter(new Random(0), tokenizer
							);
						return new Analyzer.TokenStreamComponents(tokenizer, filter);
					}
					else
					{
						return new Analyzer.TokenStreamComponents(tokenizer);
					}
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		private void Verify(Directory dir)
		{
			DirectoryReader ir = DirectoryReader.Open(dir);
			foreach (AtomicReaderContext leaf in ir.Leaves())
			{
				AtomicReader leafReader = ((AtomicReader)leaf.Reader());
				AssertTerms(leafReader.Terms("field1docs"), leafReader.Terms("field2freqs"), true
					);
				AssertTerms(leafReader.Terms("field3positions"), leafReader.Terms("field4offsets"
					), true);
				AssertTerms(leafReader.Terms("field4offsets"), leafReader.Terms("field5payloadsFixed"
					), true);
				AssertTerms(leafReader.Terms("field5payloadsFixed"), leafReader.Terms("field6payloadsVariable"
					), true);
				AssertTerms(leafReader.Terms("field6payloadsVariable"), leafReader.Terms("field7payloadsFixedOffsets"
					), true);
				AssertTerms(leafReader.Terms("field7payloadsFixedOffsets"), leafReader.Terms("field8payloadsVariableOffsets"
					), true);
			}
			ir.Close();
		}

		// following code is almost an exact dup of code from TestDuelingCodecs: sorry!
		/// <exception cref="System.Exception"></exception>
		public virtual void AssertTerms(Terms leftTerms, Terms rightTerms, bool deep)
		{
			if (leftTerms == null || rightTerms == null)
			{
				NUnit.Framework.Assert.IsNull(leftTerms);
				NUnit.Framework.Assert.IsNull(rightTerms);
				return;
			}
			AssertTermsStatistics(leftTerms, rightTerms);
			// NOTE: we don't 
			//HM:revisit 
			//assert hasOffsets/hasPositions/hasPayloads because they are allowed to be different
			TermsEnum leftTermsEnum = leftTerms.Iterator(null);
			TermsEnum rightTermsEnum = rightTerms.Iterator(null);
			AssertTermsEnum(leftTermsEnum, rightTermsEnum, true);
			AssertTermsSeeking(leftTerms, rightTerms);
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
						AssertTermsEnum(leftIntersection, rightIntersection, Rarely());
					}
				}
			}
		}

		/// <exception cref="System.Exception"></exception>
		private void AssertTermsSeeking(Terms leftTerms, Terms rightTerms)
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
						}
					}
				}
				numPasses++;
			}
			AList<BytesRef> shuffledTests = new AList<BytesRef>(tests);
			Sharpen.Collections.Shuffle(shuffledTests, random);
			foreach (BytesRef b in shuffledTests)
			{
				leftEnum = leftTerms.Iterator(leftEnum);
				rightEnum = rightTerms.Iterator(rightEnum);
				NUnit.Framework.Assert.AreEqual(leftEnum.SeekExact(b), rightEnum.SeekExact(b));
				NUnit.Framework.Assert.AreEqual(leftEnum.SeekExact(b), rightEnum.SeekExact(b));
				TermsEnum.SeekStatus leftStatus;
				TermsEnum.SeekStatus rightStatus;
				leftStatus = leftEnum.SeekCeil(b);
				rightStatus = rightEnum.SeekCeil(b);
				NUnit.Framework.Assert.AreEqual(leftStatus, rightStatus);
				if (leftStatus != TermsEnum.SeekStatus.END)
				{
					NUnit.Framework.Assert.AreEqual(leftEnum.Term(), rightEnum.Term());
				}
				leftStatus = leftEnum.SeekCeil(b);
				rightStatus = rightEnum.SeekCeil(b);
				NUnit.Framework.Assert.AreEqual(leftStatus, rightStatus);
				if (leftStatus != TermsEnum.SeekStatus.END)
				{
					NUnit.Framework.Assert.AreEqual(leftEnum.Term(), rightEnum.Term());
				}
			}
		}

		/// <summary>checks collection-level statistics on Terms</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void AssertTermsStatistics(Terms leftTerms, Terms rightTerms)
		{
			//HM:revisit 
			//assert leftTerms.getComparator() == rightTerms.getComparator();
			if (leftTerms.GetDocCount() != -1 && rightTerms.GetDocCount() != -1)
			{
				NUnit.Framework.Assert.AreEqual(leftTerms.GetDocCount(), rightTerms.GetDocCount()
					);
			}
			if (leftTerms.GetSumDocFreq() != -1 && rightTerms.GetSumDocFreq() != -1)
			{
				NUnit.Framework.Assert.AreEqual(leftTerms.GetSumDocFreq(), rightTerms.GetSumDocFreq
					());
			}
			if (leftTerms.GetSumTotalTermFreq() != -1 && rightTerms.GetSumTotalTermFreq() != 
				-1)
			{
				NUnit.Framework.Assert.AreEqual(leftTerms.GetSumTotalTermFreq(), rightTerms.GetSumTotalTermFreq
					());
			}
			if (leftTerms.Size() != -1 && rightTerms.Size() != -1)
			{
				NUnit.Framework.Assert.AreEqual(leftTerms.Size(), rightTerms.Size());
			}
		}

		/// <summary>
		/// checks the terms enum sequentially
		/// if deep is false, it does a 'shallow' test that doesnt go down to the docsenums
		/// </summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void AssertTermsEnum(TermsEnum leftTermsEnum, TermsEnum rightTermsEnum
			, bool deep)
		{
			BytesRef term;
			Bits randomBits = new TestBlockPostingsFormat3.RandomBits(MAXDOC, Random().NextDouble
				(), Random());
			DocsAndPositionsEnum leftPositions = null;
			DocsAndPositionsEnum rightPositions = null;
			DocsEnum leftDocs = null;
			DocsEnum rightDocs = null;
			while ((term = leftTermsEnum.Next()) != null)
			{
				NUnit.Framework.Assert.AreEqual(term, rightTermsEnum.Next());
				AssertTermStats(leftTermsEnum, rightTermsEnum);
				if (deep)
				{
					// with payloads + off
					AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.DocsAndPositions(null, leftPositions
						), rightPositions = rightTermsEnum.DocsAndPositions(null, rightPositions));
					AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.DocsAndPositions(randomBits
						, leftPositions), rightPositions = rightTermsEnum.DocsAndPositions(randomBits, rightPositions
						));
					AssertPositionsSkipping(leftTermsEnum.DocFreq(), leftPositions = leftTermsEnum.DocsAndPositions
						(null, leftPositions), rightPositions = rightTermsEnum.DocsAndPositions(null, rightPositions
						));
					AssertPositionsSkipping(leftTermsEnum.DocFreq(), leftPositions = leftTermsEnum.DocsAndPositions
						(randomBits, leftPositions), rightPositions = rightTermsEnum.DocsAndPositions(randomBits
						, rightPositions));
					// with payloads only
					AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.DocsAndPositions(null, leftPositions
						, DocsAndPositionsEnum.FLAG_PAYLOADS), rightPositions = rightTermsEnum.DocsAndPositions
						(null, rightPositions, DocsAndPositionsEnum.FLAG_PAYLOADS));
					AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.DocsAndPositions(randomBits
						, leftPositions, DocsAndPositionsEnum.FLAG_PAYLOADS), rightPositions = rightTermsEnum
						.DocsAndPositions(randomBits, rightPositions, DocsAndPositionsEnum.FLAG_PAYLOADS
						));
					AssertPositionsSkipping(leftTermsEnum.DocFreq(), leftPositions = leftTermsEnum.DocsAndPositions
						(null, leftPositions, DocsAndPositionsEnum.FLAG_PAYLOADS), rightPositions = rightTermsEnum
						.DocsAndPositions(null, rightPositions, DocsAndPositionsEnum.FLAG_PAYLOADS));
					AssertPositionsSkipping(leftTermsEnum.DocFreq(), leftPositions = leftTermsEnum.DocsAndPositions
						(randomBits, leftPositions, DocsAndPositionsEnum.FLAG_PAYLOADS), rightPositions 
						= rightTermsEnum.DocsAndPositions(randomBits, rightPositions, DocsAndPositionsEnum
						.FLAG_PAYLOADS));
					// with offsets only
					AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.DocsAndPositions(null, leftPositions
						, DocsAndPositionsEnum.FLAG_OFFSETS), rightPositions = rightTermsEnum.DocsAndPositions
						(null, rightPositions, DocsAndPositionsEnum.FLAG_OFFSETS));
					AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.DocsAndPositions(randomBits
						, leftPositions, DocsAndPositionsEnum.FLAG_OFFSETS), rightPositions = rightTermsEnum
						.DocsAndPositions(randomBits, rightPositions, DocsAndPositionsEnum.FLAG_OFFSETS)
						);
					AssertPositionsSkipping(leftTermsEnum.DocFreq(), leftPositions = leftTermsEnum.DocsAndPositions
						(null, leftPositions, DocsAndPositionsEnum.FLAG_OFFSETS), rightPositions = rightTermsEnum
						.DocsAndPositions(null, rightPositions, DocsAndPositionsEnum.FLAG_OFFSETS));
					AssertPositionsSkipping(leftTermsEnum.DocFreq(), leftPositions = leftTermsEnum.DocsAndPositions
						(randomBits, leftPositions, DocsAndPositionsEnum.FLAG_OFFSETS), rightPositions =
						 rightTermsEnum.DocsAndPositions(randomBits, rightPositions, DocsAndPositionsEnum
						.FLAG_OFFSETS));
					// with positions only
					AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.DocsAndPositions(null, leftPositions
						, DocsEnum.FLAG_NONE), rightPositions = rightTermsEnum.DocsAndPositions(null, rightPositions
						, DocsEnum.FLAG_NONE));
					AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.DocsAndPositions(randomBits
						, leftPositions, DocsEnum.FLAG_NONE), rightPositions = rightTermsEnum.DocsAndPositions
						(randomBits, rightPositions, DocsEnum.FLAG_NONE));
					AssertPositionsSkipping(leftTermsEnum.DocFreq(), leftPositions = leftTermsEnum.DocsAndPositions
						(null, leftPositions, DocsEnum.FLAG_NONE), rightPositions = rightTermsEnum.DocsAndPositions
						(null, rightPositions, DocsEnum.FLAG_NONE));
					AssertPositionsSkipping(leftTermsEnum.DocFreq(), leftPositions = leftTermsEnum.DocsAndPositions
						(randomBits, leftPositions, DocsEnum.FLAG_NONE), rightPositions = rightTermsEnum
						.DocsAndPositions(randomBits, rightPositions, DocsEnum.FLAG_NONE));
					// with freqs:
					AssertDocsEnum(leftDocs = leftTermsEnum.Docs(null, leftDocs), rightDocs = rightTermsEnum
						.Docs(null, rightDocs));
					AssertDocsEnum(leftDocs = leftTermsEnum.Docs(randomBits, leftDocs), rightDocs = rightTermsEnum
						.Docs(randomBits, rightDocs));
					// w/o freqs:
					AssertDocsEnum(leftDocs = leftTermsEnum.Docs(null, leftDocs, DocsEnum.FLAG_NONE), 
						rightDocs = rightTermsEnum.Docs(null, rightDocs, DocsEnum.FLAG_NONE));
					AssertDocsEnum(leftDocs = leftTermsEnum.Docs(randomBits, leftDocs, DocsEnum.FLAG_NONE
						), rightDocs = rightTermsEnum.Docs(randomBits, rightDocs, DocsEnum.FLAG_NONE));
					// with freqs:
					AssertDocsSkipping(leftTermsEnum.DocFreq(), leftDocs = leftTermsEnum.Docs(null, leftDocs
						), rightDocs = rightTermsEnum.Docs(null, rightDocs));
					AssertDocsSkipping(leftTermsEnum.DocFreq(), leftDocs = leftTermsEnum.Docs(randomBits
						, leftDocs), rightDocs = rightTermsEnum.Docs(randomBits, rightDocs));
					// w/o freqs:
					AssertDocsSkipping(leftTermsEnum.DocFreq(), leftDocs = leftTermsEnum.Docs(null, leftDocs
						, DocsEnum.FLAG_NONE), rightDocs = rightTermsEnum.Docs(null, rightDocs, DocsEnum
						.FLAG_NONE));
					AssertDocsSkipping(leftTermsEnum.DocFreq(), leftDocs = leftTermsEnum.Docs(randomBits
						, leftDocs, DocsEnum.FLAG_NONE), rightDocs = rightTermsEnum.Docs(randomBits, rightDocs
						, DocsEnum.FLAG_NONE));
				}
			}
			NUnit.Framework.Assert.IsNull(rightTermsEnum.Next());
		}

		/// <summary>checks term-level statistics</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void AssertTermStats(TermsEnum leftTermsEnum, TermsEnum rightTermsEnum
			)
		{
			NUnit.Framework.Assert.AreEqual(leftTermsEnum.DocFreq(), rightTermsEnum.DocFreq()
				);
			if (leftTermsEnum.TotalTermFreq() != -1 && rightTermsEnum.TotalTermFreq() != -1)
			{
				NUnit.Framework.Assert.AreEqual(leftTermsEnum.TotalTermFreq(), rightTermsEnum.TotalTermFreq
					());
			}
		}

		/// <summary>checks docs + freqs + positions + payloads, sequentially</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void AssertDocsAndPositionsEnum(DocsAndPositionsEnum leftDocs, DocsAndPositionsEnum
			 rightDocs)
		{
			if (leftDocs == null || rightDocs == null)
			{
				NUnit.Framework.Assert.IsNull(leftDocs);
				NUnit.Framework.Assert.IsNull(rightDocs);
				return;
			}
			NUnit.Framework.Assert.AreEqual(-1, leftDocs.DocID());
			NUnit.Framework.Assert.AreEqual(-1, rightDocs.DocID());
			int docid;
			while ((docid = leftDocs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
			{
				NUnit.Framework.Assert.AreEqual(docid, rightDocs.NextDoc());
				int freq = leftDocs.Freq();
				NUnit.Framework.Assert.AreEqual(freq, rightDocs.Freq());
				for (int i = 0; i < freq; i++)
				{
					NUnit.Framework.Assert.AreEqual(leftDocs.NextPosition(), rightDocs.NextPosition()
						);
				}
			}
			// we don't 
			//HM:revisit 
			//assert offsets/payloads, they are allowed to be different
			NUnit.Framework.Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, rightDocs.NextDoc(
				));
		}

		/// <summary>checks docs + freqs, sequentially</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void AssertDocsEnum(DocsEnum leftDocs, DocsEnum rightDocs)
		{
			if (leftDocs == null)
			{
				NUnit.Framework.Assert.IsNull(rightDocs);
				return;
			}
			NUnit.Framework.Assert.AreEqual(-1, leftDocs.DocID());
			NUnit.Framework.Assert.AreEqual(-1, rightDocs.DocID());
			int docid;
			while ((docid = leftDocs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
			{
				NUnit.Framework.Assert.AreEqual(docid, rightDocs.NextDoc());
			}
			// we don't 
			//HM:revisit 
			//assert freqs, they are allowed to be different
			NUnit.Framework.Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, rightDocs.NextDoc(
				));
		}

		/// <summary>checks advancing docs</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void AssertDocsSkipping(int docFreq, DocsEnum leftDocs, DocsEnum rightDocs
			)
		{
			if (leftDocs == null)
			{
				NUnit.Framework.Assert.IsNull(rightDocs);
				return;
			}
			int docid = -1;
			int averageGap = MAXDOC / (1 + docFreq);
			int skipInterval = 16;
			while (true)
			{
				if (Random().NextBoolean())
				{
					// nextDoc()
					docid = leftDocs.NextDoc();
					NUnit.Framework.Assert.AreEqual(docid, rightDocs.NextDoc());
				}
				else
				{
					// advance()
					int skip = docid + (int)Math.Ceil(Math.Abs(skipInterval + Random().NextGaussian()
						 * averageGap));
					docid = leftDocs.Advance(skip);
					NUnit.Framework.Assert.AreEqual(docid, rightDocs.Advance(skip));
				}
				if (docid == DocIdSetIterator.NO_MORE_DOCS)
				{
					return;
				}
			}
		}

		// we don't 
		//HM:revisit 
		//assert freqs, they are allowed to be different
		/// <summary>checks advancing docs + positions</summary>
		/// <exception cref="System.Exception"></exception>
		public virtual void AssertPositionsSkipping(int docFreq, DocsAndPositionsEnum leftDocs
			, DocsAndPositionsEnum rightDocs)
		{
			if (leftDocs == null || rightDocs == null)
			{
				NUnit.Framework.Assert.IsNull(leftDocs);
				NUnit.Framework.Assert.IsNull(rightDocs);
				return;
			}
			int docid = -1;
			int averageGap = MAXDOC / (1 + docFreq);
			int skipInterval = 16;
			while (true)
			{
				if (Random().NextBoolean())
				{
					// nextDoc()
					docid = leftDocs.NextDoc();
					NUnit.Framework.Assert.AreEqual(docid, rightDocs.NextDoc());
				}
				else
				{
					// advance()
					int skip = docid + (int)Math.Ceil(Math.Abs(skipInterval + Random().NextGaussian()
						 * averageGap));
					docid = leftDocs.Advance(skip);
					NUnit.Framework.Assert.AreEqual(docid, rightDocs.Advance(skip));
				}
				if (docid == DocIdSetIterator.NO_MORE_DOCS)
				{
					return;
				}
				int freq = leftDocs.Freq();
				NUnit.Framework.Assert.AreEqual(freq, rightDocs.Freq());
				for (int i = 0; i < freq; i++)
				{
					NUnit.Framework.Assert.AreEqual(leftDocs.NextPosition(), rightDocs.NextPosition()
						);
				}
			}
		}

		private class RandomBits : Bits
		{
			internal FixedBitSet bits;

			internal RandomBits(int maxDoc, double pctLive, Random random)
			{
				// we don't compare the payloads, its allowed that one is empty etc
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
	}
}
