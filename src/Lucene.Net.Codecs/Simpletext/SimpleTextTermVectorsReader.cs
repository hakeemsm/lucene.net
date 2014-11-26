/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Simpletext;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Simpletext
{
	/// <summary>Reads plain-text term vectors.</summary>
	/// <remarks>
	/// Reads plain-text term vectors.
	/// <p>
	/// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class SimpleTextTermVectorsReader : TermVectorsReader
	{
		private long offsets;

		private IndexInput @in;

		private BytesRef scratch = new BytesRef();

		private CharsRef scratchUTF16 = new CharsRef();

		/// <exception cref="System.IO.IOException"></exception>
		public SimpleTextTermVectorsReader(Directory directory, SegmentInfo si, IOContext
			 context)
		{
			bool success = false;
			try
			{
				@in = directory.OpenInput(IndexFileNames.SegmentFileName(si.name, string.Empty, VECTORS_EXTENSION
					), context);
				success = true;
			}
			finally
			{
				if (!success)
				{
					try
					{
						Close();
					}
					catch
					{
					}
				}
			}
			// ensure we throw our original exception
			ReadIndex(si.GetDocCount());
		}

		internal SimpleTextTermVectorsReader(long[] offsets, IndexInput @in)
		{
			// used by clone
			this.offsets = offsets;
			this.@in = @in;
		}

		// we don't actually write a .tvx-like index, instead we read the 
		// vectors file in entirety up-front and save the offsets 
		// so we can seek to the data later.
		/// <exception cref="System.IO.IOException"></exception>
		private void ReadIndex(int maxDoc)
		{
			ChecksumIndexInput input = new BufferedChecksumIndexInput(@in);
			offsets = new long[maxDoc];
			int upto = 0;
			while (!scratch.Equals(END))
			{
				SimpleTextUtil.ReadLine(input, scratch);
				if (StringHelper.StartsWith(scratch, DOC))
				{
					offsets[upto] = input.GetFilePointer();
					upto++;
				}
			}
			SimpleTextUtil.CheckFooter(input);
		}

		//HM:revisit 
		//assert upto == offsets.length;
		/// <exception cref="System.IO.IOException"></exception>
		public override Fields Get(int doc)
		{
			SortedDictionary<string, SimpleTextTermVectorsReader.SimpleTVTerms> fields = new 
				SortedDictionary<string, SimpleTextTermVectorsReader.SimpleTVTerms>();
			@in.Seek(offsets[doc]);
			ReadLine();
			//HM:revisit 
			//assert StringHelper.startsWith(scratch, NUMFIELDS);
			int numFields = ParseIntAt(NUMFIELDS.length);
			if (numFields == 0)
			{
				return null;
			}
			// no vectors for this doc
			for (int i = 0; i < numFields; i++)
			{
				ReadLine();
				//HM:revisit 
				//assert StringHelper.startsWith(scratch, FIELD);
				// skip fieldNumber:
				ParseIntAt(FIELD.length);
				ReadLine();
				//HM:revisit 
				//assert StringHelper.startsWith(scratch, FIELDNAME);
				string fieldName = ReadString(FIELDNAME.length, scratch);
				ReadLine();
				//HM:revisit 
				//assert StringHelper.startsWith(scratch, FIELDPOSITIONS);
				bool positions = System.Boolean.Parse(ReadString(FIELDPOSITIONS.length, scratch));
				ReadLine();
				//HM:revisit 
				//assert StringHelper.startsWith(scratch, FIELDOFFSETS);
				bool offsets = System.Boolean.Parse(ReadString(FIELDOFFSETS.length, scratch));
				ReadLine();
				//HM:revisit 
				//assert StringHelper.startsWith(scratch, FIELDPAYLOADS);
				bool payloads = System.Boolean.Parse(ReadString(FIELDPAYLOADS.length, scratch));
				ReadLine();
				//HM:revisit 
				//assert StringHelper.startsWith(scratch, FIELDTERMCOUNT);
				int termCount = ParseIntAt(FIELDTERMCOUNT.length);
				SimpleTextTermVectorsReader.SimpleTVTerms terms = new SimpleTextTermVectorsReader.SimpleTVTerms
					(offsets, positions, payloads);
				fields.Put(fieldName, terms);
				for (int j = 0; j < termCount; j++)
				{
					ReadLine();
					//HM:revisit 
					//assert StringHelper.startsWith(scratch, TERMTEXT);
					BytesRef term = new BytesRef();
					int termLength = scratch.length - TERMTEXT.length;
					term.Grow(termLength);
					term.length = termLength;
					System.Array.Copy(scratch.bytes, scratch.offset + TERMTEXT.length, term.bytes, term
						.offset, termLength);
					SimpleTextTermVectorsReader.SimpleTVPostings postings = new SimpleTextTermVectorsReader.SimpleTVPostings
						();
					terms.terms.Put(term, postings);
					ReadLine();
					//HM:revisit 
					//assert StringHelper.startsWith(scratch, TERMFREQ);
					postings.freq = ParseIntAt(TERMFREQ.length);
					if (positions || offsets)
					{
						if (positions)
						{
							postings.positions = new int[postings.freq];
							if (payloads)
							{
								postings.payloads = new BytesRef[postings.freq];
							}
						}
						if (offsets)
						{
							postings.startOffsets = new int[postings.freq];
							postings.endOffsets = new int[postings.freq];
						}
						for (int k = 0; k < postings.freq; k++)
						{
							if (positions)
							{
								ReadLine();
								//HM:revisit 
								//assert StringHelper.startsWith(scratch, POSITION);
								postings.positions[k] = ParseIntAt(POSITION.length);
								if (payloads)
								{
									ReadLine();
									//HM:revisit 
									//assert StringHelper.startsWith(scratch, PAYLOAD);
									if (scratch.length - PAYLOAD.length == 0)
									{
										postings.payloads[k] = null;
									}
									else
									{
										byte[] payloadBytes = new byte[scratch.length - PAYLOAD.length];
										System.Array.Copy(scratch.bytes, scratch.offset + PAYLOAD.length, payloadBytes, 0
											, payloadBytes.Length);
										postings.payloads[k] = new BytesRef(payloadBytes);
									}
								}
							}
							if (offsets)
							{
								ReadLine();
								//HM:revisit 
								//assert StringHelper.startsWith(scratch, STARTOFFSET);
								postings.startOffsets[k] = ParseIntAt(STARTOFFSET.length);
								ReadLine();
								//HM:revisit 
								//assert StringHelper.startsWith(scratch, ENDOFFSET);
								postings.endOffsets[k] = ParseIntAt(ENDOFFSET.length);
							}
						}
					}
				}
			}
			return new SimpleTextTermVectorsReader.SimpleTVFields(this, fields);
		}

		public override TermVectorsReader Clone()
		{
			if (@in == null)
			{
				throw new AlreadyClosedException("this TermVectorsReader is closed");
			}
			return new Lucene.Net.Codecs.Simpletext.SimpleTextTermVectorsReader(offsets
				, ((IndexInput)@in.Clone()));
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			try
			{
				IOUtils.Close(@in);
			}
			finally
			{
				@in = null;
				offsets = null;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ReadLine()
		{
			SimpleTextUtil.ReadLine(@in, scratch);
		}

		private int ParseIntAt(int offset)
		{
			UnicodeUtil.UTF8toUTF16(scratch.bytes, scratch.offset + offset, scratch.length - 
				offset, scratchUTF16);
			return ArrayUtil.ParseInt(scratchUTF16.chars, 0, scratchUTF16.length);
		}

		private string ReadString(int offset, BytesRef scratch)
		{
			UnicodeUtil.UTF8toUTF16(scratch.bytes, scratch.offset + offset, scratch.length - 
				offset, scratchUTF16);
			return scratchUTF16.ToString();
		}

		private class SimpleTVFields : Fields
		{
			private readonly SortedDictionary<string, SimpleTextTermVectorsReader.SimpleTVTerms
				> fields;

			internal SimpleTVFields(SimpleTextTermVectorsReader _enclosing, SortedDictionary<
				string, SimpleTextTermVectorsReader.SimpleTVTerms> fields)
			{
				this._enclosing = _enclosing;
				this.fields = fields;
			}

			public override Sharpen.Iterator<string> Iterator()
			{
				return Sharpen.Collections.UnmodifiableSet(this.fields.Keys).Iterator();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Lucene.Net.Index.Terms Terms(string field)
			{
				return this.fields.Get(field);
			}

			public override int Size()
			{
				return this.fields.Count;
			}

			private readonly SimpleTextTermVectorsReader _enclosing;
		}

		private class SimpleTVTerms : Terms
		{
			internal readonly SortedDictionary<BytesRef, SimpleTextTermVectorsReader.SimpleTVPostings
				> terms;

			internal readonly bool hasOffsets;

			internal readonly bool hasPositions;

			internal readonly bool hasPayloads;

			internal SimpleTVTerms(bool hasOffsets, bool hasPositions, bool hasPayloads)
			{
				this.hasOffsets = hasOffsets;
				this.hasPositions = hasPositions;
				this.hasPayloads = hasPayloads;
				terms = new SortedDictionary<BytesRef, SimpleTextTermVectorsReader.SimpleTVPostings
					>();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override TermsEnum Iterator(TermsEnum reuse)
			{
				// TODO: reuse
				return new SimpleTextTermVectorsReader.SimpleTVTermsEnum(terms);
			}

			public override IComparer<BytesRef> GetComparator()
			{
				return BytesRef.GetUTF8SortedAsUnicodeComparator();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override long Size()
			{
				return terms.Count;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override long GetSumTotalTermFreq()
			{
				return -1;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override long GetSumDocFreq()
			{
				return terms.Count;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int GetDocCount()
			{
				return 1;
			}

			public override bool HasFreqs()
			{
				return true;
			}

			public override bool HasOffsets()
			{
				return hasOffsets;
			}

			public override bool HasPositions()
			{
				return hasPositions;
			}

			public override bool HasPayloads()
			{
				return hasPayloads;
			}
		}

		private class SimpleTVPostings
		{
			private int freq;

			private int positions;

			private int startOffsets;

			private int endOffsets;

			private BytesRef payloads;
		}

		private class SimpleTVTermsEnum : TermsEnum
		{
			internal SortedDictionary<BytesRef, SimpleTextTermVectorsReader.SimpleTVPostings>
				 terms;

			internal Iterator<KeyValuePair<BytesRef, SimpleTextTermVectorsReader.SimpleTVPostings
				>> iterator;

			internal KeyValuePair<BytesRef, SimpleTextTermVectorsReader.SimpleTVPostings> current;

			internal SimpleTVTermsEnum(SortedDictionary<BytesRef, SimpleTextTermVectorsReader.SimpleTVPostings
				> terms)
			{
				this.terms = terms;
				this.iterator = terms.EntrySet().Iterator();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override TermsEnum.SeekStatus SeekCeil(BytesRef text)
			{
				iterator = terms.TailMap(text).EntrySet().Iterator();
				if (!iterator.HasNext())
				{
					return TermsEnum.SeekStatus.END;
				}
				else
				{
					return Next().Equals(text) ? TermsEnum.SeekStatus.FOUND : TermsEnum.SeekStatus.NOT_FOUND;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SeekExact(long ord)
			{
				throw new NotSupportedException();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override BytesRef Next()
			{
				if (!iterator.HasNext())
				{
					return null;
				}
				else
				{
					current = iterator.Next();
					return current.Key;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override BytesRef Term()
			{
				return current.Key;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override long Ord()
			{
				throw new NotSupportedException();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int DocFreq()
			{
				return 1;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override long TotalTermFreq()
			{
				return current.Value.freq;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
			{
				// TODO: reuse
				SimpleTextTermVectorsReader.SimpleTVDocsEnum e = new SimpleTextTermVectorsReader.SimpleTVDocsEnum
					();
				e.Reset(liveDocs, (flags & DocsEnum.FLAG_FREQS) == 0 ? 1 : current.Value.freq);
				return e;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum
				 reuse, int flags)
			{
				SimpleTextTermVectorsReader.SimpleTVPostings postings = current.Value;
				if (postings.positions == null && postings.startOffsets == null)
				{
					return null;
				}
				// TODO: reuse
				SimpleTextTermVectorsReader.SimpleTVDocsAndPositionsEnum e = new SimpleTextTermVectorsReader.SimpleTVDocsAndPositionsEnum
					();
				e.Reset(liveDocs, postings.positions, postings.startOffsets, postings.endOffsets, 
					postings.payloads);
				return e;
			}

			public override IComparer<BytesRef> GetComparator()
			{
				return BytesRef.GetUTF8SortedAsUnicodeComparator();
			}
		}

		private class SimpleTVDocsEnum : DocsEnum
		{
			private bool didNext;

			private int doc = -1;

			private int freq;

			private Bits liveDocs;

			// note: these two enum classes are exactly like the Default impl...
			/// <exception cref="System.IO.IOException"></exception>
			public override int Freq()
			{
				//HM:revisit 
				//assert freq != -1;
				return freq;
			}

			public override int DocID()
			{
				return doc;
			}

			public override int NextDoc()
			{
				if (!didNext && (liveDocs == null || liveDocs.Get(0)))
				{
					didNext = true;
					return (doc = 0);
				}
				else
				{
					return (doc = NO_MORE_DOCS);
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				return SlowAdvance(target);
			}

			public virtual void Reset(Bits liveDocs, int freq)
			{
				this.liveDocs = liveDocs;
				this.freq = freq;
				this.doc = -1;
				didNext = false;
			}

			public override long Cost()
			{
				return 1;
			}
		}

		private class SimpleTVDocsAndPositionsEnum : DocsAndPositionsEnum
		{
			private bool didNext;

			private int doc = -1;

			private int nextPos;

			private Bits liveDocs;

			private int[] positions;

			private BytesRef[] payloads;

			private int[] startOffsets;

			private int[] endOffsets;

			/// <exception cref="System.IO.IOException"></exception>
			public override int Freq()
			{
				if (positions != null)
				{
					return positions.Length;
				}
				else
				{
					//HM:revisit 
					//assert startOffsets != null;
					return startOffsets.Length;
				}
			}

			public override int DocID()
			{
				return doc;
			}

			public override int NextDoc()
			{
				if (!didNext && (liveDocs == null || liveDocs.Get(0)))
				{
					didNext = true;
					return (doc = 0);
				}
				else
				{
					return (doc = NO_MORE_DOCS);
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				return SlowAdvance(target);
			}

			public virtual void Reset(Bits liveDocs, int[] positions, int[] startOffsets, int
				[] endOffsets, BytesRef[] payloads)
			{
				this.liveDocs = liveDocs;
				this.positions = positions;
				this.startOffsets = startOffsets;
				this.endOffsets = endOffsets;
				this.payloads = payloads;
				this.doc = -1;
				didNext = false;
				nextPos = 0;
			}

			public override BytesRef GetPayload()
			{
				return payloads == null ? null : payloads[nextPos - 1];
			}

			public override int NextPosition()
			{
				//HM:revisit 
				//assert (positions != null && nextPos < positions.length) || startOffsets != null && nextPos < startOffsets.length;
				if (positions != null)
				{
					return positions[nextPos++];
				}
				else
				{
					nextPos++;
					return -1;
				}
			}

			public override int StartOffset()
			{
				if (startOffsets == null)
				{
					return -1;
				}
				else
				{
					return startOffsets[nextPos - 1];
				}
			}

			public override int EndOffset()
			{
				if (endOffsets == null)
				{
					return -1;
				}
				else
				{
					return endOffsets[nextPos - 1];
				}
			}

			public override long Cost()
			{
				return 1;
			}
		}

		public override long RamBytesUsed()
		{
			return 0;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CheckIntegrity()
		{
		}
	}
}
