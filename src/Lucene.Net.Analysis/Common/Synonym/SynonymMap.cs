/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Synonym;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using Sharpen;

namespace Lucene.Net.Analysis.Synonym
{
	/// <summary>A map of synonyms, keys and values are phrases.</summary>
	/// <remarks>A map of synonyms, keys and values are phrases.</remarks>
	/// <lucene.experimental></lucene.experimental>
	public class SynonymMap
	{
		/// <summary>for multiword support, you must separate words with this separator</summary>
		public const char WORD_SEPARATOR = 0;

		/// <summary>map&lt;input word, list&lt;ord&gt;&gt;</summary>
		public readonly FST<BytesRef> fst;

		/// <summary>map&lt;ord, outputword&gt;</summary>
		public readonly BytesRefHash words;

		/// <summary>maxHorizontalContext: maximum context we need on the tokenstream</summary>
		public readonly int maxHorizontalContext;

		public SynonymMap(FST<BytesRef> fst, BytesRefHash words, int maxHorizontalContext
			)
		{
			this.fst = fst;
			this.words = words;
			this.maxHorizontalContext = maxHorizontalContext;
		}

		/// <summary>Builds an FSTSynonymMap.</summary>
		/// <remarks>
		/// Builds an FSTSynonymMap.
		/// <p>
		/// Call add() until you have added all the mappings, then call build() to get an FSTSynonymMap
		/// </remarks>
		/// <lucene.experimental></lucene.experimental>
		public class Builder
		{
			private readonly Dictionary<CharsRef, SynonymMap.Builder.MapEntry> workingSet = new 
				Dictionary<CharsRef, SynonymMap.Builder.MapEntry>();

			private readonly BytesRefHash words = new BytesRefHash();

			private readonly BytesRef utf8Scratch = new BytesRef(8);

			private int maxHorizontalContext;

			private readonly bool dedup;

			/// <summary>
			/// If dedup is true then identical rules (same input,
			/// same output) will be added only once.
			/// </summary>
			/// <remarks>
			/// If dedup is true then identical rules (same input,
			/// same output) will be added only once.
			/// </remarks>
			public Builder(bool dedup)
			{
				this.dedup = dedup;
			}

			private class MapEntry
			{
				internal bool includeOrig;

				internal AList<int> ords = new AList<int>();
				// we could sort for better sharing ultimately, but it could confuse people
			}

			/// <summary>
			/// Sugar: just joins the provided terms with
			/// <see cref="SynonymMap.WORD_SEPARATOR">SynonymMap.WORD_SEPARATOR</see>
			/// .  reuse and its chars
			/// must not be null.
			/// </summary>
			public static CharsRef Join(string[] words, CharsRef reuse)
			{
				int upto = 0;
				char[] buffer = reuse.chars;
				foreach (string word in words)
				{
					int wordLen = word.Length;
					int needed = (0 == upto ? wordLen : 1 + upto + wordLen);
					// Add 1 for WORD_SEPARATOR
					if (needed > buffer.Length)
					{
						reuse.Grow(needed);
						buffer = reuse.chars;
					}
					if (upto > 0)
					{
						buffer[upto++] = SynonymMap.WORD_SEPARATOR;
					}
					Sharpen.Runtime.GetCharsForString(word, 0, wordLen, buffer, upto);
					upto += wordLen;
				}
				reuse.length = upto;
				return reuse;
			}

			/// <summary>only used for asserting!</summary>
			private bool HasHoles(CharsRef chars)
			{
				int end = chars.offset + chars.length;
				for (int idx = chars.offset + 1; idx < end; idx++)
				{
					if (chars.chars[idx] == SynonymMap.WORD_SEPARATOR && chars.chars[idx - 1] == SynonymMap
						.WORD_SEPARATOR)
					{
						return true;
					}
				}
				if (chars.chars[chars.offset] == '\u0000')
				{
					return true;
				}
				if (chars.chars[chars.offset + chars.length - 1] == '\u0000')
				{
					return true;
				}
				return false;
			}

