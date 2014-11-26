/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Synonym;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using Sharpen;

namespace Lucene.Net.Analysis.Synonym
{
	/// <summary>Matches single or multi word synonyms in a token stream.</summary>
	/// <remarks>
	/// Matches single or multi word synonyms in a token stream.
	/// This token stream cannot properly handle position
	/// increments != 1, ie, you should place this filter before
	/// filtering out stop words.
	/// <p>Note that with the current implementation, parsing is
	/// greedy, so whenever multiple parses would apply, the rule
	/// starting the earliest and parsing the most tokens wins.
	/// For example if you have these rules:
	/// <pre>
	/// a -&gt; x
	/// a b -&gt; y
	/// b c d -&gt; z
	/// </pre>
	/// Then input <code>a b c d e</code> parses to <code>y b c
	/// d</code>, ie the 2nd rule "wins" because it started
	/// earliest and matched the most input tokens of other rules
	/// starting at that point.</p>
	/// <p>A future improvement to this filter could allow
	/// non-greedy parsing, such that the 3rd rule would win, and
	/// also separately allow multiple parses, such that all 3
	/// rules would match, perhaps even on a rule by rule
	/// basis.</p>
	/// <p><b>NOTE</b>: when a match occurs, the output tokens
	/// associated with the matching rule are "stacked" on top of
	/// the input stream (if the rule had
	/// <code>keepOrig=true</code>) and also on top of another
	/// matched rule's output tokens.  This is not a correct
	/// solution, as really the output should be an arbitrary
	/// graph/lattice.  For example, with the above match, you
	/// would expect an exact <code>PhraseQuery</code> <code>"y b
	/// c"</code> to match the parsed tokens, but it will fail to
	/// do so.  This limitation is necessary because Lucene's
	/// TokenStream (and index) cannot yet represent an arbitrary
	/// graph.</p>
	/// <p><b>NOTE</b>: If multiple incoming tokens arrive on the
	/// same position, only the first token at that position is
	/// used for parsing.  Subsequent tokens simply pass through
	/// and are not parsed.  A future improvement would be to
	/// allow these tokens to also be matched.</p>
	/// </remarks>
	public sealed class SynonymFilter : TokenFilter
	{
		public static readonly string TYPE_SYNONYM = "SYNONYM";

		private readonly SynonymMap synonyms;

		private readonly bool ignoreCase;

		private readonly int rollBufferSize;

		private int captureCount;

		private readonly CharTermAttribute termAtt = AddAttribute<CharTermAttribute>();

		private readonly PositionIncrementAttribute posIncrAtt = AddAttribute<PositionIncrementAttribute
			>();

		private readonly PositionLengthAttribute posLenAtt = AddAttribute<PositionLengthAttribute
			>();

		private readonly TypeAttribute typeAtt = AddAttribute<TypeAttribute>();

		private readonly OffsetAttribute offsetAtt = AddAttribute<OffsetAttribute>();

		private int inputSkipCount;

		private class PendingInput
		{
			internal readonly CharsRef term = new CharsRef();

			internal AttributeSource.State state;

			internal bool keepOrig;

			internal bool matched;

			internal bool consumed = true;

			internal int startOffset;

			internal int endOffset;

			// TODO: maybe we should resolve token -> wordID then run
			// FST on wordIDs, for better perf?
			// TODO: a more efficient approach would be Aho/Corasick's
			// algorithm
			// http://en.wikipedia.org/wiki/Aho%E2%80%93Corasick_string_matching_algorithm
			// It improves over the current approach here
			// because it does not fully re-start matching at every
			// token.  For example if one pattern is "a b c x"
			// and another is "b c d" and the input is "a b c d", on
			// trying to parse "a b c x" but failing when you got to x,
			// rather than starting over again your really should
			// immediately recognize that "b c d" matches at the next
			// input.  I suspect this won't matter that much in
			// practice, but it's possible on some set of synonyms it
			// will.  We'd have to modify Aho/Corasick to enforce our
			// conflict resolving (eg greedy matching) because that algo
			// finds all matches.  This really amounts to adding a .*
			// closure to the FST and then determinizing it.
			// TODO: we should set PositionLengthAttr too...
			// How many future input tokens have already been matched
			// to a synonym; because the matching is "greedy" we don't
			// try to do any more matching for such tokens:
			// Hold all buffered (read ahead) stacked input tokens for
			// a future position.  When multiple tokens are at the
			// same position, we only store (and match against) the
			// term for the first token at the position, but capture
			// state for (and enumerate) all other tokens at this
			// position:
			public virtual void Reset()
			{
				state = null;
				consumed = true;
				keepOrig = false;
				matched = false;
			}
		}

