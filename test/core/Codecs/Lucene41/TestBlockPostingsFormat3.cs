using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Codecs.Lucene41;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.TestFramework.Util.Automaton;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;

namespace Lucene.Net.Test.Codecs.Lucene41
{
	/// <summary>Tests partial enumeration (only pulling a subset of the indexed data)</summary>
	[TestFixture]
    public class TestBlockPostingsFormat3 : LuceneTestCase
	{
		internal const int MAXDOC = Lucene41PostingsFormat.BLOCK_SIZE * 20;

		// creates 8 fields with different options and does "duels" of fields against each other
		
        [Test]
		public virtual void TestIndexes()
		{
			var dir = NewDirectory();
			Analyzer analyzer = new AnonymousAnalyzer1(Analyzer.PER_FIELD_REUSE_STRATEGY);
			IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer);
			iwc.SetCodec(TestUtil.AlwaysPostingsFormat(new Lucene41PostingsFormat()));
			// TODO we could actually add more fields implemented with different PFs
			// or, just put this test into the usual rotation?
			var iw = new RandomIndexWriter(Random(), dir, (IndexWriterConfig) iwc.Clone());
			var doc = new Lucene.Net.Documents.Document();
			var docsOnlyType = new FieldType(TextField.TYPE_NOT_STORED)
			{
			    StoreTermVectors = true,
			    IndexOptions = FieldInfo.IndexOptions.DOCS_ONLY
			};
			// turn this on for a cross-check
            var docsAndFreqsType = new FieldType(TextField.TYPE_NOT_STORED)
			{
			    StoreTermVectors = true,
			    IndexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS
			};
			// turn this on for a cross-check
            var positionsType = new FieldType(TextField.TYPE_NOT_STORED)
			{
			    StoreTermVectors = true,
			    StoreTermVectorPositions = true,
			    StoreTermVectorOffsets = true,
			    StoreTermVectorPayloads = true
			};
			// turn these on for a cross-check
            var offsetsType = new FieldType(positionsType)
            {
                IndexOptions = FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
            };
            Field field1 = new Field("field1docs", string.Empty, docsOnlyType);
			Field field2 = new Field("field2freqs", string.Empty, docsAndFreqsType);
			Field field3 = new Field("field3positions", string.Empty, positionsType);
			Field field4 = new Field("field4offsets", string.Empty, offsetsType);
			Field field5 = new Field("field5payloadsFixed", string.Empty, positionsType);
			Field field6 = new Field("field6payloadsVariable", string.Empty, positionsType);
			Field field7 = new Field("field7payloadsFixedOffsets", string.Empty, offsetsType);
			Field field8 = new Field("field8payloadsVariableOffsets", string.Empty, offsetsType);
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
				string stringValue = i + " verycommon " + English.IntToEnglish(i).Replace('-', ' ') + " " + TestUtil.RandomSimpleString(Random());
				field1.StringValue = stringValue;
				field2.StringValue = stringValue;
				field3.StringValue = stringValue;
				field4.StringValue = stringValue;
				field5.StringValue = stringValue;
				field6.StringValue = stringValue;
				field7.StringValue = stringValue;
				field8.StringValue = stringValue;
				iw.AddDocument(doc);
			}
			iw.Dispose();
			Verify(dir);
			TestUtil.CheckIndex(dir);
			// for some extra coverage, checkIndex before we forceMerge
			iwc.SetOpenMode(IndexWriterConfig.OpenMode.APPEND);
			IndexWriter iw2 = new IndexWriter(dir, (IndexWriterConfig) iwc.Clone());
			iw2.ForceMerge(1);
			iw2.Dispose();
			Verify(dir);
			dir.Dispose();
		}

		private sealed class AnonymousAnalyzer1 : Analyzer
		{
			public AnonymousAnalyzer1(ReuseStrategy baseArg1) : base(baseArg1)
			{
			}

		    public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
			{
				Tokenizer tokenizer = new MockTokenizer(reader);
				if (fieldName.Contains("payloadsFixed"))
				{
					TokenFilter filter = new MockFixedLengthPayloadFilter(new Random(0), tokenizer, 1);
					return new TokenStreamComponents(tokenizer, filter);
				}
		        if (fieldName.Contains("payloadsVariable"))
		        {
		            TokenFilter filter = new MockVariableLengthPayloadFilter(new Random(0), tokenizer);
		            return new TokenStreamComponents(tokenizer, filter);
		        }
		        return new TokenStreamComponents(tokenizer);
			}
		}

		/// <exception cref="System.Exception"></exception>
		private void Verify(Lucene.Net.Store.Directory dir)
		{
			DirectoryReader ir = DirectoryReader.Open(dir);
			foreach (AtomicReaderContext leaf in ir.Leaves)
			{
				AtomicReader leafReader = ((AtomicReader)leaf.Reader);
				AssertTerms(leafReader.Terms("field1docs"), leafReader.Terms("field2freqs"), true);
				AssertTerms(leafReader.Terms("field3positions"), leafReader.Terms("field4offsets"), true);
				AssertTerms(leafReader.Terms("field4offsets"), leafReader.Terms("field5payloadsFixed"), true);
				AssertTerms(leafReader.Terms("field5payloadsFixed"), leafReader.Terms("field6payloadsVariable"), true);
				AssertTerms(leafReader.Terms("field6payloadsVariable"), leafReader.Terms("field7payloadsFixedOffsets"), true);
				AssertTerms(leafReader.Terms("field7payloadsFixedOffsets"), leafReader.Terms("field8payloadsVariableOffsets"), true);
			}
			ir.Dispose();
		}

		// following code is almost an exact dup of code from TestDuelingCodecs: sorry!
		
		public virtual void AssertTerms(Terms leftTerms, Terms rightTerms, bool deep)
		{
			if (leftTerms == null || rightTerms == null)
			{
				IsNull(leftTerms);
				IsNull(rightTerms);
				return;
			}
			AssertTermsStatistics(leftTerms, rightTerms);
			// NOTE: we don't 
			
			//assert hasOffsets/hasPositions/hasPayloads because they are allowed to be different
			TermsEnum leftTermsEnum = leftTerms.IEnumerator(null);
			TermsEnum rightTermsEnum = rightTerms.IEnumerator(null);
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
				leftEnum = leftTerms.IEnumerator(leftEnum);
				BytesRef term = null;
				while ((term = leftEnum.Next()) != null)
				{
					int code = random.Next(10);
					if (code == 0)
					{
						// the term
						tests.Add(BytesRef.DeepCopyOf(term));
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
								var newbytes = new sbyte[term.length + 5];
								System.Array.Copy(term.bytes, term.offset, newbytes, 5, term.length);
								tests.Add(new BytesRef(newbytes, 5, term.length));
							}
						}
					}
				}
				numPasses++;
			}
			var shuffledTests = new List<BytesRef>(tests);
			shuffledTests.Shuffle(random);
			foreach (BytesRef b in shuffledTests)
			{
				leftEnum = leftTerms.IEnumerator(leftEnum);
				rightEnum = rightTerms.IEnumerator(rightEnum);
				AreEqual(leftEnum.SeekExact(b), rightEnum.SeekExact(b));
				AreEqual(leftEnum.SeekExact(b), rightEnum.SeekExact(b));
				TermsEnum.SeekStatus leftStatus;
				TermsEnum.SeekStatus rightStatus;
				leftStatus = leftEnum.SeekCeil(b);
				rightStatus = rightEnum.SeekCeil(b);
				AreEqual(leftStatus, rightStatus);
				if (leftStatus != TermsEnum.SeekStatus.END)
				{
					AreEqual(leftEnum.Term, rightEnum.Term);
				}
				leftStatus = leftEnum.SeekCeil(b);
				rightStatus = rightEnum.SeekCeil(b);
				AreEqual(leftStatus, rightStatus);
				if (leftStatus != TermsEnum.SeekStatus.END)
				{
					AreEqual(leftEnum.Term, rightEnum.Term);
				}
			}
		}

		/// <summary>checks collection-level statistics on Terms</summary>
		
		public virtual void AssertTermsStatistics(Terms leftTerms, Terms rightTerms)
		{
			 
			//assert leftTerms.getComparator() == rightTerms.getComparator();
			if (leftTerms.DocCount != -1 && rightTerms.DocCount != -1)
			{
				AreEqual(leftTerms.DocCount, rightTerms.DocCount);
			}
			if (leftTerms.SumDocFreq != -1 && rightTerms.SumDocFreq != -1)
			{
				AreEqual(leftTerms.SumDocFreq, rightTerms.SumDocFreq);
			}
			if (leftTerms.SumTotalTermFreq != -1 && rightTerms.SumTotalTermFreq != -1)
			{
				AreEqual(leftTerms.SumTotalTermFreq, rightTerms.SumTotalTermFreq);
			}
			if (leftTerms.Size != -1 && rightTerms.Size != -1)
			{
				AreEqual(leftTerms.Size, rightTerms.Size);
			}
		}

		/// <summary>
		/// checks the terms enum sequentially
		/// if deep is false, it does a 'shallow' test that doesnt go down to the docsenums
		/// </summary>
		
		public virtual void AssertTermsEnum(TermsEnum leftTermsEnum, TermsEnum rightTermsEnum, bool deep)
		{
			BytesRef term;
			IBits randomBits = new RandomBits(MAXDOC, Random().NextDouble(), Random());
			DocsAndPositionsEnum leftPositions = null;
			DocsAndPositionsEnum rightPositions = null;
			DocsEnum leftDocs = null;
			DocsEnum rightDocs = null;
			while ((term = leftTermsEnum.Next()) != null)
			{
				AreEqual(term, rightTermsEnum.Next());
				AssertTermStats(leftTermsEnum, rightTermsEnum);
				if (deep)
				{
					// with payloads + off
					AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.DocsAndPositions(null, leftPositions
						), rightPositions = rightTermsEnum.DocsAndPositions(null, rightPositions));
					AssertDocsAndPositionsEnum(leftPositions = leftTermsEnum.DocsAndPositions(randomBits
						, leftPositions), rightPositions = rightTermsEnum.DocsAndPositions(randomBits, rightPositions
						));
					AssertPositionsSkipping(leftTermsEnum.DocFreq, leftPositions = leftTermsEnum.DocsAndPositions
						(null, leftPositions), rightPositions = rightTermsEnum.DocsAndPositions(null, rightPositions
						));
					AssertPositionsSkipping(leftTermsEnum.DocFreq, leftPositions = leftTermsEnum.DocsAndPositions
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
					AssertPositionsSkipping(leftTermsEnum.DocFreq, leftPositions = leftTermsEnum.DocsAndPositions
						(null, leftPositions, DocsAndPositionsEnum.FLAG_PAYLOADS), rightPositions = rightTermsEnum
						.DocsAndPositions(null, rightPositions, DocsAndPositionsEnum.FLAG_PAYLOADS));
					AssertPositionsSkipping(leftTermsEnum.DocFreq, leftPositions = leftTermsEnum.DocsAndPositions
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
					AssertPositionsSkipping(leftTermsEnum.DocFreq, leftPositions = leftTermsEnum.DocsAndPositions
						(null, leftPositions, DocsAndPositionsEnum.FLAG_OFFSETS), rightPositions = rightTermsEnum
						.DocsAndPositions(null, rightPositions, DocsAndPositionsEnum.FLAG_OFFSETS));
					AssertPositionsSkipping(leftTermsEnum.DocFreq, leftPositions = leftTermsEnum.DocsAndPositions
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
					AssertPositionsSkipping(leftTermsEnum.DocFreq, leftPositions = leftTermsEnum.DocsAndPositions
						(null, leftPositions, DocsEnum.FLAG_NONE), rightPositions = rightTermsEnum.DocsAndPositions
						(null, rightPositions, DocsEnum.FLAG_NONE));
					AssertPositionsSkipping(leftTermsEnum.DocFreq, leftPositions = leftTermsEnum.DocsAndPositions
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
					AssertDocsSkipping(leftTermsEnum.DocFreq, leftDocs = leftTermsEnum.Docs(null, leftDocs
						), rightDocs = rightTermsEnum.Docs(null, rightDocs));
					AssertDocsSkipping(leftTermsEnum.DocFreq, leftDocs = leftTermsEnum.Docs(randomBits
						, leftDocs), rightDocs = rightTermsEnum.Docs(randomBits, rightDocs));
					// w/o freqs:
					AssertDocsSkipping(leftTermsEnum.DocFreq, leftDocs = leftTermsEnum.Docs(null, leftDocs
						, DocsEnum.FLAG_NONE), rightDocs = rightTermsEnum.Docs(null, rightDocs, DocsEnum
						.FLAG_NONE));
					AssertDocsSkipping(leftTermsEnum.DocFreq, leftDocs = leftTermsEnum.Docs(randomBits
						, leftDocs, DocsEnum.FLAG_NONE), rightDocs = rightTermsEnum.Docs(randomBits, rightDocs
						, DocsEnum.FLAG_NONE));
				}
			}
			IsNull(rightTermsEnum.Next());
		}

		/// <summary>checks term-level statistics</summary>
		
		public virtual void AssertTermStats(TermsEnum leftTermsEnum, TermsEnum rightTermsEnum)
		{
			AreEqual(leftTermsEnum.DocFreq, rightTermsEnum.DocFreq);
			if (leftTermsEnum.TotalTermFreq != -1 && rightTermsEnum.TotalTermFreq != -1)
			{
				AreEqual(leftTermsEnum.TotalTermFreq, rightTermsEnum.TotalTermFreq);
			}
		}

		/// <summary>checks docs + freqs + positions + payloads, sequentially</summary>
		
		public virtual void AssertDocsAndPositionsEnum(DocsAndPositionsEnum leftDocs, DocsAndPositionsEnum rightDocs)
		{
			if (leftDocs == null || rightDocs == null)
			{
				IsNull(leftDocs);
				IsNull(rightDocs);
				return;
			}
			AreEqual(-1, leftDocs.DocID);
			AreEqual(-1, rightDocs.DocID);
			int docid;
			while ((docid = leftDocs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
			{
				AreEqual(docid, rightDocs.NextDoc());
				int freq = leftDocs.Freq;
				Assert.AreEqual(freq, rightDocs.Freq);
				for (int i = 0; i < freq; i++)
				{
					AreEqual(leftDocs.NextPosition(), rightDocs.NextPosition());
				}
			}
			// we don't 
			
			//assert offsets/payloads, they are allowed to be different
			AreEqual(DocIdSetIterator.NO_MORE_DOCS, rightDocs.NextDoc(
				));
		}

		/// <summary>checks docs + freqs, sequentially</summary>
		
		public virtual void AssertDocsEnum(DocsEnum leftDocs, DocsEnum rightDocs)
		{
			if (leftDocs == null)
			{
				IsNull(rightDocs);
				return;
			}
			AreEqual(-1, leftDocs.DocID);
			AreEqual(-1, rightDocs.DocID);
			int docid;
			while ((docid = leftDocs.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
			{
				AreEqual(docid, rightDocs.NextDoc());
			}
			// we don't 
			
			//assert freqs, they are allowed to be different
			AreEqual(DocIdSetIterator.NO_MORE_DOCS, rightDocs.NextDoc());
		}

		/// <summary>checks advancing docs</summary>
		
		public virtual void AssertDocsSkipping(int docFreq, DocsEnum leftDocs, DocsEnum rightDocs)
		{
			if (leftDocs == null)
			{
				IsNull(rightDocs);
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
					AreEqual(docid, rightDocs.NextDoc());
				}
				else
				{
					// advance()
					int skip = docid + (int)Math.Ceiling(Math.Abs(skipInterval + Random().NextGaussian()
						 * averageGap));
					docid = leftDocs.Advance(skip);
					AreEqual(docid, rightDocs.Advance(skip));
				}
				if (docid == DocIdSetIterator.NO_MORE_DOCS)
				{
					return;
				}
			}
		}

		// we don't 
		
		//assert freqs, they are allowed to be different
		/// <summary>checks advancing docs + positions</summary>
		
		public virtual void AssertPositionsSkipping(int docFreq, DocsAndPositionsEnum leftDocs, DocsAndPositionsEnum rightDocs)
		{
			if (leftDocs == null || rightDocs == null)
			{
				IsNull(leftDocs);
				IsNull(rightDocs);
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
					AreEqual(docid, rightDocs.NextDoc());
				}
				else
				{
					// advance()
					int skip = docid + (int)Math.Ceiling(Math.Abs(skipInterval + Random().NextGaussian()
						 * averageGap));
					docid = leftDocs.Advance(skip);
					AreEqual(docid, rightDocs.Advance(skip));
				}
				if (docid == DocIdSetIterator.NO_MORE_DOCS)
				{
					return;
				}
				int freq = leftDocs.Freq;
				AreEqual(freq, rightDocs.Freq);
				for (int i = 0; i < freq; i++)
				{
					AreEqual(leftDocs.NextPosition(), rightDocs.NextPosition()
						);
				}
			}
		}

		private class RandomBits : IBits
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

			

		    public bool this[int index]
		    {
		        get { return bits[index]; }
		    }

		    int IBits.Length
		    {
		        get { return bits.Length; }
		    }
		}
	}
}