			// NOTE: while it's tempting to make this public, since
			// caller's parser likely knows the
			// numInput/numOutputWords, sneaky exceptions, much later
			// on, will result if these values are wrong; so we always
			// recompute ourselves to be safe:
			private void Add(CharsRef input, int numInputWords, CharsRef output, int numOutputWords
				, bool includeOrig)
			{
				// first convert to UTF-8
				if (numInputWords <= 0)
				{
					throw new ArgumentException("numInputWords must be > 0 (got " + numInputWords + ")"
						);
				}
				if (input.length <= 0)
				{
					throw new ArgumentException("input.length must be > 0 (got " + input.length + ")"
						);
				}
				if (numOutputWords <= 0)
				{
					throw new ArgumentException("numOutputWords must be > 0 (got " + numOutputWords +
						 ")");
				}
				if (output.length <= 0)
				{
					throw new ArgumentException("output.length must be > 0 (got " + output.length + ")"
						);
				}
				//HM:revisit 
				//assert !hasHoles(input): "input has holes: " + input;
				//HM:revisit 
				//assert !hasHoles(output): "output has holes: " + output;
				//System.out.println("fmap.add input=" + input + " numInputWords=" + numInputWords + " output=" + output + " numOutputWords=" + numOutputWords);
				UnicodeUtil.UTF16toUTF8(output.chars, output.offset, output.length, utf8Scratch);
				// lookup in hash
				int ord = words.Add(utf8Scratch);
				if (ord < 0)
				{
					// already exists in our hash
					ord = (-ord) - 1;
				}
				//System.out.println("  output=" + output + " old ord=" + ord);
				//System.out.println("  output=" + output + " new ord=" + ord);
				SynonymMap.Builder.MapEntry e = workingSet.Get(input);
				if (e == null)
				{
					e = new SynonymMap.Builder.MapEntry();
					workingSet.Put(CharsRef.DeepCopyOf(input), e);
				}
				// make a copy, since we will keep around in our map    
				e.ords.AddItem(ord);
				e.includeOrig |= includeOrig;
				maxHorizontalContext = Math.Max(maxHorizontalContext, numInputWords);
				maxHorizontalContext = Math.Max(maxHorizontalContext, numOutputWords);
			}

			private int CountWords(CharsRef chars)
			{
				int wordCount = 1;
				int upto = chars.offset;
				int limit = chars.offset + chars.length;
				while (upto < limit)
				{
					if (chars.chars[upto++] == SynonymMap.WORD_SEPARATOR)
					{
						wordCount++;
					}
				}
				return wordCount;
			}

			/// <summary>Add a phrase-&gt;phrase synonym mapping.</summary>
			/// <remarks>
			/// Add a phrase-&gt;phrase synonym mapping.
			/// Phrases are character sequences where words are
			/// separated with character zero (U+0000).  Empty words
			/// (two U+0000s in a row) are not allowed in the input nor
			/// the output!
			/// </remarks>
			/// <param name="input">input phrase</param>
			/// <param name="output">output phrase</param>
			/// <param name="includeOrig">true if the original should be included</param>
			public virtual void Add(CharsRef input, CharsRef output, bool includeOrig)
			{
				Add(input, CountWords(input), output, CountWords(output), includeOrig);
			}

