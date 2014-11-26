/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Memory;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Sharpen;

namespace Lucene.Net.Codecs.Memory
{
	/// <summary>
	/// Wraps
	/// <see cref="Lucene.Net.Codecs.Lucene41.Lucene41PostingsFormat">Lucene.Net.Codecs.Lucene41.Lucene41PostingsFormat
	/// 	</see>
	/// format for on-disk
	/// storage, but then at read time loads and stores all
	/// terms & postings directly in RAM as byte[], int[].
	/// <p><b><font color=red>WARNING</font></b>: This is
	/// exceptionally RAM intensive: it makes no effort to
	/// compress the postings data, storing terms as separate
	/// byte[] and postings as separate int[], but as a result it
	/// gives substantial increase in search performance.
	/// <p>This postings format supports
	/// <see cref="Lucene.Net.Index.TermsEnum.Ord()">Lucene.Net.Index.TermsEnum.Ord()
	/// 	</see>
	/// and
	/// <see cref="Lucene.Net.Index.TermsEnum.SeekExact(long)">Lucene.Net.Index.TermsEnum.SeekExact(long)
	/// 	</see>
	/// .
	/// <p>Because this holds all term bytes as a single
	/// byte[], you cannot have more than 2.1GB worth of term
	/// bytes in a single segment.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public sealed class DirectPostingsFormat : PostingsFormat
	{
		private readonly int minSkipCount;

		private readonly int lowFreqCutoff;

		private const int DEFAULT_MIN_SKIP_COUNT = 8;

		private const int DEFAULT_LOW_FREQ_CUTOFF = 32;

		public DirectPostingsFormat() : this(DEFAULT_MIN_SKIP_COUNT, DEFAULT_LOW_FREQ_CUTOFF
			)
		{
		}

		/// <summary>
		/// minSkipCount is how many terms in a row must have the
		/// same prefix before we put a skip pointer down.
		/// </summary>
		/// <remarks>
		/// minSkipCount is how many terms in a row must have the
		/// same prefix before we put a skip pointer down.  Terms
		/// with docFreq &lt;= lowFreqCutoff will use a single int[]
		/// to hold all docs, freqs, position and offsets; terms
		/// with higher docFreq will use separate arrays.
		/// </remarks>
		public DirectPostingsFormat(int minSkipCount, int lowFreqCutoff) : base("Direct")
		{
			// javadocs
			// TODO: 
			//   - build depth-N prefix hash?
			//   - or: longer dense skip lists than just next byte?
			//private static final boolean DEBUG = true;
			// TODO: allow passing/wrapping arbitrary postings format?
			this.minSkipCount = minSkipCount;
			this.lowFreqCutoff = lowFreqCutoff;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.Codecs.FieldsConsumer FieldsConsumer(SegmentWriteState
			 state)
		{
			return PostingsFormat.ForName("Lucene41").FieldsConsumer(state);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Lucene.Net.Codecs.FieldsProducer FieldsProducer(SegmentReadState
			 state)
		{
			Lucene.Net.Codecs.FieldsProducer postings = PostingsFormat.ForName("Lucene41"
				).FieldsProducer(state);
			if (state.context.context != IOContext.Context.MERGE)
			{
				Lucene.Net.Codecs.FieldsProducer loadedPostings;
				try
				{
					postings.CheckIntegrity();
					loadedPostings = new DirectPostingsFormat.DirectFields(state, postings, minSkipCount
						, lowFreqCutoff);
				}
				finally
				{
					postings.Close();
				}
				return loadedPostings;
			}
			else
			{
				// Don't load postings for merge:
				return postings;
			}
		}

		private sealed class DirectFields : FieldsProducer
		{
			private readonly IDictionary<string, DirectPostingsFormat.DirectField> fields = new 
				SortedDictionary<string, DirectPostingsFormat.DirectField>();

			/// <exception cref="System.IO.IOException"></exception>
			public DirectFields(SegmentReadState state, Fields fields, int minSkipCount, int 
				lowFreqCutoff)
			{
				foreach (string field in fields)
				{
					this.fields.Put(field, new DirectPostingsFormat.DirectField(state, field, fields.
						Terms(field), minSkipCount, lowFreqCutoff));
				}
			}

			public override Sharpen.Iterator<string> Iterator()
			{
				return Sharpen.Collections.UnmodifiableSet(fields.Keys).Iterator();
			}

			public override Lucene.Net.Index.Terms Terms(string field)
			{
				return fields.Get(field);
			}

			public override int Size()
			{
				return fields.Count;
			}

			public override long GetUniqueTermCount()
			{
				long numTerms = 0;
				foreach (DirectPostingsFormat.DirectField field in fields.Values)
				{
					numTerms += field.terms.Length;
				}
				return numTerms;
			}

			public override void Close()
			{
			}

			public override long RamBytesUsed()
			{
				long sizeInBytes = 0;
				foreach (KeyValuePair<string, DirectPostingsFormat.DirectField> entry in fields.EntrySet
					())
				{
					sizeInBytes += entry.Key.Length * RamUsageEstimator.NUM_BYTES_CHAR;
					sizeInBytes += entry.Value.RamBytesUsed();
				}
				return sizeInBytes;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void CheckIntegrity()
			{
			}
			// if we read entirely into ram, we already validated.
			// otherwise returned the raw postings reader
		}

		private sealed class DirectField : Terms
		{
			private abstract class TermAndSkip
			{
				public int[] skips;

				/// <summary>Returns the approximate number of RAM bytes used</summary>
				public abstract long RamBytesUsed();
			}

			private sealed class LowFreqTerm : DirectPostingsFormat.DirectField.TermAndSkip
			{
				public readonly int[] postings;

				public readonly byte[] payloads;

				public readonly int docFreq;

				public readonly int totalTermFreq;

				public LowFreqTerm(int[] postings, byte[] payloads, int docFreq, int totalTermFreq
					)
				{
					this.postings = postings;
					this.payloads = payloads;
					this.docFreq = docFreq;
					this.totalTermFreq = totalTermFreq;
				}

				public override long RamBytesUsed()
				{
					return ((postings != null) ? RamUsageEstimator.SizeOf(postings) : 0) + ((payloads
						 != null) ? RamUsageEstimator.SizeOf(payloads) : 0);
				}
			}

			private sealed class HighFreqTerm : DirectPostingsFormat.DirectField.TermAndSkip
			{
				public readonly long totalTermFreq;

				public readonly int[] docIDs;

				public readonly int[] freqs;

				public readonly int[][] positions;

				public readonly byte[][][] payloads;

				public HighFreqTerm(int[] docIDs, int[] freqs, int[][] positions, byte[][][] payloads
					, long totalTermFreq)
				{
					// TODO: maybe specialize into prx/no-prx/no-frq cases?
					this.docIDs = docIDs;
					this.freqs = freqs;
					this.positions = positions;
					this.payloads = payloads;
					this.totalTermFreq = totalTermFreq;
				}

				public override long RamBytesUsed()
				{
					long sizeInBytes = 0;
					sizeInBytes += (docIDs != null) ? RamUsageEstimator.SizeOf(docIDs) : 0;
					sizeInBytes += (freqs != null) ? RamUsageEstimator.SizeOf(freqs) : 0;
					if (positions != null)
					{
						foreach (int[] position in positions)
						{
							sizeInBytes += (position != null) ? RamUsageEstimator.SizeOf(position) : 0;
						}
					}
					if (payloads != null)
					{
						foreach (byte[][] payload in payloads)
						{
							if (payload != null)
							{
								foreach (byte[] pload in payload)
								{
									sizeInBytes += (pload != null) ? RamUsageEstimator.SizeOf(pload) : 0;
								}
							}
						}
					}
					return sizeInBytes;
				}
			}

			private readonly byte[] termBytes;

			private readonly int[] termOffsets;

			private readonly int[] skips;

			private readonly int[] skipOffsets;

			private readonly DirectPostingsFormat.DirectField.TermAndSkip[] terms;

			private readonly bool hasFreq;

			private readonly bool hasPos;

			private readonly bool hasOffsets;

			private readonly bool hasPayloads;

			private readonly long sumTotalTermFreq;

			private readonly int docCount;

			private readonly long sumDocFreq;

			private int skipCount;

			private int count;

			private int[] sameCounts = new int[10];

			private readonly int minSkipCount;

			private sealed class IntArrayWriter
			{
				private int[] ints = new int[10];

				private int upto;

				// TODO: maybe make a separate builder?  These are only
				// used during load:
				public void Add(int value)
				{
					if (ints.Length == upto)
					{
						ints = ArrayUtil.Grow(ints);
					}
					ints[upto++] = value;
				}

				public int[] Get()
				{
					int[] arr = new int[upto];
					System.Array.Copy(ints, 0, arr, 0, upto);
					upto = 0;
					return arr;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public DirectField(SegmentReadState state, string field, Terms termsIn, int minSkipCount
				, int lowFreqCutoff)
			{
				FieldInfo fieldInfo = state.fieldInfos.FieldInfo(field);
				sumTotalTermFreq = termsIn.GetSumTotalTermFreq();
				sumDocFreq = termsIn.GetSumDocFreq();
				docCount = termsIn.GetDocCount();
				int numTerms = (int)termsIn.Size();
				if (numTerms == -1)
				{
					throw new ArgumentException("codec does not provide Terms.size()");
				}
				terms = new DirectPostingsFormat.DirectField.TermAndSkip[numTerms];
				termOffsets = new int[1 + numTerms];
				byte[] termBytes = new byte[1024];
				this.minSkipCount = minSkipCount;
				hasFreq = fieldInfo.GetIndexOptions().CompareTo(FieldInfo.IndexOptions.DOCS_ONLY)
					 > 0;
				hasPos = fieldInfo.GetIndexOptions().CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS
					) > 0;
				hasOffsets = fieldInfo.GetIndexOptions().CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS
					) > 0;
				hasPayloads = fieldInfo.HasPayloads();
				BytesRef term;
				DocsEnum docsEnum = null;
				DocsAndPositionsEnum docsAndPositionsEnum = null;
				TermsEnum termsEnum = termsIn.Iterator(null);
				int termOffset = 0;
				DirectPostingsFormat.DirectField.IntArrayWriter scratch = new DirectPostingsFormat.DirectField.IntArrayWriter
					();
				// Used for payloads, if any:
				RAMOutputStream ros = new RAMOutputStream();
				// if (DEBUG) {
				//   System.out.println("\nLOAD terms seg=" + state.segmentInfo.name + " field=" + field + " hasOffsets=" + hasOffsets + " hasFreq=" + hasFreq + " hasPos=" + hasPos + " hasPayloads=" + hasPayloads);
				// }
				while ((term = termsEnum.Next()) != null)
				{
					int docFreq = termsEnum.DocFreq();
					long totalTermFreq = termsEnum.TotalTermFreq();
					// if (DEBUG) {
					//   System.out.println("  term=" + term.utf8ToString());
					// }
					termOffsets[count] = termOffset;
					if (termBytes.Length < (termOffset + term.length))
					{
						termBytes = ArrayUtil.Grow(termBytes, termOffset + term.length);
					}
					System.Array.Copy(term.bytes, term.offset, termBytes, termOffset, term.length);
					termOffset += term.length;
					termOffsets[count + 1] = termOffset;
					if (hasPos)
					{
						docsAndPositionsEnum = termsEnum.DocsAndPositions(null, docsAndPositionsEnum);
					}
					else
					{
						docsEnum = termsEnum.Docs(null, docsEnum);
					}
					DirectPostingsFormat.DirectField.TermAndSkip ent;
					DocsEnum docsEnum2;
					if (hasPos)
					{
						docsEnum2 = docsAndPositionsEnum;
					}
					else
					{
						docsEnum2 = docsEnum;
					}
					int docID;
					if (docFreq <= lowFreqCutoff)
					{
						ros.Reset();
						// Pack postings for low-freq terms into a single int[]:
						while ((docID = docsEnum2.NextDoc()) != DocsEnum.NO_MORE_DOCS)
						{
							scratch.Add(docID);
							if (hasFreq)
							{
								int freq = docsEnum2.Freq();
								scratch.Add(freq);
								if (hasPos)
								{
									for (int pos = 0; pos < freq; pos++)
									{
										scratch.Add(docsAndPositionsEnum.NextPosition());
										if (hasOffsets)
										{
											scratch.Add(docsAndPositionsEnum.StartOffset());
											scratch.Add(docsAndPositionsEnum.EndOffset());
										}
										if (hasPayloads)
										{
											BytesRef payload = docsAndPositionsEnum.GetPayload();
											if (payload != null)
											{
												scratch.Add(payload.length);
												ros.WriteBytes(payload.bytes, payload.offset, payload.length);
											}
											else
											{
												scratch.Add(0);
											}
										}
									}
								}
							}
						}
						byte[] payloads;
						if (hasPayloads)
						{
							ros.Flush();
							payloads = new byte[(int)ros.Length()];
							ros.WriteTo(payloads, 0);
						}
						else
						{
							payloads = null;
						}
						int[] postings = scratch.Get();
						ent = new DirectPostingsFormat.DirectField.LowFreqTerm(postings, payloads, docFreq
							, (int)totalTermFreq);
					}
					else
					{
						int[] docs = new int[docFreq];
						int[] freqs;
						int[][] positions;
						byte[][][] payloads;
						if (hasFreq)
						{
							freqs = new int[docFreq];
							if (hasPos)
							{
								positions = new int[docFreq][];
								if (hasPayloads)
								{
									payloads = new byte[docFreq][][];
								}
								else
								{
									payloads = null;
								}
							}
							else
							{
								positions = null;
								payloads = null;
							}
						}
						else
						{
							freqs = null;
							positions = null;
							payloads = null;
						}
						// Use separate int[] for the postings for high-freq
						// terms:
						int upto = 0;
						while ((docID = docsEnum2.NextDoc()) != DocsEnum.NO_MORE_DOCS)
						{
							docs[upto] = docID;
							if (hasFreq)
							{
								int freq = docsEnum2.Freq();
								freqs[upto] = freq;
								if (hasPos)
								{
									int mult;
									if (hasOffsets)
									{
										mult = 3;
									}
									else
									{
										mult = 1;
									}
									if (hasPayloads)
									{
										payloads[upto] = new byte[freq][];
									}
									positions[upto] = new int[mult * freq];
									int posUpto = 0;
									for (int pos = 0; pos < freq; pos++)
									{
										positions[upto][posUpto] = docsAndPositionsEnum.NextPosition();
										if (hasPayloads)
										{
											BytesRef payload = docsAndPositionsEnum.GetPayload();
											if (payload != null)
											{
												byte[] payloadBytes = new byte[payload.length];
												System.Array.Copy(payload.bytes, payload.offset, payloadBytes, 0, payload.length);
												payloads[upto][pos] = payloadBytes;
											}
										}
										posUpto++;
										if (hasOffsets)
										{
											positions[upto][posUpto++] = docsAndPositionsEnum.StartOffset();
											positions[upto][posUpto++] = docsAndPositionsEnum.EndOffset();
										}
									}
								}
							}
							upto++;
						}
						//HM:revisit 
						//assert upto == docFreq;
						ent = new DirectPostingsFormat.DirectField.HighFreqTerm(docs, freqs, positions, payloads
							, totalTermFreq);
					}
					terms[count] = ent;
					SetSkips(count, termBytes);
					count++;
				}
				// End sentinel:
				termOffsets[count] = termOffset;
				FinishSkips();
				//System.out.println(skipCount + " skips: " + field);
				this.termBytes = new byte[termOffset];
				System.Array.Copy(termBytes, 0, this.termBytes, 0, termOffset);
				// Pack skips:
				this.skips = new int[skipCount];
				this.skipOffsets = new int[1 + numTerms];
				int skipOffset = 0;
				for (int i = 0; i < numTerms; i++)
				{
					int[] termSkips = terms[i].skips;
					skipOffsets[i] = skipOffset;
					if (termSkips != null)
					{
						System.Array.Copy(termSkips, 0, skips, skipOffset, termSkips.Length);
						skipOffset += termSkips.Length;
						terms[i].skips = null;
					}
				}
				this.skipOffsets[numTerms] = skipOffset;
			}

			//HM:revisit 
			//assert skipOffset == skipCount;
			/// <summary>Returns approximate RAM bytes used</summary>
			public long RamBytesUsed()
			{
				long sizeInBytes = 0;
				sizeInBytes += ((termBytes != null) ? RamUsageEstimator.SizeOf(termBytes) : 0);
				sizeInBytes += ((termOffsets != null) ? RamUsageEstimator.SizeOf(termOffsets) : 0
					);
				sizeInBytes += ((skips != null) ? RamUsageEstimator.SizeOf(skips) : 0);
				sizeInBytes += ((skipOffsets != null) ? RamUsageEstimator.SizeOf(skipOffsets) : 0
					);
				sizeInBytes += ((sameCounts != null) ? RamUsageEstimator.SizeOf(sameCounts) : 0);
				if (terms != null)
				{
					foreach (DirectPostingsFormat.DirectField.TermAndSkip termAndSkip in terms)
					{
						sizeInBytes += (termAndSkip != null) ? termAndSkip.RamBytesUsed() : 0;
					}
				}
				return sizeInBytes;
			}

			// Compares in unicode (UTF8) order:
			internal int Compare(int ord, BytesRef other)
			{
				byte[] otherBytes = other.bytes;
				int upto = termOffsets[ord];
				int termLen = termOffsets[1 + ord] - upto;
				int otherUpto = other.offset;
				int stop = upto + Math.Min(termLen, other.length);
				while (upto < stop)
				{
					int diff = (termBytes[upto++] & unchecked((int)(0xFF))) - (otherBytes[otherUpto++
						] & unchecked((int)(0xFF)));
					if (diff != 0)
					{
						return diff;
					}
				}
				// One is a prefix of the other, or, they are equal:
				return termLen - other.length;
			}

			private void SetSkips(int termOrd, byte[] termBytes)
			{
				int termLength = termOffsets[termOrd + 1] - termOffsets[termOrd];
				if (sameCounts.Length < termLength)
				{
					sameCounts = ArrayUtil.Grow(sameCounts, termLength);
				}
				// Update skip pointers:
				if (termOrd > 0)
				{
					int lastTermLength = termOffsets[termOrd] - termOffsets[termOrd - 1];
					int limit = Math.Min(termLength, lastTermLength);
					int lastTermOffset = termOffsets[termOrd - 1];
					int termOffset = termOffsets[termOrd];
					int i = 0;
					for (; i < limit; i++)
					{
						if (termBytes[lastTermOffset++] == termBytes[termOffset++])
						{
							sameCounts[i]++;
						}
						else
						{
							for (; i < limit; i++)
							{
								if (sameCounts[i] >= minSkipCount)
								{
									// Go back and add a skip pointer:
									SaveSkip(termOrd, sameCounts[i]);
								}
								sameCounts[i] = 1;
							}
							break;
						}
					}
					for (; i < lastTermLength; i++)
					{
						if (sameCounts[i] >= minSkipCount)
						{
							// Go back and add a skip pointer:
							SaveSkip(termOrd, sameCounts[i]);
						}
						sameCounts[i] = 0;
					}
					for (int j = limit; j < termLength; j++)
					{
						sameCounts[j] = 1;
					}
				}
				else
				{
					for (int i = 0; i < termLength; i++)
					{
						sameCounts[i]++;
					}
				}
			}

			private void FinishSkips()
			{
				//HM:revisit 
				//assert count == terms.length;
				int lastTermOffset = termOffsets[count - 1];
				int lastTermLength = termOffsets[count] - lastTermOffset;
				for (int i = 0; i < lastTermLength; i++)
				{
					if (sameCounts[i] >= minSkipCount)
					{
						// Go back and add a skip pointer:
						SaveSkip(count, sameCounts[i]);
					}
				}
				// Reverse the skip pointers so they are "nested":
				for (int termID = 0; termID < terms.Length; termID++)
				{
					DirectPostingsFormat.DirectField.TermAndSkip term = terms[termID];
					if (term.skips != null && term.skips.Length > 1)
					{
						for (int pos = 0; pos < term.skips.Length / 2; pos++)
						{
							int otherPos = term.skips.Length - pos - 1;
							int temp = term.skips[pos];
							term.skips[pos] = term.skips[otherPos];
							term.skips[otherPos] = temp;
						}
					}
				}
			}

			private void SaveSkip(int ord, int backCount)
			{
				DirectPostingsFormat.DirectField.TermAndSkip term = terms[ord - backCount];
				skipCount++;
				if (term.skips == null)
				{
					term.skips = new int[] { ord };
				}
				else
				{
					// Normally we'd grow at a slight exponential... but
					// given that the skips themselves are already log(N)
					// we can grow by only 1 and still have amortized
					// linear time:
					int[] newSkips = new int[term.skips.Length + 1];
					System.Array.Copy(term.skips, 0, newSkips, 0, term.skips.Length);
					term.skips = newSkips;
					term.skips[term.skips.Length - 1] = ord;
				}
			}

			public override TermsEnum Iterator(TermsEnum reuse)
			{
				DirectPostingsFormat.DirectField.DirectTermsEnum termsEnum;
				if (reuse != null && reuse is DirectPostingsFormat.DirectField.DirectTermsEnum)
				{
					termsEnum = (DirectPostingsFormat.DirectField.DirectTermsEnum)reuse;
					if (!termsEnum.CanReuse(terms))
					{
						termsEnum = new DirectPostingsFormat.DirectField.DirectTermsEnum(this);
					}
				}
				else
				{
					termsEnum = new DirectPostingsFormat.DirectField.DirectTermsEnum(this);
				}
				termsEnum.Reset();
				return termsEnum;
			}

			public override TermsEnum Intersect(CompiledAutomaton compiled, BytesRef startTerm
				)
			{
				return new DirectPostingsFormat.DirectField.DirectIntersectTermsEnum(this, compiled
					, startTerm);
			}

			public override long Size()
			{
				return terms.Length;
			}

			public override long GetSumTotalTermFreq()
			{
				return sumTotalTermFreq;
			}

			public override long GetSumDocFreq()
			{
				return sumDocFreq;
			}

			public override int GetDocCount()
			{
				return docCount;
			}

			public override IComparer<BytesRef> GetComparator()
			{
				return BytesRef.GetUTF8SortedAsUnicodeComparator();
			}

			public override bool HasFreqs()
			{
				return hasFreq;
			}

			public override bool HasOffsets()
			{
				return hasOffsets;
			}

			public override bool HasPositions()
			{
				return hasPos;
			}

			public override bool HasPayloads()
			{
				return hasPayloads;
			}

			private sealed class DirectTermsEnum : TermsEnum
			{
				private readonly BytesRef scratch = new BytesRef();

				private int termOrd;

				internal bool CanReuse(DirectPostingsFormat.DirectField.TermAndSkip[] other)
				{
					return this._enclosing.terms == other;
				}

				private BytesRef SetTerm()
				{
					this.scratch.bytes = this._enclosing.termBytes;
					this.scratch.offset = this._enclosing.termOffsets[this.termOrd];
					this.scratch.length = this._enclosing.termOffsets[this.termOrd + 1] - this._enclosing
						.termOffsets[this.termOrd];
					return this.scratch;
				}

				public void Reset()
				{
					this.termOrd = -1;
				}

				public override IComparer<BytesRef> GetComparator()
				{
					return BytesRef.GetUTF8SortedAsUnicodeComparator();
				}

				public override BytesRef Next()
				{
					this.termOrd++;
					if (this.termOrd < this._enclosing.terms.Length)
					{
						return this.SetTerm();
					}
					else
					{
						return null;
					}
				}

				public override Lucene.Net.Index.TermState TermState()
				{
					OrdTermState state = new OrdTermState();
					state.ord = this.termOrd;
					return state;
				}

				// If non-negative, exact match; else, -ord-1, where ord
				// is where you would insert the term.
				private int FindTerm(BytesRef term)
				{
					// Just do binary search: should be (constant factor)
					// faster than using the skip list:
					int low = 0;
					int high = this._enclosing.terms.Length - 1;
					while (low <= high)
					{
						int mid = (int)(((uint)(low + high)) >> 1);
						int cmp = this._enclosing.Compare(mid, term);
						if (cmp < 0)
						{
							low = mid + 1;
						}
						else
						{
							if (cmp > 0)
							{
								high = mid - 1;
							}
							else
							{
								return mid;
							}
						}
					}
					// key found
					return -(low + 1);
				}

				// key not found.
				public override TermsEnum.SeekStatus SeekCeil(BytesRef term)
				{
					// TODO: we should use the skip pointers; should be
					// faster than bin search; we should also hold
					// & reuse current state so seeking forwards is
					// faster
					int ord = this.FindTerm(term);
					// if (DEBUG) {
					//   System.out.println("  find term=" + term.utf8ToString() + " ord=" + ord);
					// }
					if (ord >= 0)
					{
						this.termOrd = ord;
						this.SetTerm();
						return TermsEnum.SeekStatus.FOUND;
					}
					else
					{
						if (ord == -this._enclosing.terms.Length - 1)
						{
							return TermsEnum.SeekStatus.END;
						}
						else
						{
							this.termOrd = -ord - 1;
							this.SetTerm();
							return TermsEnum.SeekStatus.NOT_FOUND;
						}
					}
				}

				public override bool SeekExact(BytesRef term)
				{
					// TODO: we should use the skip pointers; should be
					// faster than bin search; we should also hold
					// & reuse current state so seeking forwards is
					// faster
					int ord = this.FindTerm(term);
					if (ord >= 0)
					{
						this.termOrd = ord;
						this.SetTerm();
						return true;
					}
					else
					{
						return false;
					}
				}

				public override void SeekExact(long ord)
				{
					this.termOrd = (int)ord;
					this.SetTerm();
				}

				/// <exception cref="System.IO.IOException"></exception>
				public override void SeekExact(BytesRef term, Lucene.Net.Index.TermState state
					)
				{
					this.termOrd = (int)((OrdTermState)state).ord;
					this.SetTerm();
				}

				//HM:revisit 
				//assert term.equals(scratch);
				public override BytesRef Term()
				{
					return this.scratch;
				}

				public override long Ord()
				{
					return this.termOrd;
				}

				public override int DocFreq()
				{
					if (this._enclosing.terms[this.termOrd] is DirectPostingsFormat.DirectField.LowFreqTerm)
					{
						return ((DirectPostingsFormat.DirectField.LowFreqTerm)this._enclosing.terms[this.
							termOrd]).docFreq;
					}
					else
					{
						return ((DirectPostingsFormat.DirectField.HighFreqTerm)this._enclosing.terms[this
							.termOrd]).docIDs.Length;
					}
				}

				public override long TotalTermFreq()
				{
					if (this._enclosing.terms[this.termOrd] is DirectPostingsFormat.DirectField.LowFreqTerm)
					{
						return ((DirectPostingsFormat.DirectField.LowFreqTerm)this._enclosing.terms[this.
							termOrd]).totalTermFreq;
					}
					else
					{
						return ((DirectPostingsFormat.DirectField.HighFreqTerm)this._enclosing.terms[this
							.termOrd]).totalTermFreq;
					}
				}

				public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
				{
					// TODO: implement reuse, something like Pulsing:
					// it's hairy!
					if (this._enclosing.terms[this.termOrd] is DirectPostingsFormat.DirectField.LowFreqTerm)
					{
						int[] postings = ((DirectPostingsFormat.DirectField.LowFreqTerm)this._enclosing.terms
							[this.termOrd]).postings;
						if (this._enclosing.hasFreq)
						{
							if (this._enclosing.hasPos)
							{
								int posLen;
								if (this._enclosing.hasOffsets)
								{
									posLen = 3;
								}
								else
								{
									posLen = 1;
								}
								if (this._enclosing.hasPayloads)
								{
									posLen++;
								}
								DirectPostingsFormat.LowFreqDocsEnum docsEnum;
								if (reuse is DirectPostingsFormat.LowFreqDocsEnum)
								{
									docsEnum = (DirectPostingsFormat.LowFreqDocsEnum)reuse;
									if (!docsEnum.CanReuse(liveDocs, posLen))
									{
										docsEnum = new DirectPostingsFormat.LowFreqDocsEnum(liveDocs, posLen);
									}
								}
								else
								{
									docsEnum = new DirectPostingsFormat.LowFreqDocsEnum(liveDocs, posLen);
								}
								return docsEnum.Reset(postings);
							}
							else
							{
								DirectPostingsFormat.LowFreqDocsEnumNoPos docsEnum;
								if (reuse is DirectPostingsFormat.LowFreqDocsEnumNoPos)
								{
									docsEnum = (DirectPostingsFormat.LowFreqDocsEnumNoPos)reuse;
									if (!docsEnum.CanReuse(liveDocs))
									{
										docsEnum = new DirectPostingsFormat.LowFreqDocsEnumNoPos(liveDocs);
									}
								}
								else
								{
									docsEnum = new DirectPostingsFormat.LowFreqDocsEnumNoPos(liveDocs);
								}
								return docsEnum.Reset(postings);
							}
						}
						else
						{
							DirectPostingsFormat.LowFreqDocsEnumNoTF docsEnum;
							if (reuse is DirectPostingsFormat.LowFreqDocsEnumNoTF)
							{
								docsEnum = (DirectPostingsFormat.LowFreqDocsEnumNoTF)reuse;
								if (!docsEnum.CanReuse(liveDocs))
								{
									docsEnum = new DirectPostingsFormat.LowFreqDocsEnumNoTF(liveDocs);
								}
							}
							else
							{
								docsEnum = new DirectPostingsFormat.LowFreqDocsEnumNoTF(liveDocs);
							}
							return docsEnum.Reset(postings);
						}
					}
					else
					{
						DirectPostingsFormat.DirectField.HighFreqTerm term = (DirectPostingsFormat.DirectField.HighFreqTerm
							)this._enclosing.terms[this.termOrd];
						DirectPostingsFormat.HighFreqDocsEnum docsEnum;
						if (reuse is DirectPostingsFormat.HighFreqDocsEnum)
						{
							docsEnum = (DirectPostingsFormat.HighFreqDocsEnum)reuse;
							if (!docsEnum.CanReuse(liveDocs))
							{
								docsEnum = new DirectPostingsFormat.HighFreqDocsEnum(liveDocs);
							}
						}
						else
						{
							docsEnum = new DirectPostingsFormat.HighFreqDocsEnum(liveDocs);
						}
						//System.out.println("  DE for term=" + new BytesRef(terms[termOrd].term).utf8ToString() + ": " + term.docIDs.length + " docs");
						return docsEnum.Reset(term.docIDs, term.freqs);
					}
				}

				public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum
					 reuse, int flags)
				{
					if (!this._enclosing.hasPos)
					{
						return null;
					}
					// TODO: implement reuse, something like Pulsing:
					// it's hairy!
					if (this._enclosing.terms[this.termOrd] is DirectPostingsFormat.DirectField.LowFreqTerm)
					{
						DirectPostingsFormat.DirectField.LowFreqTerm term = ((DirectPostingsFormat.DirectField.LowFreqTerm
							)this._enclosing.terms[this.termOrd]);
						int[] postings = term.postings;
						byte[] payloads = term.payloads;
						return new DirectPostingsFormat.LowFreqDocsAndPositionsEnum(liveDocs, this._enclosing
							.hasOffsets, this._enclosing.hasPayloads).Reset(postings, payloads);
					}
					else
					{
						DirectPostingsFormat.DirectField.HighFreqTerm term = (DirectPostingsFormat.DirectField.HighFreqTerm
							)this._enclosing.terms[this.termOrd];
						return new DirectPostingsFormat.HighFreqDocsAndPositionsEnum(liveDocs, this._enclosing
							.hasOffsets).Reset(term.docIDs, term.freqs, term.positions, term.payloads);
					}
				}

				internal DirectTermsEnum(DirectField _enclosing)
				{
					this._enclosing = _enclosing;
				}

				private readonly DirectField _enclosing;
			}

			private sealed class DirectIntersectTermsEnum : TermsEnum
			{
				private readonly RunAutomaton runAutomaton;

				private readonly CompiledAutomaton compiledAutomaton;

				private int termOrd;

				private readonly BytesRef scratch = new BytesRef();

				private sealed class State
				{
					internal int changeOrd;

					internal int state;

					internal Transition[] transitions;

					internal int transitionUpto;

					internal int transitionMax;

					internal int transitionMin;

					internal State(DirectIntersectTermsEnum _enclosing)
					{
						this._enclosing = _enclosing;
					}

					private readonly DirectIntersectTermsEnum _enclosing;
				}

				private DirectPostingsFormat.DirectField.DirectIntersectTermsEnum.State[] states;

				private int stateUpto;

				public DirectIntersectTermsEnum(DirectField _enclosing, CompiledAutomaton compiled
					, BytesRef startTerm)
				{
					this._enclosing = _enclosing;
					this.runAutomaton = compiled.runAutomaton;
					this.compiledAutomaton = compiled;
					this.termOrd = -1;
					this.states = new DirectPostingsFormat.DirectField.DirectIntersectTermsEnum.State
						[1];
					this.states[0] = new DirectPostingsFormat.DirectField.DirectIntersectTermsEnum.State
						(this);
					this.states[0].changeOrd = this._enclosing.terms.Length;
					this.states[0].state = this.runAutomaton.GetInitialState();
					this.states[0].transitions = this.compiledAutomaton.sortedTransitions[this.states
						[0].state];
					this.states[0].transitionUpto = -1;
					this.states[0].transitionMax = -1;
					//System.out.println("IE.init startTerm=" + startTerm);
					if (startTerm != null)
					{
						int skipUpto = 0;
						if (startTerm.length == 0)
						{
							if (this._enclosing.terms.Length > 0 && this._enclosing.termOffsets[1] == 0)
							{
								this.termOrd = 0;
							}
						}
						else
						{
							this.termOrd++;
							for (int i = 0; i < startTerm.length; i++)
							{
								int label = startTerm.bytes[startTerm.offset + i] & unchecked((int)(0xFF));
								while (label > this.states[i].transitionMax)
								{
									this.states[i].transitionUpto++;
									//HM:revisit 
									//assert states[i].transitionUpto < states[i].transitions.length;
									this.states[i].transitionMin = this.states[i].transitions[this.states[i].transitionUpto
										].GetMin();
									this.states[i].transitionMax = this.states[i].transitions[this.states[i].transitionUpto
										].GetMax();
								}
								//HM:revisit 
								//assert states[i].transitionMin >= 0;
								//HM:revisit 
								//assert states[i].transitionMin <= 255;
								//HM:revisit 
								//assert states[i].transitionMax >= 0;
								//HM:revisit 
								//assert states[i].transitionMax <= 255;
								// Skip forwards until we find a term matching
								// the label at this position:
								while (this.termOrd < this._enclosing.terms.Length)
								{
									int skipOffset = this._enclosing.skipOffsets[this.termOrd];
									int numSkips = this._enclosing.skipOffsets[this.termOrd + 1] - skipOffset;
									int termOffset = this._enclosing.termOffsets[this.termOrd];
									int termLength = this._enclosing.termOffsets[1 + this.termOrd] - termOffset;
									// if (DEBUG) {
									//   System.out.println("  check termOrd=" + termOrd + " term=" + new BytesRef(termBytes, termOffset, termLength).utf8ToString() + " skips=" + Arrays.toString(skips) + " i=" + i);
									// }
									if (this.termOrd == this.states[this.stateUpto].changeOrd)
									{
										// if (DEBUG) {
										//   System.out.println("  end push return");
										// }
										this.stateUpto--;
										this.termOrd--;
										return;
									}
									if (termLength == i)
									{
										this.termOrd++;
										skipUpto = 0;
									}
									else
									{
										// if (DEBUG) {
										//   System.out.println("    term too short; next term");
										// }
										if (label < (this._enclosing.termBytes[termOffset + i] & unchecked((int)(0xFF))))
										{
											this.termOrd--;
											// if (DEBUG) {
											//   System.out.println("  no match; already beyond; return termOrd=" + termOrd);
											// }
											this.stateUpto -= skipUpto;
											//HM:revisit 
											//assert stateUpto >= 0;
											return;
										}
										else
										{
											if (label == (this._enclosing.termBytes[termOffset + i] & unchecked((int)(0xFF))))
											{
												// if (DEBUG) {
												//   System.out.println("    label[" + i + "] matches");
												// }
												if (skipUpto < numSkips)
												{
													this.Grow();
													int nextState = this.runAutomaton.Step(this.states[this.stateUpto].state, label);
													// Automaton is required to accept startTerm:
													//HM:revisit 
													//assert nextState != -1;
													this.stateUpto++;
													this.states[this.stateUpto].changeOrd = this._enclosing.skips[skipOffset + skipUpto
														++];
													this.states[this.stateUpto].state = nextState;
													this.states[this.stateUpto].transitions = this.compiledAutomaton.sortedTransitions
														[nextState];
													this.states[this.stateUpto].transitionUpto = -1;
													this.states[this.stateUpto].transitionMax = -1;
													//System.out.println("  push " + states[stateUpto].transitions.length + " trans");
													// if (DEBUG) {
													//   System.out.println("    push skip; changeOrd=" + states[stateUpto].changeOrd);
													// }
													// Match next label at this same term:
													goto nextLabel_continue;
												}
												else
												{
													// if (DEBUG) {
													//   System.out.println("    linear scan");
													// }
													// Index exhausted: just scan now (the
													// number of scans required will be less
													// than the minSkipCount):
													int startTermOrd = this.termOrd;
													while (this.termOrd < this._enclosing.terms.Length && this._enclosing.Compare(this
														.termOrd, startTerm) <= 0)
													{
														//HM:revisit 
														//assert termOrd == startTermOrd || skipOffsets[termOrd] == skipOffsets[termOrd+1];
														this.termOrd++;
													}
													//HM:revisit 
													//assert termOrd - startTermOrd < minSkipCount;
													this.termOrd--;
													this.stateUpto -= skipUpto;
													// if (DEBUG) {
													//   System.out.println("  end termOrd=" + termOrd);
													// }
													return;
												}
											}
											else
											{
												if (skipUpto < numSkips)
												{
													this.termOrd = this._enclosing.skips[skipOffset + skipUpto];
												}
												else
												{
													// if (DEBUG) {
													//   System.out.println("  no match; skip to termOrd=" + termOrd);
													// }
													// if (DEBUG) {
													//   System.out.println("  no match; next term");
													// }
													this.termOrd++;
												}
												skipUpto = 0;
											}
										}
									}
								}
								// startTerm is >= last term so enum will not
								// return any terms:
								this.termOrd--;
								// if (DEBUG) {
								//   System.out.println("  beyond end; no terms will match");
								// }
								return;
nextLabel_continue: ;
							}
nextLabel_break: ;
						}
						int termOffset_1 = this._enclosing.termOffsets[this.termOrd];
						int termLen = this._enclosing.termOffsets[1 + this.termOrd] - termOffset_1;
						if (this.termOrd >= 0 && !startTerm.Equals(new BytesRef(this._enclosing.termBytes
							, termOffset_1, termLen)))
						{
							this.stateUpto -= skipUpto;
							this.termOrd--;
						}
					}
				}

				// if (DEBUG) {
				//   System.out.println("  loop end; return termOrd=" + termOrd + " stateUpto=" + stateUpto);
				// }
				public override IComparer<BytesRef> GetComparator()
				{
					return BytesRef.GetUTF8SortedAsUnicodeComparator();
				}

				private void Grow()
				{
					if (this.states.Length == 1 + this.stateUpto)
					{
						DirectPostingsFormat.DirectField.DirectIntersectTermsEnum.State[] newStates = new 
							DirectPostingsFormat.DirectField.DirectIntersectTermsEnum.State[this.states.Length
							 + 1];
						System.Array.Copy(this.states, 0, newStates, 0, this.states.Length);
						newStates[this.states.Length] = new DirectPostingsFormat.DirectField.DirectIntersectTermsEnum.State
							(this);
						this.states = newStates;
					}
				}

				public override BytesRef Next()
				{
					// if (DEBUG) {
					//   System.out.println("\nIE.next");
					// }
					this.termOrd++;
					int skipUpto = 0;
					if (this.termOrd == 0 && this._enclosing.termOffsets[1] == 0)
					{
						// Special-case empty string:
						//HM:revisit 
						//assert stateUpto == 0;
						// if (DEBUG) {
						//   System.out.println("  visit empty string");
						// }
						if (this.runAutomaton.IsAccept(this.states[0].state))
						{
							this.scratch.bytes = this._enclosing.termBytes;
							this.scratch.offset = 0;
							this.scratch.length = 0;
							return this.scratch;
						}
						this.termOrd++;
					}
					while (true)
					{
						// if (DEBUG) {
						//   System.out.println("  cycle termOrd=" + termOrd + " stateUpto=" + stateUpto + " skipUpto=" + skipUpto);
						// }
						if (this.termOrd == this._enclosing.terms.Length)
						{
							// if (DEBUG) {
							//   System.out.println("  return END");
							// }
							return null;
						}
						DirectPostingsFormat.DirectField.DirectIntersectTermsEnum.State state = this.states
							[this.stateUpto];
						if (this.termOrd == state.changeOrd)
						{
							// Pop:
							// if (DEBUG) {
							//   System.out.println("  pop stateUpto=" + stateUpto);
							// }
							this.stateUpto--;
							continue;
						}
						int termOffset = this._enclosing.termOffsets[this.termOrd];
						int termLength = this._enclosing.termOffsets[this.termOrd + 1] - termOffset;
						int skipOffset = this._enclosing.skipOffsets[this.termOrd];
						int numSkips = this._enclosing.skipOffsets[this.termOrd + 1] - skipOffset;
						// if (DEBUG) {
						//   System.out.println("  term=" + new BytesRef(termBytes, termOffset, termLength).utf8ToString() + " skips=" + Arrays.toString(skips));
						// }
						//HM:revisit 
						//assert termOrd < state.changeOrd;
						//HM:revisit 
						//assert stateUpto <= termLength: "term.length=" + termLength + "; stateUpto=" + stateUpto;
						int label = this._enclosing.termBytes[termOffset + this.stateUpto] & unchecked((int
							)(0xFF));
						while (label > state.transitionMax)
						{
							//System.out.println("  label=" + label + " vs max=" + state.transitionMax + " transUpto=" + state.transitionUpto + " vs " + state.transitions.length);
							state.transitionUpto++;
							if (state.transitionUpto == state.transitions.Length)
							{
								// We've exhausted transitions leaving this
								// state; force pop+next/skip now:
								//System.out.println("forcepop: stateUpto=" + stateUpto);
								if (this.stateUpto == 0)
								{
									this.termOrd = this._enclosing.terms.Length;
									return null;
								}
								else
								{
									//HM:revisit 
									//assert state.changeOrd > termOrd;
									// if (DEBUG) {
									//   System.out.println("  jumpend " + (state.changeOrd - termOrd));
									// }
									//System.out.println("  jump to termOrd=" + states[stateUpto].changeOrd + " vs " + termOrd);
									this.termOrd = this.states[this.stateUpto].changeOrd;
									skipUpto = 0;
									this.stateUpto--;
								}
								goto nextTerm_continue;
							}
							//HM:revisit 
							//assert state.transitionUpto < state.transitions.length: " state.transitionUpto=" + state.transitionUpto + " vs " + state.transitions.length;
							state.transitionMin = state.transitions[state.transitionUpto].GetMin();
							state.transitionMax = state.transitions[state.transitionUpto].GetMax();
						}
						//HM:revisit 
						//assert state.transitionMin >= 0;
						//HM:revisit 
						//assert state.transitionMin <= 255;
						//HM:revisit 
						//assert state.transitionMax >= 0;
						//HM:revisit 
						//assert state.transitionMax <= 255;
						int targetLabel = state.transitionMin;
						if ((this._enclosing.termBytes[termOffset + this.stateUpto] & unchecked((int)(0xFF
							))) < targetLabel)
						{
							// if (DEBUG) {
							//   System.out.println("    do bin search");
							// }
							//int startTermOrd = termOrd;
							int low = this.termOrd + 1;
							int high = state.changeOrd - 1;
							while (true)
							{
								if (low > high)
								{
									// Label not found
									this.termOrd = low;
									// if (DEBUG) {
									//   System.out.println("      advanced by " + (termOrd - startTermOrd));
									// }
									//System.out.println("  jump " + (termOrd - startTermOrd));
									skipUpto = 0;
									goto nextTerm_continue;
								}
								int mid = (int)(((uint)(low + high)) >> 1);
								int cmp = (this._enclosing.termBytes[this._enclosing.termOffsets[mid] + this.stateUpto
									] & unchecked((int)(0xFF))) - targetLabel;
								// if (DEBUG) {
								//   System.out.println("      bin: check label=" + (char) (termBytes[termOffsets[low] + stateUpto] & 0xFF) + " ord=" + mid);
								// }
								if (cmp < 0)
								{
									low = mid + 1;
								}
								else
								{
									if (cmp > 0)
									{
										high = mid - 1;
									}
									else
									{
										// Label found; walk backwards to first
										// occurrence:
										while (mid > this.termOrd && (this._enclosing.termBytes[this._enclosing.termOffsets
											[mid - 1] + this.stateUpto] & unchecked((int)(0xFF))) == targetLabel)
										{
											mid--;
										}
										this.termOrd = mid;
										// if (DEBUG) {
										//   System.out.println("      advanced by " + (termOrd - startTermOrd));
										// }
										//System.out.println("  jump " + (termOrd - startTermOrd));
										skipUpto = 0;
										goto nextTerm_continue;
									}
								}
							}
						}
						int nextState = this.runAutomaton.Step(this.states[this.stateUpto].state, label);
						if (nextState == -1)
						{
							// Skip
							// if (DEBUG) {
							//   System.out.println("  automaton doesn't accept; skip");
							// }
							if (skipUpto < numSkips)
							{
								// if (DEBUG) {
								//   System.out.println("  jump " + (skips[skipOffset+skipUpto]-1 - termOrd));
								// }
								this.termOrd = this._enclosing.skips[skipOffset + skipUpto];
							}
							else
							{
								this.termOrd++;
							}
							skipUpto = 0;
						}
						else
						{
							if (skipUpto < numSkips)
							{
								// Push:
								// if (DEBUG) {
								//   System.out.println("  push");
								// }
								this.Grow();
								this.stateUpto++;
								this.states[this.stateUpto].state = nextState;
								this.states[this.stateUpto].changeOrd = this._enclosing.skips[skipOffset + skipUpto
									++];
								this.states[this.stateUpto].transitions = this.compiledAutomaton.sortedTransitions
									[nextState];
								this.states[this.stateUpto].transitionUpto = -1;
								this.states[this.stateUpto].transitionMax = -1;
								if (this.stateUpto == termLength)
								{
									// if (DEBUG) {
									//   System.out.println("  term ends after push");
									// }
									if (this.runAutomaton.IsAccept(nextState))
									{
										// if (DEBUG) {
										//   System.out.println("  automaton accepts: return");
										// }
										this.scratch.bytes = this._enclosing.termBytes;
										this.scratch.offset = this._enclosing.termOffsets[this.termOrd];
										this.scratch.length = this._enclosing.termOffsets[1 + this.termOrd] - this.scratch
											.offset;
										// if (DEBUG) {
										//   System.out.println("  ret " + scratch.utf8ToString());
										// }
										return this.scratch;
									}
									else
									{
										// if (DEBUG) {
										//   System.out.println("  automaton rejects: nextTerm");
										// }
										this.termOrd++;
										skipUpto = 0;
									}
								}
							}
							else
							{
								// Run the non-indexed tail of this term:
								// TODO: add 
								//HM:revisit 
								//assert that we don't inc too many times
								if (this.compiledAutomaton.commonSuffixRef != null)
								{
									//System.out.println("suffix " + compiledAutomaton.commonSuffixRef.utf8ToString());
									//HM:revisit 
									//assert compiledAutomaton.commonSuffixRef.offset == 0;
									if (termLength < this.compiledAutomaton.commonSuffixRef.length)
									{
										this.termOrd++;
										skipUpto = 0;
										goto nextTerm_continue;
									}
									int offset = termOffset + termLength - this.compiledAutomaton.commonSuffixRef.length;
									for (int suffix = 0; suffix < this.compiledAutomaton.commonSuffixRef.length; suffix
										++)
									{
										if (this._enclosing.termBytes[offset + suffix] != this.compiledAutomaton.commonSuffixRef
											.bytes[suffix])
										{
											this.termOrd++;
											skipUpto = 0;
											goto nextTerm_continue;
										}
									}
								}
								int upto = this.stateUpto + 1;
								while (upto < termLength)
								{
									nextState = this.runAutomaton.Step(nextState, this._enclosing.termBytes[termOffset
										 + upto] & unchecked((int)(0xFF)));
									if (nextState == -1)
									{
										this.termOrd++;
										skipUpto = 0;
										// if (DEBUG) {
										//   System.out.println("  nomatch tail; next term");
										// }
										goto nextTerm_continue;
									}
									upto++;
								}
								if (this.runAutomaton.IsAccept(nextState))
								{
									this.scratch.bytes = this._enclosing.termBytes;
									this.scratch.offset = this._enclosing.termOffsets[this.termOrd];
									this.scratch.length = this._enclosing.termOffsets[1 + this.termOrd] - this.scratch
										.offset;
									// if (DEBUG) {
									//   System.out.println("  match tail; return " + scratch.utf8ToString());
									//   System.out.println("  ret2 " + scratch.utf8ToString());
									// }
									return this.scratch;
								}
								else
								{
									this.termOrd++;
									skipUpto = 0;
								}
							}
						}
nextTerm_continue: ;
					}
nextTerm_break: ;
				}

				// if (DEBUG) {
				//   System.out.println("  nomatch tail; next term");
				// }
				public override TermState TermState()
				{
					OrdTermState state = new OrdTermState();
					state.ord = this.termOrd;
					return state;
				}

				public override BytesRef Term()
				{
					return this.scratch;
				}

				public override long Ord()
				{
					return this.termOrd;
				}

				public override int DocFreq()
				{
					if (this._enclosing.terms[this.termOrd] is DirectPostingsFormat.DirectField.LowFreqTerm)
					{
						return ((DirectPostingsFormat.DirectField.LowFreqTerm)this._enclosing.terms[this.
							termOrd]).docFreq;
					}
					else
					{
						return ((DirectPostingsFormat.DirectField.HighFreqTerm)this._enclosing.terms[this
							.termOrd]).docIDs.Length;
					}
				}

				public override long TotalTermFreq()
				{
					if (this._enclosing.terms[this.termOrd] is DirectPostingsFormat.DirectField.LowFreqTerm)
					{
						return ((DirectPostingsFormat.DirectField.LowFreqTerm)this._enclosing.terms[this.
							termOrd]).totalTermFreq;
					}
					else
					{
						return ((DirectPostingsFormat.DirectField.HighFreqTerm)this._enclosing.terms[this
							.termOrd]).totalTermFreq;
					}
				}

				public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
				{
					// TODO: implement reuse, something like Pulsing:
					// it's hairy!
					if (this._enclosing.terms[this.termOrd] is DirectPostingsFormat.DirectField.LowFreqTerm)
					{
						int[] postings = ((DirectPostingsFormat.DirectField.LowFreqTerm)this._enclosing.terms
							[this.termOrd]).postings;
						if (this._enclosing.hasFreq)
						{
							if (this._enclosing.hasPos)
							{
								int posLen;
								if (this._enclosing.hasOffsets)
								{
									posLen = 3;
								}
								else
								{
									posLen = 1;
								}
								if (this._enclosing.hasPayloads)
								{
									posLen++;
								}
								return new DirectPostingsFormat.LowFreqDocsEnum(liveDocs, posLen).Reset(postings);
							}
							else
							{
								return new DirectPostingsFormat.LowFreqDocsEnumNoPos(liveDocs).Reset(postings);
							}
						}
						else
						{
							return new DirectPostingsFormat.LowFreqDocsEnumNoTF(liveDocs).Reset(postings);
						}
					}
					else
					{
						DirectPostingsFormat.DirectField.HighFreqTerm term = (DirectPostingsFormat.DirectField.HighFreqTerm
							)this._enclosing.terms[this.termOrd];
						//  System.out.println("DE for term=" + new BytesRef(terms[termOrd].term).utf8ToString() + ": " + term.docIDs.length + " docs");
						return new DirectPostingsFormat.HighFreqDocsEnum(liveDocs).Reset(term.docIDs, term
							.freqs);
					}
				}

				public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum
					 reuse, int flags)
				{
					if (!this._enclosing.hasPos)
					{
						return null;
					}
					// TODO: implement reuse, something like Pulsing:
					// it's hairy!
					if (this._enclosing.terms[this.termOrd] is DirectPostingsFormat.DirectField.LowFreqTerm)
					{
						DirectPostingsFormat.DirectField.LowFreqTerm term = ((DirectPostingsFormat.DirectField.LowFreqTerm
							)this._enclosing.terms[this.termOrd]);
						int[] postings = term.postings;
						byte[] payloads = term.payloads;
						return new DirectPostingsFormat.LowFreqDocsAndPositionsEnum(liveDocs, this._enclosing
							.hasOffsets, this._enclosing.hasPayloads).Reset(postings, payloads);
					}
					else
					{
						DirectPostingsFormat.DirectField.HighFreqTerm term = (DirectPostingsFormat.DirectField.HighFreqTerm
							)this._enclosing.terms[this.termOrd];
						return new DirectPostingsFormat.HighFreqDocsAndPositionsEnum(liveDocs, this._enclosing
							.hasOffsets).Reset(term.docIDs, term.freqs, term.positions, term.payloads);
					}
				}

				public override TermsEnum.SeekStatus SeekCeil(BytesRef term)
				{
					throw new NotSupportedException();
				}

				public override void SeekExact(long ord)
				{
					throw new NotSupportedException();
				}

				private readonly DirectField _enclosing;
			}
		}

		private sealed class LowFreqDocsEnumNoTF : DocsEnum
		{
			private int[] postings;

			private readonly Bits liveDocs;

			private int upto;

			public LowFreqDocsEnumNoTF(Bits liveDocs)
			{
				// Docs only:
				this.liveDocs = liveDocs;
			}

			public bool CanReuse(Bits liveDocs)
			{
				return liveDocs == this.liveDocs;
			}

			public DocsEnum Reset(int[] postings)
			{
				this.postings = postings;
				upto = -1;
				return this;
			}

			// TODO: can do this w/o setting members?
			public override int NextDoc()
			{
				upto++;
				if (liveDocs == null)
				{
					if (upto < postings.Length)
					{
						return postings[upto];
					}
				}
				else
				{
					while (upto < postings.Length)
					{
						if (liveDocs.Get(postings[upto]))
						{
							return postings[upto];
						}
						upto++;
					}
				}
				return NO_MORE_DOCS;
			}

			public override int DocID()
			{
				if (upto < 0)
				{
					return -1;
				}
				else
				{
					if (upto < postings.Length)
					{
						return postings[upto];
					}
					else
					{
						return NO_MORE_DOCS;
					}
				}
			}

			public override int Freq()
			{
				return 1;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				// Linear scan, but this is low-freq term so it won't
				// be costly:
				return SlowAdvance(target);
			}

			public override long Cost()
			{
				return postings.Length;
			}
		}

		private sealed class LowFreqDocsEnumNoPos : DocsEnum
		{
			private int[] postings;

			private readonly Bits liveDocs;

			private int upto;

			public LowFreqDocsEnumNoPos(Bits liveDocs)
			{
				// Docs + freqs:
				this.liveDocs = liveDocs;
			}

			public bool CanReuse(Bits liveDocs)
			{
				return liveDocs == this.liveDocs;
			}

			public DocsEnum Reset(int[] postings)
			{
				this.postings = postings;
				upto = -2;
				return this;
			}

			// TODO: can do this w/o setting members?
			public override int NextDoc()
			{
				upto += 2;
				if (liveDocs == null)
				{
					if (upto < postings.Length)
					{
						return postings[upto];
					}
				}
				else
				{
					while (upto < postings.Length)
					{
						if (liveDocs.Get(postings[upto]))
						{
							return postings[upto];
						}
						upto += 2;
					}
				}
				return NO_MORE_DOCS;
			}

			public override int DocID()
			{
				if (upto < 0)
				{
					return -1;
				}
				else
				{
					if (upto < postings.Length)
					{
						return postings[upto];
					}
					else
					{
						return NO_MORE_DOCS;
					}
				}
			}

			public override int Freq()
			{
				return postings[upto + 1];
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				// Linear scan, but this is low-freq term so it won't
				// be costly:
				return SlowAdvance(target);
			}

			public override long Cost()
			{
				return postings.Length / 2;
			}
		}

		private sealed class LowFreqDocsEnum : DocsEnum
		{
			private int[] postings;

			private readonly Bits liveDocs;

			private readonly int posMult;

			private int upto;

			private int freq;

			public LowFreqDocsEnum(Bits liveDocs, int posMult)
			{
				// Docs + freqs + positions/offets:
				this.liveDocs = liveDocs;
				this.posMult = posMult;
			}

			// if (DEBUG) {
			//   System.out.println("LowFreqDE: posMult=" + posMult);
			// }
			public bool CanReuse(Bits liveDocs, int posMult)
			{
				return liveDocs == this.liveDocs && posMult == this.posMult;
			}

			public DocsEnum Reset(int[] postings)
			{
				this.postings = postings;
				upto = -2;
				freq = 0;
				return this;
			}

			// TODO: can do this w/o setting members?
			public override int NextDoc()
			{
				upto += 2 + freq * posMult;
				// if (DEBUG) {
				//   System.out.println("  nextDoc freq=" + freq + " upto=" + upto + " vs " + postings.length);
				// }
				if (liveDocs == null)
				{
					if (upto < postings.Length)
					{
						freq = postings[upto + 1];
						//HM:revisit 
						//assert freq > 0;
						return postings[upto];
					}
				}
				else
				{
					while (upto < postings.Length)
					{
						freq = postings[upto + 1];
						//HM:revisit 
						//assert freq > 0;
						if (liveDocs.Get(postings[upto]))
						{
							return postings[upto];
						}
						upto += 2 + freq * posMult;
					}
				}
				return NO_MORE_DOCS;
			}

			public override int DocID()
			{
				// TODO: store docID member?
				if (upto < 0)
				{
					return -1;
				}
				else
				{
					if (upto < postings.Length)
					{
						return postings[upto];
					}
					else
					{
						return NO_MORE_DOCS;
					}
				}
			}

			public override int Freq()
			{
				// TODO: can I do postings[upto+1]?
				return freq;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				// Linear scan, but this is low-freq term so it won't
				// be costly:
				return SlowAdvance(target);
			}

			public override long Cost()
			{
				// TODO: could do a better estimate
				return postings.Length / 2;
			}
		}

		private sealed class LowFreqDocsAndPositionsEnum : DocsAndPositionsEnum
		{
			private int[] postings;

			private readonly Bits liveDocs;

			private readonly int posMult;

			private readonly bool hasOffsets;

			private readonly bool hasPayloads;

			private readonly BytesRef payload = new BytesRef();

			private int upto;

			private int docID;

			private int freq;

			private int skipPositions;

			private int startOffset;

			private int endOffset;

			private int lastPayloadOffset;

			private int payloadOffset;

			private int payloadLength;

			private byte[] payloadBytes;

			public LowFreqDocsAndPositionsEnum(Bits liveDocs, bool hasOffsets, bool hasPayloads
				)
			{
				this.liveDocs = liveDocs;
				this.hasOffsets = hasOffsets;
				this.hasPayloads = hasPayloads;
				if (hasOffsets)
				{
					if (hasPayloads)
					{
						posMult = 4;
					}
					else
					{
						posMult = 3;
					}
				}
				else
				{
					if (hasPayloads)
					{
						posMult = 2;
					}
					else
					{
						posMult = 1;
					}
				}
			}

			public DocsAndPositionsEnum Reset(int[] postings, byte[] payloadBytes)
			{
				this.postings = postings;
				upto = 0;
				skipPositions = 0;
				startOffset = -1;
				endOffset = -1;
				docID = -1;
				payloadLength = 0;
				this.payloadBytes = payloadBytes;
				return this;
			}

			public override int NextDoc()
			{
				if (hasPayloads)
				{
					for (int i = 0; i < skipPositions; i++)
					{
						upto++;
						if (hasOffsets)
						{
							upto += 2;
						}
						payloadOffset += postings[upto++];
					}
				}
				else
				{
					upto += posMult * skipPositions;
				}
				if (liveDocs == null)
				{
					if (upto < postings.Length)
					{
						docID = postings[upto++];
						freq = postings[upto++];
						skipPositions = freq;
						return docID;
					}
				}
				else
				{
					while (upto < postings.Length)
					{
						docID = postings[upto++];
						freq = postings[upto++];
						if (liveDocs.Get(docID))
						{
							skipPositions = freq;
							return docID;
						}
						if (hasPayloads)
						{
							for (int i = 0; i < freq; i++)
							{
								upto++;
								if (hasOffsets)
								{
									upto += 2;
								}
								payloadOffset += postings[upto++];
							}
						}
						else
						{
							upto += posMult * freq;
						}
					}
				}
				return docID = NO_MORE_DOCS;
			}

			public override int DocID()
			{
				return docID;
			}

			public override int Freq()
			{
				return freq;
			}

			public override int NextPosition()
			{
				//HM:revisit 
				//assert skipPositions > 0;
				skipPositions--;
				int pos = postings[upto++];
				if (hasOffsets)
				{
					startOffset = postings[upto++];
					endOffset = postings[upto++];
				}
				if (hasPayloads)
				{
					payloadLength = postings[upto++];
					lastPayloadOffset = payloadOffset;
					payloadOffset += payloadLength;
				}
				return pos;
			}

			public override int StartOffset()
			{
				return startOffset;
			}

			public override int EndOffset()
			{
				return endOffset;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				return SlowAdvance(target);
			}

			public override BytesRef GetPayload()
			{
				if (payloadLength > 0)
				{
					payload.bytes = payloadBytes;
					payload.offset = lastPayloadOffset;
					payload.length = payloadLength;
					return payload;
				}
				else
				{
					return null;
				}
			}

			public override long Cost()
			{
				// TODO: could do a better estimate
				return postings.Length / 2;
			}
		}

		private sealed class HighFreqDocsEnum : DocsEnum
		{
			private int[] docIDs;

			private int[] freqs;

			private readonly Bits liveDocs;

			private int upto;

			private int docID = -1;

			public HighFreqDocsEnum(Bits liveDocs)
			{
				// Docs + freqs:
				this.liveDocs = liveDocs;
			}

			public bool CanReuse(Bits liveDocs)
			{
				return liveDocs == this.liveDocs;
			}

			public int[] GetDocIDs()
			{
				return docIDs;
			}

			public int[] GetFreqs()
			{
				return freqs;
			}

			public DocsEnum Reset(int[] docIDs, int[] freqs)
			{
				this.docIDs = docIDs;
				this.freqs = freqs;
				docID = upto = -1;
				return this;
			}

			public override int NextDoc()
			{
				upto++;
				if (liveDocs == null)
				{
					try
					{
						return docID = docIDs[upto];
					}
					catch (IndexOutOfRangeException)
					{
					}
				}
				else
				{
					while (upto < docIDs.Length)
					{
						if (liveDocs.Get(docIDs[upto]))
						{
							return docID = docIDs[upto];
						}
						upto++;
					}
				}
				return docID = NO_MORE_DOCS;
			}

			public override int DocID()
			{
				return docID;
			}

			public override int Freq()
			{
				if (freqs == null)
				{
					return 1;
				}
				else
				{
					return freqs[upto];
				}
			}

			public override int Advance(int target)
			{
				//System.out.println("  advance target=" + target + " cur=" + docID() + " upto=" + upto + " of " + docIDs.length);
				// if (DEBUG) {
				//   System.out.println("advance target=" + target + " len=" + docIDs.length);
				// }
				upto++;
				if (upto == docIDs.Length)
				{
					return docID = NO_MORE_DOCS;
				}
				// First "grow" outwards, since most advances are to
				// nearby docs:
				int inc = 10;
				int nextUpto = upto + 10;
				int low;
				int high;
				while (true)
				{
					//System.out.println("  grow nextUpto=" + nextUpto + " inc=" + inc);
					if (nextUpto >= docIDs.Length)
					{
						low = nextUpto - inc;
						high = docIDs.Length - 1;
						break;
					}
					//System.out.println("    docID=" + docIDs[nextUpto]);
					if (target <= docIDs[nextUpto])
					{
						low = nextUpto - inc;
						high = nextUpto;
						break;
					}
					inc *= 2;
					nextUpto += inc;
				}
				// Now do normal binary search
				//System.out.println("    after fwd: low=" + low + " high=" + high);
				while (true)
				{
					if (low > high)
					{
						// Not exactly found
						//System.out.println("    break: no match");
						upto = low;
						break;
					}
					int mid = (int)(((uint)(low + high)) >> 1);
					int cmp = docIDs[mid] - target;
					//System.out.println("    bsearch low=" + low + " high=" + high+ ": docIDs[" + mid + "]=" + docIDs[mid]);
					if (cmp < 0)
					{
						low = mid + 1;
					}
					else
					{
						if (cmp > 0)
						{
							high = mid - 1;
						}
						else
						{
							// Found target
							upto = mid;
							//System.out.println("    break: match");
							break;
						}
					}
				}
				//System.out.println("    end upto=" + upto + " docID=" + (upto >= docIDs.length ? NO_MORE_DOCS : docIDs[upto]));
				if (liveDocs != null)
				{
					while (upto < docIDs.Length)
					{
						if (liveDocs.Get(docIDs[upto]))
						{
							break;
						}
						upto++;
					}
				}
				if (upto == docIDs.Length)
				{
					//System.out.println("    return END");
					return docID = NO_MORE_DOCS;
				}
				else
				{
					//System.out.println("    return docID=" + docIDs[upto] + " upto=" + upto);
					return docID = docIDs[upto];
				}
			}

			public override long Cost()
			{
				return docIDs.Length;
			}
		}

		private sealed class HighFreqDocsAndPositionsEnum : DocsAndPositionsEnum
		{
			private int[] docIDs;

			private int[] freqs;

			private int[][] positions;

			private byte[][][] payloads;

			private readonly Bits liveDocs;

			private readonly bool hasOffsets;

			private readonly int posJump;

			private int upto;

			private int docID = -1;

			private int posUpto;

			private int[] curPositions;

			public HighFreqDocsAndPositionsEnum(Bits liveDocs, bool hasOffsets)
			{
				// TODO: specialize offsets and not
				this.liveDocs = liveDocs;
				this.hasOffsets = hasOffsets;
				posJump = hasOffsets ? 3 : 1;
			}

			public int[] GetDocIDs()
			{
				return docIDs;
			}

			public int[][] GetPositions()
			{
				return positions;
			}

			public int GetPosJump()
			{
				return posJump;
			}

			public Bits GetLiveDocs()
			{
				return liveDocs;
			}

			public DocsAndPositionsEnum Reset(int[] docIDs, int[] freqs, int[][] positions, byte
				[][][] payloads)
			{
				this.docIDs = docIDs;
				this.freqs = freqs;
				this.positions = positions;
				this.payloads = payloads;
				upto = -1;
				return this;
			}

			public override int NextDoc()
			{
				upto++;
				if (liveDocs == null)
				{
					if (upto < docIDs.Length)
					{
						posUpto = -posJump;
						curPositions = positions[upto];
						return docID = docIDs[upto];
					}
				}
				else
				{
					while (upto < docIDs.Length)
					{
						if (liveDocs.Get(docIDs[upto]))
						{
							posUpto = -posJump;
							curPositions = positions[upto];
							return docID = docIDs[upto];
						}
						upto++;
					}
				}
				return docID = NO_MORE_DOCS;
			}

			public override int Freq()
			{
				return freqs[upto];
			}

			public override int DocID()
			{
				return docID;
			}

			public override int NextPosition()
			{
				posUpto += posJump;
				return curPositions[posUpto];
			}

			public override int StartOffset()
			{
				if (hasOffsets)
				{
					return curPositions[posUpto + 1];
				}
				else
				{
					return -1;
				}
			}

			public override int EndOffset()
			{
				if (hasOffsets)
				{
					return curPositions[posUpto + 2];
				}
				else
				{
					return -1;
				}
			}

			public override int Advance(int target)
			{
				//System.out.println("  advance target=" + target + " cur=" + docID() + " upto=" + upto + " of " + docIDs.length);
				// if (DEBUG) {
				//   System.out.println("advance target=" + target + " len=" + docIDs.length);
				// }
				upto++;
				if (upto == docIDs.Length)
				{
					return docID = NO_MORE_DOCS;
				}
				// First "grow" outwards, since most advances are to
				// nearby docs:
				int inc = 10;
				int nextUpto = upto + 10;
				int low;
				int high;
				while (true)
				{
					//System.out.println("  grow nextUpto=" + nextUpto + " inc=" + inc);
					if (nextUpto >= docIDs.Length)
					{
						low = nextUpto - inc;
						high = docIDs.Length - 1;
						break;
					}
					//System.out.println("    docID=" + docIDs[nextUpto]);
					if (target <= docIDs[nextUpto])
					{
						low = nextUpto - inc;
						high = nextUpto;
						break;
					}
					inc *= 2;
					nextUpto += inc;
				}
				// Now do normal binary search
				//System.out.println("    after fwd: low=" + low + " high=" + high);
				while (true)
				{
					if (low > high)
					{
						// Not exactly found
						//System.out.println("    break: no match");
						upto = low;
						break;
					}
					int mid = (int)(((uint)(low + high)) >> 1);
					int cmp = docIDs[mid] - target;
					//System.out.println("    bsearch low=" + low + " high=" + high+ ": docIDs[" + mid + "]=" + docIDs[mid]);
					if (cmp < 0)
					{
						low = mid + 1;
					}
					else
					{
						if (cmp > 0)
						{
							high = mid - 1;
						}
						else
						{
							// Found target
							upto = mid;
							//System.out.println("    break: match");
							break;
						}
					}
				}
				//System.out.println("    end upto=" + upto + " docID=" + (upto >= docIDs.length ? NO_MORE_DOCS : docIDs[upto]));
				if (liveDocs != null)
				{
					while (upto < docIDs.Length)
					{
						if (liveDocs.Get(docIDs[upto]))
						{
							break;
						}
						upto++;
					}
				}
				if (upto == docIDs.Length)
				{
					//System.out.println("    return END");
					return docID = NO_MORE_DOCS;
				}
				else
				{
					//System.out.println("    return docID=" + docIDs[upto] + " upto=" + upto);
					posUpto = -posJump;
					curPositions = positions[upto];
					return docID = docIDs[upto];
				}
			}

			private readonly BytesRef payload = new BytesRef();

			public override BytesRef GetPayload()
			{
				if (payloads == null)
				{
					return null;
				}
				else
				{
					byte[] payloadBytes = payloads[upto][posUpto / (hasOffsets ? 3 : 1)];
					if (payloadBytes == null)
					{
						return null;
					}
					payload.bytes = payloadBytes;
					payload.length = payloadBytes.Length;
					payload.offset = 0;
					return payload;
				}
			}

			public override long Cost()
			{
				return docIDs.Length;
			}
		}
	}
}
