using System;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;
using Attribute = Lucene.Net.Util.Attribute;

namespace Lucene.Net.TestFramework.Analysis
{
	/// <summary>
	/// TokenStream from a canned list of binary (BytesRef-based)
	/// tokens.
	/// </summary>
	/// <remarks>
	/// TokenStream from a canned list of binary (BytesRef-based)
	/// tokens.
	/// </remarks>
	public sealed class CannedBinaryTokenStream : TokenStream
	{
		/// <summary>Represents a binary token.</summary>
		/// <remarks>Represents a binary token.</remarks>
		public sealed class BinaryToken
		{
			internal BytesRef term;

			internal int posInc;

			internal int posLen;

			internal int startOffset;

			internal int endOffset;

			public BinaryToken(BytesRef term)
			{
				this.term = term;
				this.posInc = 1;
				this.posLen = 1;
                
			}

			public BinaryToken(BytesRef term, int posInc, int posLen)
			{
				this.term = term;
				this.posInc = posInc;
				this.posLen = posLen;
			}
		}

		private readonly BinaryToken[] tokens;

		private int upto = 0;

	    private readonly BinaryTermAttribute termAtt;

	    private readonly PositionIncrementAttribute posIncrAtt;

	    private readonly PositionLengthAttribute posLengthAtt;

	    private readonly OffsetAttribute offsetAtt;

	    public CannedBinaryTokenStream()
	    {
            termAtt = AddAttribute<BinaryTermAttribute>();
            posIncrAtt = AddAttribute<PositionIncrementAttribute>();
            posLengthAtt = AddAttribute<PositionLengthAttribute>();
            offsetAtt = AddAttribute<OffsetAttribute>();
	    }

		/// <summary>
		/// An attribute extending
		/// <see cref="Lucene.Net.TestFramework.Analysis.Tokenattributes.TermToBytesRefAttribute">Lucene.Net.TestFramework.Analysis.Tokenattributes.TermToBytesRefAttribute
		/// 	</see>
		/// but exposing
		/// <see cref="SetBytesRef(BytesRef)">SetBytesRef(Lucene.Net.TestFramework.Util.BytesRef)
		/// 	</see>
		/// method.
		/// </summary>
		public interface BinaryTermAttribute : ITermToBytesRefAttribute
		{
			/// <summary>Set the current binary value.</summary>
			/// <remarks>Set the current binary value.</remarks>
			void SetBytesRef(BytesRef bytes);
		}

		/// <summary>
		/// Implementation for
		/// <see cref="BinaryTermAttribute">BinaryTermAttribute</see>
		/// .
		/// </summary>
		public sealed class BinaryTermAttributeImpl : Attribute, BinaryTermAttribute, ITermToBytesRefAttribute
		{
			private readonly BytesRef bytes = new BytesRef();

			public void FillBytesRef()
			{
			}

			// no-op: we already filled externally during owner's incrementToken
			public BytesRef BytesRef
			{
			    get { return bytes; }
			}

			public void SetBytesRef(BytesRef bytes)
			{
				this.bytes.CopyBytes(bytes);
			}

			public override void Clear()
			{
			}

			public override bool Equals(object other)
			{
				return other == this;
			}

			public override int GetHashCode()
			{
			    return 31*bytes.length; //.NET Port
			}

			public override void CopyTo(Attribute target)
			{
				BinaryTermAttributeImpl other = (BinaryTermAttributeImpl)target;
				other.bytes.CopyBytes(bytes);
			}

			public override object Clone()
			{
				throw new NotSupportedException();
			}
		}

		public CannedBinaryTokenStream(params CannedBinaryTokenStream.BinaryToken[] tokens
			) : base()
		{
			this.tokens = tokens;
		}

		public override bool IncrementToken()
		{
			if (upto < tokens.Length)
			{
				CannedBinaryTokenStream.BinaryToken token = tokens[upto++];
				// TODO: can we just capture/restoreState so
				// we get all attrs...?
				ClearAttributes();
				termAtt.SetBytesRef(token.term);
				posIncrAtt.PositionIncrement = token.posInc;
				posLengthAtt.PositionLength = token.posLen;
				offsetAtt.SetOffset(token.startOffset, token.endOffset);
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}