		private readonly SynonymFilter.PendingInput[] futureInputs;

		private class PendingOutputs
		{
			internal CharsRef[] outputs;

			internal int[] endOffsets;

			internal int[] posLengths;

			internal int upto;

			internal int count;

			internal int posIncr = 1;

			internal int lastEndOffset;

			internal int lastPosLength;

			public PendingOutputs()
			{
				// Rolling buffer, holding pending input tokens we had to
				// clone because we needed to look ahead, indexed by
				// position:
				// Holds pending output synonyms for one future position:
				outputs = new CharsRef[1];
				endOffsets = new int[1];
				posLengths = new int[1];
			}

			public virtual void Reset()
			{
				upto = count = 0;
				posIncr = 1;
			}

			public virtual CharsRef PullNext()
			{
				//HM:revisit 
				//assert upto < count;
				lastEndOffset = endOffsets[upto];
				lastPosLength = posLengths[upto];
				CharsRef result = outputs[upto++];
				posIncr = 0;
				if (upto == count)
				{
					Reset();
				}
				return result;
			}

			public virtual int GetLastEndOffset()
			{
				return lastEndOffset;
			}

			public virtual int GetLastPosLength()
			{
				return lastPosLength;
			}

			public virtual void Add(char[] output, int offset, int len, int endOffset, int posLength
				)
			{
				if (count == outputs.Length)
				{
					CharsRef[] next = new CharsRef[ArrayUtil.Oversize(1 + count, RamUsageEstimator.NUM_BYTES_OBJECT_REF
						)];
					System.Array.Copy(outputs, 0, next, 0, count);
					outputs = next;
				}
				if (count == endOffsets.Length)
				{
					int[] next = new int[ArrayUtil.Oversize(1 + count, RamUsageEstimator.NUM_BYTES_INT
						)];
					System.Array.Copy(endOffsets, 0, next, 0, count);
					endOffsets = next;
				}
				if (count == posLengths.Length)
				{
					int[] next = new int[ArrayUtil.Oversize(1 + count, RamUsageEstimator.NUM_BYTES_INT
						)];
					System.Array.Copy(posLengths, 0, next, 0, count);
					posLengths = next;
				}
				if (outputs[count] == null)
				{
					outputs[count] = new CharsRef();
				}
				outputs[count].CopyChars(output, offset, len);
				// endOffset can be -1, in which case we should simply
				// use the endOffset of the input token, or X >= 0, in
				// which case we use X as the endOffset for this output
				endOffsets[count] = endOffset;
				posLengths[count] = posLength;
				count++;
			}
		}

		private readonly ByteArrayDataInput bytesReader = new ByteArrayDataInput();

		private readonly SynonymFilter.PendingOutputs[] futureOutputs;

		private int nextWrite;

		private int nextRead;

		private bool finished;

		private readonly FST.Arc<BytesRef> scratchArc;

		private readonly FST<BytesRef> fst;

		private readonly FST.BytesReader fstReader;

		private readonly BytesRef scratchBytes = new BytesRef();

		private readonly CharsRef scratchChars = new CharsRef();

		/// <param name="input">input tokenstream</param>
		/// <param name="synonyms">synonym map</param>
		/// <param name="ignoreCase">
		/// case-folds input for matching with
		/// <see cref="System.Char.ToLower(int)">System.Char.ToLower(int)</see>
		/// .
		/// Note, if you set this to true, its your responsibility to lowercase
		/// the input entries when you create the
		/// <see cref="SynonymMap">SynonymMap</see>
		/// </param>
		public SynonymFilter(TokenStream input, SynonymMap synonyms, bool ignoreCase) : base
			(input)
		{
			// Rolling buffer, holding stack of pending synonym
			// outputs, indexed by position:
			// Where (in rolling buffers) to write next input saved state:
			// Where (in rolling buffers) to read next input saved state:
			// True once we've read last token
			this.synonyms = synonyms;
			this.ignoreCase = ignoreCase;
			this.fst = synonyms.fst;
			if (fst == null)
			{
				throw new ArgumentException("fst must be non-null");
			}
			this.fstReader = fst.GetBytesReader();
			// Must be 1+ so that when roll buffer is at full
			// lookahead we can distinguish this full buffer from
			// the empty buffer:
			rollBufferSize = 1 + synonyms.maxHorizontalContext;
			futureInputs = new SynonymFilter.PendingInput[rollBufferSize];
			futureOutputs = new SynonymFilter.PendingOutputs[rollBufferSize];
			for (int pos = 0; pos < rollBufferSize; pos++)
			{
				futureInputs[pos] = new SynonymFilter.PendingInput();
				futureOutputs[pos] = new SynonymFilter.PendingOutputs();
			}
			//System.out.println("FSTFilt maxH=" + synonyms.maxHorizontalContext);
			scratchArc = new FST.Arc<BytesRef>();
		}

