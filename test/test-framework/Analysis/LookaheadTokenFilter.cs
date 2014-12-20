/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.TestFramework.Analysis;
using Lucene.Net.TestFramework.Analysis.Tokenattributes;
using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Analysis
{
	/// <summary>
	/// An abstract TokenFilter to make it easier to build graph
	/// token filters requiring some lookahead.
	/// </summary>
	/// <remarks>
	/// An abstract TokenFilter to make it easier to build graph
	/// token filters requiring some lookahead.  This class handles
	/// the details of buffering up tokens, recording them by
	/// position, restoring them, providing access to them, etc.
	/// </remarks>
	public abstract class LookaheadTokenFilter<T> : TokenFilter where T:LookaheadTokenFilter.Position
	{
		private const bool DEBUG = false;

		protected internal readonly PositionIncrementAttribute posIncAtt = AddAttribute<PositionIncrementAttribute
			>();

		protected internal readonly PositionLengthAttribute posLenAtt = AddAttribute<PositionLengthAttribute
			>();

		protected internal readonly OffsetAttribute offsetAtt = AddAttribute<OffsetAttribute
			>();

		protected internal int inputPos;

		protected internal int outputPos;

		protected internal bool end;

		private bool tokenPending;

		private bool insertPending;

		/// <summary>
		/// Holds all state for a single position; subclass this
		/// to record other state at each position.
		/// </summary>
		/// <remarks>
		/// Holds all state for a single position; subclass this
		/// to record other state at each position.
		/// </remarks>
		protected internal class Position : RollingBuffer.Resettable
		{
			public readonly IList<AttributeSource.State> inputTokens = new AList<AttributeSource.State
				>();

			public int nextRead;

			public int startOffset = -1;

			public int endOffset = -1;

			// TODO: cut SynFilter over to this
			// TODO: somehow add "nuke this input token" capability...
			// Position of last read input token:
			// Position of next possible output token to return:
			// True if we hit end from our input:
			// Buffered input tokens at this position:
			// Next buffered token to be returned to consumer:
			// Any token leaving from this position should have this startOffset:
			// Any token arriving to this position should have this endOffset:
			public virtual void Reset()
			{
				inputTokens.Clear();
				nextRead = 0;
				startOffset = -1;
				endOffset = -1;
			}

			public virtual void Add(AttributeSource.State state)
			{
				inputTokens.AddItem(state);
			}

			public virtual AttributeSource.State NextState()
			{
				//HM:revisit 
				//assert nextRead < inputTokens.size();
				return inputTokens[nextRead++];
			}
		}

		protected LookaheadTokenFilter(TokenStream input) : base(input)
		{
			positions = new _RollingBuffer_121(this);
		}

		/// <summary>
		/// Call this only from within afterPosition, to insert a new
		/// token.
		/// </summary>
		/// <remarks>
		/// Call this only from within afterPosition, to insert a new
		/// token.  After calling this you should set any
		/// necessary token you need.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual void InsertToken()
		{
			if (tokenPending)
			{
				positions.Get(inputPos).Add(CaptureState());
				tokenPending = false;
			}
			//HM:revisit 
			//assert !insertPending;
			insertPending = true;
		}

		/// <summary>
		/// This is called when all input tokens leaving a given
		/// position have been returned.
		/// </summary>
		/// <remarks>
		/// This is called when all input tokens leaving a given
		/// position have been returned.  Override this and
		/// call insertToken and then set whichever token's
		/// attributes you want, if you want to inject
		/// a token starting from this position.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual void AfterPosition()
		{
		}

		protected internal abstract T NewPosition();

		private sealed class _RollingBuffer_121 : RollingBuffer<T>
		{
			public _RollingBuffer_121(LookaheadTokenFilter<T> _enclosing)
			{
				this._enclosing = _enclosing;
			}

			protected override T NewInstance()
			{
				return this._enclosing.NewPosition();
			}

			private readonly LookaheadTokenFilter<T> _enclosing;
		}

		protected internal readonly RollingBuffer<T> positions;

		/// <summary>Returns true if there is a new token.</summary>
		/// <remarks>Returns true if there is a new token.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual bool PeekToken()
		{
			//HM:revisit 
			//assert !end;
			//HM:revisit 
			//assert inputPos == -1 || outputPos <= inputPos;
			if (tokenPending)
			{
				positions.Get(inputPos).Add(CaptureState());
				tokenPending = false;
			}
			bool gotToken = input.IncrementToken();
			if (gotToken)
			{
				inputPos += posIncAtt.GetPositionIncrement();
				//HM:revisit 
				//assert inputPos >= 0;
				LookaheadTokenFilter.Position startPosData = positions.Get(inputPos);
				LookaheadTokenFilter.Position endPosData = positions.Get(inputPos + posLenAtt.GetPositionLength
					());
				int startOffset = offsetAtt.StartOffset();
				if (startPosData.startOffset == -1)
				{
					startPosData.startOffset = startOffset;
				}
				// Make sure our input isn't messing up offsets:
				//HM:revisit 
				//assert startPosData.startOffset == startOffset: "prev startOffset=" + startPosData.startOffset + " vs new startOffset=" + startOffset + " inputPos=" + inputPos;
				int endOffset = offsetAtt.EndOffset();
				if (endPosData.endOffset == -1)
				{
					endPosData.endOffset = endOffset;
				}
				// Make sure our input isn't messing up offsets:
				//HM:revisit 
				//assert endPosData.endOffset == endOffset: "prev endOffset=" + endPosData.endOffset + " vs new endOffset=" + endOffset + " inputPos=" + inputPos;
				tokenPending = true;
			}
			else
			{
				end = true;
			}
			return gotToken;
		}

		/// <summary>
		/// Call this when you are done looking ahead; it will set
		/// the next token to return.
		/// </summary>
		/// <remarks>
		/// Call this when you are done looking ahead; it will set
		/// the next token to return.  Return the boolean back to
		/// the caller.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual bool NextToken()
		{
			//System.out.println("  nextToken: tokenPending=" + tokenPending);
			LookaheadTokenFilter.Position posData = positions.Get(outputPos);
			// While loop here in case we have to
			// skip over a hole from the input:
			while (true)
			{
				//System.out.println("    check buffer @ outputPos=" +
				//outputPos + " inputPos=" + inputPos + " nextRead=" +
				//posData.nextRead + " vs size=" +
				//posData.inputTokens.size());
				// See if we have a previously buffered token to
				// return at the current position:
				if (posData.nextRead < posData.inputTokens.Count)
				{
					// This position has buffered tokens to serve up:
					if (tokenPending)
					{
						positions.Get(inputPos).Add(CaptureState());
						tokenPending = false;
					}
					RestoreState(positions.Get(outputPos).NextState());
					//System.out.println("      return!");
					return true;
				}
				if (inputPos == -1 || outputPos == inputPos)
				{
					// No more buffered tokens:
					// We may still get input tokens at this position
					//System.out.println("    break buffer");
					if (tokenPending)
					{
						// Fast path: just return token we had just incr'd,
						// without having captured/restored its state:
						tokenPending = false;
						return true;
					}
					else
					{
						if (end || !PeekToken())
						{
							AfterPosition();
							if (insertPending)
							{
								// Subclass inserted a token at this same
								// position:
								//HM:revisit 
								//assert insertedTokenConsistent();
								insertPending = false;
								return true;
							}
							return false;
						}
					}
				}
				else
				{
					if (posData.startOffset != -1)
					{
						// This position had at least one token leaving
						AfterPosition();
						if (insertPending)
						{
							// Subclass inserted a token at this same
							// position:
							//HM:revisit 
							//assert insertedTokenConsistent();
							insertPending = false;
							return true;
						}
					}
					// Done with this position; move on:
					outputPos++;
					positions.FreeBefore(outputPos);
					posData = positions.Get(outputPos);
				}
			}
		}

		// If subclass inserted a token, make sure it had in fact
		// looked ahead enough:
		private bool InsertedTokenConsistent()
		{
			int posLen = posLenAtt.GetPositionLength();
			LookaheadTokenFilter.Position endPosData = positions.Get(outputPos + posLen);
			//HM:revisit 
			//assert endPosData.endOffset != -1;
			//HM:revisit 
			//assert offsetAtt.endOffset() == endPosData.endOffset: "offsetAtt.endOffset=" + offsetAtt.endOffset() + " vs expected=" + endPosData.endOffset;
			return true;
		}

		// TODO: end()?
		// TODO: close()?
		/// <exception cref="System.IO.IOException"></exception>
		public override void Reset()
		{
			base.Reset();
			positions.Reset();
			inputPos = -1;
			outputPos = 0;
			tokenPending = false;
			end = false;
		}
	}
}
