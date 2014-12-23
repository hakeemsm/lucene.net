/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	/// <summary>
	/// A binary tokenstream that lets you index a single
	/// binary token (BytesRef value).
	/// </summary>
	/// <remarks>
	/// A binary tokenstream that lets you index a single
	/// binary token (BytesRef value).
	/// </remarks>
	/// <seealso cref="Lucene.Net.Analysis.CannedBinaryTokenStream">Lucene.Net.Analysis.CannedBinaryTokenStream
	/// 	</seealso>
	public sealed class BinaryTokenStream : TokenStream
	{
		private readonly BinaryTokenStream.ByteTermAttribute bytesAtt = AddAttribute<BinaryTokenStream.ByteTermAttribute
			>();

		private readonly BytesRef bytes;

		private bool available = true;

		public BinaryTokenStream(BytesRef bytes)
		{
			// javadocs
			this.bytes = bytes;
		}

		public override bool IncrementToken()
		{
			if (available)
			{
				ClearAttributes();
				available = false;
				bytesAtt.SetBytesRef(bytes);
				return true;
			}
			return false;
		}

		public override void Reset()
		{
			available = true;
		}

		public interface ByteTermAttribute : TermToBytesRefAttribute
		{
			void SetBytesRef(BytesRef bytes);
		}

		public class ByteTermAttributeImpl : AttributeImpl, BinaryTokenStream.ByteTermAttribute
			, TermToBytesRefAttribute
		{
			private BytesRef bytes;

			public virtual void FillBytesRef()
			{
			}

			// no-op: the bytes was already filled by our owner's incrementToken
			public virtual BytesRef GetBytesRef()
			{
				return bytes;
			}

			public virtual void SetBytesRef(BytesRef bytes)
			{
				this.bytes = bytes;
			}

			public override void Clear()
			{
			}

			public override void CopyTo(AttributeImpl target)
			{
				BinaryTokenStream.ByteTermAttributeImpl other = (BinaryTokenStream.ByteTermAttributeImpl
					)target;
				other.bytes = bytes;
			}
		}
	}
}