		private void Capture()
		{
			captureCount++;
			//System.out.println("  capture slot=" + nextWrite);
			SynonymFilter.PendingInput input = futureInputs[nextWrite];
			input.state = CaptureState();
			input.consumed = false;
			input.term.CopyChars(termAtt.Buffer, 0, termAtt.Length);
			nextWrite = RollIncr(nextWrite);
		}

		private int lastStartOffset;

		private int lastEndOffset;

		// Buffer head should never catch up to tail:
		//HM:revisit 
		//assert nextWrite != nextRead;
		/// <exception cref="System.IO.IOException"></exception>
		private void Parse()
		{
			//System.out.println("\nS: parse");
			//HM:revisit 
			//assert inputSkipCount == 0;
			int curNextRead = nextRead;
			// Holds the longest match we've seen so far:
			BytesRef matchOutput = null;
			int matchInputLength = 0;
			int matchEndOffset = -1;
			BytesRef pendingOutput = fst.outputs.GetNoOutput();
			fst.GetFirstArc(scratchArc);
			//HM:revisit 
			//assert scratchArc.output == fst.outputs.getNoOutput();
			int tokenCount = 0;
			while (true)
			{
				// Pull next token's chars:
				char[] buffer;
				int bufferLen;
				//System.out.println("  cycle nextRead=" + curNextRead + " nextWrite=" + nextWrite);
				int inputEndOffset = 0;
				if (curNextRead == nextWrite)
				{
					// We used up our lookahead buffer of input tokens
					// -- pull next real input token:
					if (finished)
					{
						break;
					}
					else
					{
						//System.out.println("  input.incrToken");
						//HM:revisit 
						//assert futureInputs[nextWrite].consumed;
						// Not correct: a syn match whose output is longer
						// than its input can set future inputs keepOrig
						// to true:
						//
						//HM:revisit 
						//assert !futureInputs[nextWrite].keepOrig;
						if (input.IncrementToken())
						{
							buffer = termAtt.Buffer;
							bufferLen = termAtt.Length;
							SynonymFilter.PendingInput input = futureInputs[nextWrite];
							lastStartOffset = input.startOffset = offsetAtt.StartOffset();
							lastEndOffset = input.endOffset = offsetAtt.EndOffset();
							inputEndOffset = input.endOffset;
							//System.out.println("  new token=" + new String(buffer, 0, bufferLen));
							if (nextRead != nextWrite)
							{
								Capture();
							}
							else
							{
								input.consumed = false;
							}
						}
						else
						{
							// No more input tokens
							//System.out.println("      set end");
							finished = true;
							break;
						}
					}
				}
				else
				{
					// Still in our lookahead
					buffer = futureInputs[curNextRead].term.chars;
					bufferLen = futureInputs[curNextRead].term.length;
					inputEndOffset = futureInputs[curNextRead].endOffset;
				}
				//System.out.println("  old token=" + new String(buffer, 0, bufferLen));
				tokenCount++;
				// Run each char in this token through the FST:
				int bufUpto = 0;
				while (bufUpto < bufferLen)
				{
					int codePoint = char.CodePointAt(buffer, bufUpto, bufferLen);
					if (fst.FindTargetArc(ignoreCase ? System.Char.ToLower(codePoint) : codePoint, scratchArc
						, scratchArc, fstReader) == null)
					{
						//System.out.println("    stop");
						goto byToken_break;
					}
					// Accum the output
					pendingOutput = fst.outputs.Add(pendingOutput, scratchArc.output);
					//System.out.println("    char=" + buffer[bufUpto] + " output=" + pendingOutput + " arc.output=" + scratchArc.output);
					bufUpto += char.CharCount(codePoint);
				}
				// OK, entire token matched; now see if this is a final
				// state:
				if (scratchArc.IsFinal())
				{
					matchOutput = fst.outputs.Add(pendingOutput, scratchArc.nextFinalOutput);
					matchInputLength = tokenCount;
					matchEndOffset = inputEndOffset;
				}
				//System.out.println("  found matchLength=" + matchInputLength + " output=" + matchOutput);
				// See if the FST wants to continue matching (ie, needs to
				// see the next input token):
				if (fst.FindTargetArc(SynonymMap.WORD_SEPARATOR, scratchArc, scratchArc, fstReader
					) == null)
				{
					// No further rules can match here; we're done
					// searching for matching rules starting at the
					// current input position.
					break;
				}
				else
				{
					// More matching is possible -- accum the output (if
					// any) of the WORD_SEP arc:
					pendingOutput = fst.outputs.Add(pendingOutput, scratchArc.output);
					if (nextRead == nextWrite)
					{
						Capture();
					}
				}
				curNextRead = RollIncr(curNextRead);
byToken_continue: ;
			}
byToken_break: ;
			if (nextRead == nextWrite && !finished)
			{
				//System.out.println("  skip write slot=" + nextWrite);
				nextWrite = RollIncr(nextWrite);
			}
			if (matchOutput != null)
			{
				//System.out.println("  add matchLength=" + matchInputLength + " output=" + matchOutput);
				inputSkipCount = matchInputLength;
				AddOutput(matchOutput, matchInputLength, matchEndOffset);
			}
			else
			{
				if (nextRead != nextWrite)
				{
					// Even though we had no match here, we set to 1
					// because we need to skip current input token before
					// trying to match again:
					inputSkipCount = 1;
				}
			}
		}

