using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.TestFramework.Analysis;
using Lucene.Net.Util;

namespace Lucene.Net.Test.Index
{
	/// <summary>
	/// A binary tokenstream that lets you index a single
	/// binary token (BytesRef value).
	/// </summary>
	/// <remarks>
	/// A binary tokenstream that lets you index a single
	/// binary token (BytesRef value).
	/// </remarks>
	/// <seealso cref="CannedBinaryTokenStream">Lucene.Net.Test.Analysis.CannedBinaryTokenStream
	/// 	</seealso>
	public sealed class BinaryTokenStream : TokenStream
	{
	    private readonly ByteTermAttribute bytesAtt;

		private readonly BytesRef bytes;

		private bool available = true;

		public BinaryTokenStream(BytesRef bytes)
		{
			// javadocs
			this.bytes = bytes;
            bytesAtt = AddAttribute<BinaryTokenStream.ByteTermAttribute>();
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

		public interface ByteTermAttribute : ITermToBytesRefAttribute
		{
			void SetBytesRef(BytesRef bytes);
		}

		public class ByteTermAttributeImpl : ByteTermAttribute
		{
			private BytesRef bytes;

			public virtual void FillBytesRef()
			{
			}

		    public BytesRef BytesRef
            {
		        get { return bytes; }
		        private set { bytes = value; } }

		    // no-op: the bytes was already filled by our owner's incrementToken
			

			public virtual void SetBytesRef(BytesRef bytes)
			{
				this.bytes = bytes;
			}

			public void Clear()
			{
			}

			public void CopyTo(IAttribute target)
			{
				var other = (ByteTermAttributeImpl)target;
				other.bytes = bytes;
			}
		}
	}
}
