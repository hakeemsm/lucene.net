/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Index;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Search;
using Lucene.Net.TestFramework.Util;
using Lucene.Net.TestFramework.Util.Automaton;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Index
{
	/// <summary>
	/// A
	/// <see cref="FilterAtomicReader">FilterAtomicReader</see>
	/// that can be used to apply
	/// additional checks for tests.
	/// </summary>
	public class AssertingAtomicReader : FilterAtomicReader
	{
		public AssertingAtomicReader(AtomicReader @in) : base(@in)
		{
		}

		//HM:revist. all asserts commented out
		// check some basic reader sanity
		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.TestFramework.Index.Fields GetTermVectors(int docID)
		{
			Lucene.Net.TestFramework.Index.Fields fields = base.GetTermVectors(docID);
			return fields == null ? null : new AssertingAtomicReader.AssertingFields(fields);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.TestFramework.Index.Fields Fields()
		{
			Lucene.Net.TestFramework.Index.Fields fields = base.Fields();
			return fields == null ? null : new AssertingAtomicReader.AssertingFields(fields);
		}

		/// <summary>Wraps a Fields but with additional asserts</summary>
		public class AssertingFields : FilterAtomicReader.FilterFields
		{
			public AssertingFields(Fields @in) : base(@in)
			{
			}

			public override Sharpen.Iterator<string> Iterator()
			{
				Sharpen.Iterator<string> iterator = base.Iterator();
				////
				 
				//assert iterator != null;
				return iterator;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Lucene.Net.TestFramework.Index.Terms Terms(string field)
			{
				Lucene.Net.TestFramework.Index.Terms terms = base.Terms(field);
				return terms == null ? null : new AssertingAtomicReader.AssertingTerms(terms);
			}
		}

		/// <summary>Wraps a Terms but with additional asserts</summary>
		public class AssertingTerms : FilterAtomicReader.FilterTerms
		{
			public AssertingTerms(Terms @in) : base(@in)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override TermsEnum Intersect(CompiledAutomaton automaton, BytesRef bytes)
			{
				TermsEnum termsEnum = @in.Intersect(automaton, bytes);
				return new AssertingAtomicReader.AssertingTermsEnum(termsEnum);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override TermsEnum Iterator(TermsEnum reuse)
			{
				// TODO: should we give this thing a random to be super-evil,
				// and randomly *not* unwrap?
				if (reuse is AssertingAtomicReader.AssertingTermsEnum)
				{
					reuse = ((AssertingAtomicReader.AssertingTermsEnum)reuse).@in;
				}
				TermsEnum termsEnum = base.Iterator(reuse);
				////
				 
				//assert termsEnum != null;
				return new AssertingAtomicReader.AssertingTermsEnum(termsEnum);
			}
		}

		internal class AssertingTermsEnum : FilterAtomicReader.FilterTermsEnum
		{
			private enum State
			{
				INITIAL,
				POSITIONED,
				UNPOSITIONED
			}

			private AssertingAtomicReader.AssertingTermsEnum.State state = AssertingAtomicReader.AssertingTermsEnum.State
				.INITIAL;

			public AssertingTermsEnum(TermsEnum @in) : base(@in)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
			{
				////
				 
				//assert state == State.POSITIONED: "docs(...) called on unpositioned TermsEnum";
				// TODO: should we give this thing a random to be super-evil,
				// and randomly *not* unwrap?
				if (reuse is AssertingAtomicReader.AssertingDocsEnum)
				{
					reuse = ((AssertingAtomicReader.AssertingDocsEnum)reuse).@in;
				}
				DocsEnum docs = base.Docs(liveDocs, reuse, flags);
				return docs == null ? null : new AssertingAtomicReader.AssertingDocsEnum(docs);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum
				 reuse, int flags)
			{
				////
				 
				//assert state == State.POSITIONED: "docsAndPositions(...) called on unpositioned TermsEnum";
				// TODO: should we give this thing a random to be super-evil,
				// and randomly *not* unwrap?
				if (reuse is AssertingAtomicReader.AssertingDocsAndPositionsEnum)
				{
					reuse = ((AssertingAtomicReader.AssertingDocsAndPositionsEnum)reuse).@in;
				}
				DocsAndPositionsEnum docs = base.DocsAndPositions(liveDocs, reuse, flags);
				return docs == null ? null : new AssertingAtomicReader.AssertingDocsAndPositionsEnum
					(docs);
			}

			// TODO: we should separately track if we are 'at the end' ?
			// someone should not call next() after it returns null!!!!
			/// <exception cref="System.IO.IOException"></exception>
			public override BytesRef Next()
			{
				////
				 
				//assert state == State.INITIAL || state == State.POSITIONED: "next() called on unpositioned TermsEnum";
				BytesRef result = base.Next();
				if (result == null)
				{
					state = AssertingAtomicReader.AssertingTermsEnum.State.UNPOSITIONED;
				}
				else
				{
					////
					 
					//assert result.isValid();
					state = AssertingAtomicReader.AssertingTermsEnum.State.POSITIONED;
				}
				return result;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override long Ord()
			{
				////
				 
				//assert state == State.POSITIONED : "ord() called on unpositioned TermsEnum";
				return base.Ord();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int DocFreq()
			{
				////
				 
				//assert state == State.POSITIONED : "docFreq() called on unpositioned TermsEnum";
				return base.DocFreq();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override long TotalTermFreq()
			{
				////
				 
				//assert state == State.POSITIONED : "totalTermFreq() called on unpositioned TermsEnum";
				return base.TotalTermFreq();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override BytesRef Term()
			{
				////
				 
				//assert state == State.POSITIONED : "term() called on unpositioned TermsEnum";
				BytesRef ret = base.Term();
				////
				 
				//assert ret == null || ret.isValid();
				return ret;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SeekExact(long ord)
			{
				base.SeekExact(ord);
				state = AssertingAtomicReader.AssertingTermsEnum.State.POSITIONED;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override TermsEnum.SeekStatus SeekCeil(BytesRef term)
			{
				////
				 
				//assert term.isValid();
				TermsEnum.SeekStatus result = base.SeekCeil(term);
				if (result == TermsEnum.SeekStatus.END)
				{
					state = AssertingAtomicReader.AssertingTermsEnum.State.UNPOSITIONED;
				}
				else
				{
					state = AssertingAtomicReader.AssertingTermsEnum.State.POSITIONED;
				}
				return result;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override bool SeekExact(BytesRef text)
			{
				////
				 
				//assert text.isValid();
				if (base.SeekExact(text))
				{
					state = AssertingAtomicReader.AssertingTermsEnum.State.POSITIONED;
					return true;
				}
				else
				{
					state = AssertingAtomicReader.AssertingTermsEnum.State.UNPOSITIONED;
					return false;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Lucene.Net.TestFramework.Index.TermState TermState()
			{
				////
				 
				//assert state == State.POSITIONED : "termState() called on unpositioned TermsEnum";
				return base.TermState();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SeekExact(BytesRef term, Lucene.Net.TestFramework.Index.TermState state
				)
			{
				////
				 
				//assert term.isValid();
				base.SeekExact(term, state);
				this.state = AssertingAtomicReader.AssertingTermsEnum.State.POSITIONED;
			}
		}

		internal enum DocsEnumState
		{
			START,
			ITERATING,
			FINISHED
		}

		/// <summary>Wraps a docsenum with additional checks</summary>
		public class AssertingDocsEnum : FilterAtomicReader.FilterDocsEnum
		{
			private AssertingAtomicReader.DocsEnumState state = AssertingAtomicReader.DocsEnumState
				.START;

			private int doc;

			public AssertingDocsEnum(DocsEnum @in) : this(@in, true)
			{
			}

			public AssertingDocsEnum(DocsEnum @in, bool failOnUnsupportedDocID) : base(@in)
			{
				try
				{
					int docid = @in.DocID();
				}
				catch (NotSupportedException e)
				{
					////
					 
					//assert docid == -1 : in.getClass() + ": invalid initial doc id: " + docid;
					if (failOnUnsupportedDocID)
					{
						throw;
					}
				}
				doc = -1;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextDoc()
			{
				////
				 
				//assert state != DocsEnumState.FINISHED : "nextDoc() called after NO_MORE_DOCS";
				int nextDoc = base.NextDoc();
				////
				 
				//assert nextDoc > doc : "backwards nextDoc from " + doc + " to " + nextDoc + " " + in;
				if (nextDoc == DocIdSetIterator.NO_MORE_DOCS)
				{
					state = AssertingAtomicReader.DocsEnumState.FINISHED;
				}
				else
				{
					state = AssertingAtomicReader.DocsEnumState.ITERATING;
				}
				//
				 
				//assert super.docID() == nextDoc;
				return doc = nextDoc;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				int advanced = base.Advance(target);
				////
				 
				//assert advanced >= target : "backwards advance from: " + target + " to: " + advanced;
				if (advanced == DocIdSetIterator.NO_MORE_DOCS)
				{
					state = AssertingAtomicReader.DocsEnumState.FINISHED;
				}
				else
				{
					state = AssertingAtomicReader.DocsEnumState.ITERATING;
				}
				////
				 
				//assert super.docID() == advanced;
				return doc = advanced;
			}

			public override int DocID()
			{
				////
				 
				//assert doc == super.docID() : " invalid docID() in " + in.getClass() + " " + super.docID() + " instead of " + doc;
				return doc;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Freq()
			{
				int freq = base.Freq();
				//
				 
				//assert freq > 0;
				return freq;
			}
		}

		internal class AssertingDocsAndPositionsEnum : FilterAtomicReader.FilterDocsAndPositionsEnum
		{
			private AssertingAtomicReader.DocsEnumState state = AssertingAtomicReader.DocsEnumState
				.START;

			private int positionMax = 0;

			private int positionCount = 0;

			private int doc;

			public AssertingDocsAndPositionsEnum(DocsAndPositionsEnum @in) : base(@in)
			{
				int docid = @in.DocID();
				////
				 
				//assert docid == -1 : "invalid initial doc id: " + docid;
				doc = -1;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextDoc()
			{
				////
				 
				//assert state != DocsEnumState.FINISHED : "nextDoc() called after NO_MORE_DOCS";
				int nextDoc = base.NextDoc();
				////
				 
				//assert nextDoc > doc : "backwards nextDoc from " + doc + " to " + nextDoc;
				positionCount = 0;
				if (nextDoc == DocIdSetIterator.NO_MORE_DOCS)
				{
					state = AssertingAtomicReader.DocsEnumState.FINISHED;
					positionMax = 0;
				}
				else
				{
					state = AssertingAtomicReader.DocsEnumState.ITERATING;
					positionMax = base.Freq();
				}
				////
				 
				//assert super.docID() == nextDoc;
				return doc = nextDoc;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				int advanced = base.Advance(target);
				////
				 
				//assert advanced >= target : "backwards advance from: " + target + " to: " + advanced;
				positionCount = 0;
				if (advanced == DocIdSetIterator.NO_MORE_DOCS)
				{
					state = AssertingAtomicReader.DocsEnumState.FINISHED;
					positionMax = 0;
				}
				else
				{
					state = AssertingAtomicReader.DocsEnumState.ITERATING;
					positionMax = base.Freq();
				}
				////
				 
				//assert super.docID() == advanced;
				return doc = advanced;
			}

			public override int DocID()
			{
				////
				 
				//assert doc == super.docID() : " invalid docID() in " + in.getClass() + " " + super.docID() + " instead of " + doc;
				return doc;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Freq()
			{
				int freq = base.Freq();
				////
				 
				//assert freq > 0;
				return freq;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextPosition()
			{
				int position = base.NextPosition();
				////
				 
				//assert position >= 0 || position == -1 : "invalid position: " + position;
				positionCount++;
				return position;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int StartOffset()
			{
				return base.StartOffset();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int EndOffset()
			{
				return base.EndOffset();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override BytesRef GetPayload()
			{
				BytesRef payload = base.GetPayload();
				////
				 
				//assert payload == null || payload.isValid() && payload.length > 0 : "getPayload() returned payload with invalid length!";
				return payload;
			}
		}

		/// <summary>Wraps a NumericDocValues but with additional asserts</summary>
		public class AssertingNumericDocValues : NumericDocValues
		{
			private readonly NumericDocValues @in;

			private readonly int maxDoc;

			public AssertingNumericDocValues(NumericDocValues @in, int maxDoc)
			{
				this.@in = @in;
				this.maxDoc = maxDoc;
			}

			public override long Get(int docID)
			{
				////
				 
				//assert docID >= 0 && docID < maxDoc;
				return @in.Get(docID);
			}
		}

		/// <summary>Wraps a BinaryDocValues but with additional asserts</summary>
		public class AssertingBinaryDocValues : BinaryDocValues
		{
			private readonly BinaryDocValues @in;

			private readonly int maxDoc;

			public AssertingBinaryDocValues(BinaryDocValues @in, int maxDoc)
			{
				this.@in = @in;
				this.maxDoc = maxDoc;
			}

			public override void Get(int docID, BytesRef result)
			{
				@in.Get(docID, result);
			}
			////
			 
			//assert result.isValid();
		}

		/// <summary>Wraps a SortedDocValues but with additional asserts</summary>
		public class AssertingSortedDocValues : SortedDocValues
		{
			private readonly SortedDocValues @in;

			private readonly int maxDoc;

			private readonly int valueCount;

			public AssertingSortedDocValues(SortedDocValues @in, int maxDoc)
			{
				this.@in = @in;
				this.maxDoc = maxDoc;
				this.valueCount = @in.GetValueCount();
			}

			////
			 
			//assert valueCount >= 0 && valueCount <= maxDoc;
			public override int GetOrd(int docID)
			{
				////
				 
				//assert docID >= 0 && docID < maxDoc;
				int ord = @in.GetOrd(docID);
				////
				 
				//assert ord >= -1 && ord < valueCount;
				return ord;
			}

			public override void LookupOrd(int ord, BytesRef result)
			{
				@in.LookupOrd(ord, result);
			}

			////
			 
			//assert result.isValid();
			public override int GetValueCount()
			{
				int valueCount = @in.GetValueCount();
				////
				 
				//assert valueCount == this.valueCount; // should not change
				return valueCount;
			}

			public override void Get(int docID, BytesRef result)
			{
				@in.Get(docID, result);
			}

			////
			 
			//assert result.isValid();
			public override int LookupTerm(BytesRef key)
			{
				////
				 
				//assert key.isValid();
				int result = @in.LookupTerm(key);
				return result;
			}
		}

		/// <summary>Wraps a SortedSetDocValues but with additional asserts</summary>
		public class AssertingSortedSetDocValues : SortedSetDocValues
		{
			private readonly SortedSetDocValues @in;

			private readonly int maxDoc;

			private readonly long valueCount;

			internal long lastOrd = NO_MORE_ORDS;

			public AssertingSortedSetDocValues(SortedSetDocValues @in, int maxDoc)
			{
				this.@in = @in;
				this.maxDoc = maxDoc;
				this.valueCount = @in.GetValueCount();
			}

			//
			 
			//assert valueCount >= 0;
			public override long NextOrd()
			{
				////
				 
				//assert lastOrd != NO_MORE_ORDS;
				long ord = @in.NextOrd();
				lastOrd = ord;
				return ord;
			}

			public override void SetDocument(int docID)
			{
				////
				 
				//assert docID >= 0 && docID < maxDoc : "docid=" + docID + ",maxDoc=" + maxDoc;
				@in.SetDocument(docID);
				lastOrd = -2;
			}

			public override void LookupOrd(long ord, BytesRef result)
			{
				@in.LookupOrd(ord, result);
			}

			//
			 
			//assert result.isValid();
			public override long GetValueCount()
			{
				long valueCount = @in.GetValueCount();
				//
				 
				//assert valueCount == this.valueCount; // should not change
				return valueCount;
			}

			public override long LookupTerm(BytesRef key)
			{
				//
				 
				//assert key.isValid();
				long result = @in.LookupTerm(key);
				//
				 
				//assert result < valueCount;
				//
				 
				//assert key.isValid();
				return result;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override NumericDocValues GetNumericDocValues(string field)
		{
			NumericDocValues dv = base.GetNumericDocValues(field);
			FieldInfo fi = GetFieldInfos().FieldInfo(field);
			if (dv != null)
			{
				//
				 
				//assert fi != null;
				//
				 
				//assert fi.getDocValuesType() == FieldInfo.DocValuesType.NUMERIC;
				return new AssertingAtomicReader.AssertingNumericDocValues(dv, MaxDoc());
			}
			else
			{
				////
				 
				//assert fi == null || fi.getDocValuesType() != FieldInfo.DocValuesType.NUMERIC;
				return null;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override BinaryDocValues GetBinaryDocValues(string field)
		{
			BinaryDocValues dv = base.GetBinaryDocValues(field);
			FieldInfo fi = GetFieldInfos().FieldInfo(field);
			if (dv != null)
			{
				//
				 
				//assert fi != null;
				//
				 
				//assert fi.getDocValuesType() == FieldInfo.DocValuesType.BINARY;
				return new AssertingAtomicReader.AssertingBinaryDocValues(dv, MaxDoc());
			}
			else
			{
				////
				 
				//assert fi == null || fi.getDocValuesType() != FieldInfo.DocValuesType.BINARY;
				return null;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override SortedDocValues GetSortedDocValues(string field)
		{
			SortedDocValues dv = base.GetSortedDocValues(field);
			FieldInfo fi = GetFieldInfos().FieldInfo(field);
			if (dv != null)
			{
				//
				 
				//assert fi != null;
				//
				 
				//assert fi.getDocValuesType() == FieldInfo.DocValuesType.SORTED;
				return new AssertingAtomicReader.AssertingSortedDocValues(dv, MaxDoc());
			}
			else
			{
				//
				 
				//assert fi == null || fi.getDocValuesType() != FieldInfo.DocValuesType.SORTED;
				return null;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override SortedSetDocValues GetSortedSetDocValues(string field)
		{
			SortedSetDocValues dv = base.GetSortedSetDocValues(field);
			FieldInfo fi = GetFieldInfos().FieldInfo(field);
			if (dv != null)
			{
				//
				 
				//assert fi != null;
				//
				 
				//assert fi.getDocValuesType() == FieldInfo.DocValuesType.SORTED_SET;
				return new AssertingAtomicReader.AssertingSortedSetDocValues(dv, MaxDoc());
			}
			else
			{
				//
				 
				//assert fi == null || fi.getDocValuesType() != FieldInfo.DocValuesType.SORTED_SET;
				return null;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override NumericDocValues GetNormValues(string field)
		{
			NumericDocValues dv = base.GetNormValues(field);
			FieldInfo fi = GetFieldInfos().FieldInfo(field);
			if (dv != null)
			{
				//
				 
				//assert fi != null;
				//
				 
				//assert fi.hasNorms();
				return new AssertingAtomicReader.AssertingNumericDocValues(dv, MaxDoc());
			}
			else
			{
				//
				 
				//assert fi == null || fi.hasNorms() == false;
				return null;
			}
		}

		/// <summary>Wraps a Bits but with additional asserts</summary>
		public class AssertingBits : IBits
		{
			internal readonly IBits @in;

			public AssertingBits(IBits @in)
			{
				this.@in = @in;
			}

			public bool this[int index]
			{
			    get
			    {

			        //assert index >= 0 && index < length();
			        return @in[index];
			    }
			}

			public int Length
			{
			    get { return @in.Length; }
			}
		}

		public override Bits GetLiveDocs()
		{
			Bits liveDocs = base.GetLiveDocs();
			if (liveDocs != null)
			{
				//
				 
				//assert maxDoc() == liveDocs.length();
				liveDocs = new AssertingAtomicReader.AssertingBits(liveDocs);
			}
			//
			 
			//assert maxDoc() == numDocs();
			//
			 
			//assert !hasDeletions();
			return liveDocs;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Bits GetDocsWithField(string field)
		{
			Bits docsWithField = base.GetDocsWithField(field);
			FieldInfo fi = GetFieldInfos().FieldInfo(field);
			if (docsWithField != null)
			{
				//
				 
				//assert fi != null;
				//
				 
				//assert fi.hasDocValues();
				//
				 
				//assert maxDoc() == docsWithField.length();
				docsWithField = new AssertingAtomicReader.AssertingBits(docsWithField);
			}
			//
			 
			//assert fi == null || fi.hasDocValues() == false;
			return docsWithField;
		}

		// this is the same hack as FCInvisible
		public override object GetCoreCacheKey()
		{
			return cacheKey;
		}

		public override object GetCombinedCoreAndDeletesKey()
		{
			return cacheKey;
		}

		private readonly object cacheKey = new object();
	}
}