		//HM:revisit 
		//assert finished;
		//System.out.println("  parse done inputSkipCount=" + inputSkipCount + " nextRead=" + nextRead + " nextWrite=" + nextWrite);
		// Interleaves all output tokens onto the futureOutputs:
		private void AddOutput(BytesRef bytes, int matchInputLength, int matchEndOffset)
		{
			bytesReader.Reset(bytes.bytes, bytes.offset, bytes.length);
			int code = bytesReader.ReadVInt();
			bool keepOrig = (code & unchecked((int)(0x1))) == 0;
			int count = (int)(((uint)code) >> 1);
			//System.out.println("  addOutput count=" + count + " keepOrig=" + keepOrig);
			for (int outputIDX = 0; outputIDX < count; outputIDX++)
			{
				synonyms.words.Get(bytesReader.ReadVInt(), scratchBytes);
				//System.out.println("    outIDX=" + outputIDX + " bytes=" + scratchBytes.length);
				UnicodeUtil.UTF8toUTF16(scratchBytes, scratchChars);
				int lastStart = scratchChars.offset;
				int chEnd = lastStart + scratchChars.length;
				int outputUpto = nextRead;
				for (int chIDX = lastStart; chIDX <= chEnd; chIDX++)
				{
					if (chIDX == chEnd || scratchChars.chars[chIDX] == SynonymMap.WORD_SEPARATOR)
					{
						int outputLen = chIDX - lastStart;
						// Caller is not allowed to have empty string in
						// the output:
						//HM:revisit 
						//assert outputLen > 0: "output contains empty string: " + scratchChars;
						int endOffset;
						int posLen;
						if (chIDX == chEnd && lastStart == scratchChars.offset)
						{
							// This rule had a single output token, so, we set
							// this output's endOffset to the current
							// endOffset (ie, endOffset of the last input
							// token it matched):
							endOffset = matchEndOffset;
							posLen = keepOrig ? matchInputLength : 1;
						}
						else
						{
							// This rule has more than one output token; we
							// can't pick any particular endOffset for this
							// case, so, we inherit the endOffset for the
							// input token which this output overlaps:
							endOffset = -1;
							posLen = 1;
						}
						futureOutputs[outputUpto].Add(scratchChars.chars, lastStart, outputLen, endOffset
							, posLen);
						//System.out.println("      " + new String(scratchChars.chars, lastStart, outputLen) + " outputUpto=" + outputUpto);
						lastStart = 1 + chIDX;
						//System.out.println("  slot=" + outputUpto + " keepOrig=" + keepOrig);
						outputUpto = RollIncr(outputUpto);
					}
				}
			}
			//HM:revisit 
			//assert futureOutputs[outputUpto].posIncr == 1: "outputUpto=" + outputUpto + " vs nextWrite=" + nextWrite;
			int upto = nextRead;
			for (int idx = 0; idx < matchInputLength; idx++)
			{
				futureInputs[upto].keepOrig |= keepOrig;
				futureInputs[upto].matched = true;
				upto = RollIncr(upto);
			}
		}

		// ++ mod rollBufferSize
		private int RollIncr(int count)
		{
			count++;
			if (count == rollBufferSize)
			{
				return 0;
			}
			else
			{
				return count;
			}
		}