			/// <summary>
			/// Builds an
			/// <see cref="SynonymMap">SynonymMap</see>
			/// and returns it.
			/// </summary>
			/// <exception cref="System.IO.IOException"></exception>
			public virtual SynonymMap Build()
			{
				ByteSequenceOutputs outputs = ByteSequenceOutputs.GetSingleton();
				// TODO: are we using the best sharing options?
				Builder<BytesRef> builder = new Builder<BytesRef>(FST.INPUT_TYPE.BYTE4, outputs);
				BytesRef scratch = new BytesRef(64);
				ByteArrayDataOutput scratchOutput = new ByteArrayDataOutput();
				ICollection<int> dedupSet;
				if (dedup)
				{
					dedupSet = new HashSet<int>();
				}
				else
				{
					dedupSet = null;
				}
				byte[] spare = new byte[5];
				ICollection<CharsRef> keys = workingSet.Keys;
				CharsRef[] sortedKeys = Sharpen.Collections.ToArray(keys, new CharsRef[keys.Count
					]);
				Arrays.Sort(sortedKeys, CharsRef.GetUTF16SortedAsUTF8Comparator());
				IntsRef scratchIntsRef = new IntsRef();
				//System.out.println("fmap.build");
				for (int keyIdx = 0; keyIdx < sortedKeys.Length; keyIdx++)
				{
					CharsRef input = sortedKeys[keyIdx];
					SynonymMap.Builder.MapEntry output = workingSet.Get(input);
					int numEntries = output.ords.Count;
					// output size, assume the worst case
					int estimatedSize = 5 + numEntries * 5;
					// numEntries + one ord for each entry
					scratch.Grow(estimatedSize);
					scratchOutput.Reset(scratch.bytes, scratch.offset, scratch.bytes.Length);
					//HM:revisit 
					//assert scratch.offset == 0;
					// now write our output data:
					int count = 0;
					for (int i = 0; i < numEntries; i++)
					{
						if (dedupSet != null)
						{
							// box once
							int ent = output.ords[i];
							if (dedupSet.Contains(ent))
							{
								continue;
							}
							dedupSet.AddItem(ent);
						}
						scratchOutput.WriteVInt(output.ords[i]);
						count++;
					}
					int pos = scratchOutput.GetPosition();
					scratchOutput.WriteVInt(count << 1 | (output.includeOrig ? 0 : 1));
					int pos2 = scratchOutput.GetPosition();
					int vIntLen = pos2 - pos;
					// Move the count + includeOrig to the front of the byte[]:
					System.Array.Copy(scratch.bytes, pos, spare, 0, vIntLen);
					System.Array.Copy(scratch.bytes, 0, scratch.bytes, vIntLen, pos);
					System.Array.Copy(spare, 0, scratch.bytes, 0, vIntLen);
					if (dedupSet != null)
					{
						dedupSet.Clear();
					}
					scratch.length = scratchOutput.GetPosition() - scratch.offset;
					//System.out.println("  add input=" + input + " output=" + scratch + " offset=" + scratch.offset + " length=" + scratch.length + " count=" + count);
					builder.Add(Lucene.Net.Util.Fst.Util.ToUTF32(input, scratchIntsRef), BytesRef
						.DeepCopyOf(scratch));
				}
				FST<BytesRef> fst = builder.Finish();
				return new SynonymMap(fst, words, maxHorizontalContext);
			}
		}

		/// <summary>Abstraction for parsing synonym files.</summary>
		/// <remarks>Abstraction for parsing synonym files.</remarks>
		/// <lucene.experimental></lucene.experimental>
		public abstract class Parser : SynonymMap.Builder
		{
			private readonly Analyzer analyzer;

			public Parser(bool dedup, Analyzer analyzer) : base(dedup)
			{
				this.analyzer = analyzer;
			}

			/// <summary>
			/// Parse the given input, adding synonyms to the inherited
			/// <see cref="Builder">Builder</see>
			/// .
			/// </summary>
			/// <param name="in">The input to parse</param>
			/// <exception cref="System.IO.IOException"></exception>
			/// <exception cref="Sharpen.ParseException"></exception>
			public abstract void Parse(StreamReader @in);

			/// <summary>
			/// Sugar: analyzes the text with the analyzer and
			/// separates by
			/// <see cref="SynonymMap.WORD_SEPARATOR">SynonymMap.WORD_SEPARATOR</see>
			/// .
			/// reuse and its chars must not be null.
			/// </summary>
			/// <exception cref="System.IO.IOException"></exception>
			public virtual CharsRef Analyze(string text, CharsRef reuse)
			{
				CharTermAttribute termAtt = ts.AddAttribute<CharTermAttribute>();
				PositionIncrementAttribute posIncAtt = ts.AddAttribute<PositionIncrementAttribute
					>();
				ts.Reset();
				reuse.length = 0;
				while (ts.IncrementToken())
				{
					int length = termAtt.Length;
					if (length == 0)
					{
						throw new ArgumentException("term: " + text + " analyzed to a zero-length token");
					}
					if (posIncAtt.GetPositionIncrement() != 1)
					{
						throw new ArgumentException("term: " + text + " analyzed to a token with posinc != 1"
							);
					}
					reuse.Grow(reuse.length + length + 1);
					int end = reuse.offset + reuse.length;
					if (reuse.length > 0)
					{
						reuse.chars[end++] = SynonymMap.WORD_SEPARATOR;
						reuse.length++;
					}
					System.Array.Copy(termAtt.Buffer, 0, reuse.chars, end, length);
					reuse.length += length;
				}
				ts.End();
				if (reuse.length == 0)
				{
					throw new ArgumentException("term: " + text + " was completely eliminated by analyzer"
						);
				}
				return reuse;
			}
		}
	}
}