		// for testing
		internal int GetCaptureCount()
		{
			return captureCount;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool IncrementToken()
		{
			//System.out.println("\nS: incrToken inputSkipCount=" + inputSkipCount + " nextRead=" + nextRead + " nextWrite=" + nextWrite);
			while (true)
			{
				// First play back any buffered future inputs/outputs
				// w/o running parsing again:
				while (inputSkipCount != 0)
				{
					// At each position, we first output the original
					// token
					// TODO: maybe just a PendingState class, holding
					// both input & outputs?
					SynonymFilter.PendingInput input = futureInputs[nextRead];
					SynonymFilter.PendingOutputs outputs = futureOutputs[nextRead];
					//System.out.println("  cycle nextRead=" + nextRead + " nextWrite=" + nextWrite + " inputSkipCount="+ inputSkipCount + " input.keepOrig=" + input.keepOrig + " input.consumed=" + input.consumed + " input.state=" + input.state);
					if (!input.consumed && (input.keepOrig || !input.matched))
					{
						if (input.state != null)
						{
							// Return a previously saved token (because we
							// had to lookahead):
							RestoreState(input.state);
						}
						// Pass-through case: return token we just pulled
						// but didn't capture:
						//HM:revisit 
						//assert inputSkipCount == 1: "inputSkipCount=" + inputSkipCount + " nextRead=" + nextRead;
						input.Reset();
						if (outputs.count > 0)
						{
							outputs.posIncr = 0;
						}
						else
						{
							nextRead = RollIncr(nextRead);
							inputSkipCount--;
						}
						//System.out.println("  return token=" + termAtt.toString());
						return true;
					}
					else
					{
						if (outputs.upto < outputs.count)
						{
							// Still have pending outputs to replay at this
							// position
							input.Reset();
							int posIncr = outputs.posIncr;
							CharsRef output = outputs.PullNext();
							ClearAttributes();
							termAtt.CopyBuffer(output.chars, output.offset, output.length);
							typeAtt.SetType(TYPE_SYNONYM);
							int endOffset = outputs.GetLastEndOffset();
							if (endOffset == -1)
							{
								endOffset = input.endOffset;
							}
							offsetAtt.SetOffset(input.startOffset, endOffset);
							posIncrAtt.SetPositionIncrement(posIncr);
							posLenAtt.SetPositionLength(outputs.GetLastPosLength());
							if (outputs.count == 0)
							{
								// Done with the buffered input and all outputs at
								// this position
								nextRead = RollIncr(nextRead);
								inputSkipCount--;
							}
							//System.out.println("  return token=" + termAtt.toString());
							return true;
						}
						else
						{
							// Done with the buffered input and all outputs at
							// this position
							input.Reset();
							nextRead = RollIncr(nextRead);
							inputSkipCount--;
						}
					}
				}
				if (finished && nextRead == nextWrite)
				{
					// End case: if any output syns went beyond end of
					// input stream, enumerate them now:
					SynonymFilter.PendingOutputs outputs = futureOutputs[nextRead];
					if (outputs.upto < outputs.count)
					{
						int posIncr = outputs.posIncr;
						CharsRef output = outputs.PullNext();
						futureInputs[nextRead].Reset();
						if (outputs.count == 0)
						{
							nextWrite = nextRead = RollIncr(nextRead);
						}
						ClearAttributes();
						// Keep offset from last input token:
						offsetAtt.SetOffset(lastStartOffset, lastEndOffset);
						termAtt.CopyBuffer(output.chars, output.offset, output.length);
						typeAtt.SetType(TYPE_SYNONYM);
						//System.out.println("  set posIncr=" + outputs.posIncr + " outputs=" + outputs);
						posIncrAtt.SetPositionIncrement(posIncr);
						//System.out.println("  return token=" + termAtt.toString());
						return true;
					}
					else
					{
						return false;
					}
				}
				// Find new synonym matches:
				Parse();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Reset()
		{
			base.Reset();
			captureCount = 0;
			finished = false;
			inputSkipCount = 0;
			nextRead = nextWrite = 0;
			// In normal usage these resets would not be needed,
			// since they reset-as-they-are-consumed, but the app
			// may not consume all input tokens (or we might hit an
			// exception), in which case we have leftover state
			// here:
			foreach (SynonymFilter.PendingInput input in futureInputs)
			{
				input.Reset();
			}
			foreach (SynonymFilter.PendingOutputs output in futureOutputs)
			{
				output.Reset();
			}
		}
	}
}
