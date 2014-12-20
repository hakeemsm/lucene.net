/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Codecs.Mocksep;
using Org.Apache.Lucene.Codecs.Sep;
using Org.Apache.Lucene.Store;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Mocksep
{
	/// <summary>
	/// Reads IndexInputs written with
	/// <see cref="MockSingleIntIndexOutput">MockSingleIntIndexOutput</see>
	/// .  NOTE: this class is just for
	/// demonstration purposes (it is a very slow way to read a
	/// block of ints).
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class MockSingleIntIndexInput : IntIndexInput
	{
		private readonly IndexInput @in;

		/// <exception cref="System.IO.IOException"></exception>
		public MockSingleIntIndexInput(Directory dir, string fileName, IOContext context)
		{
			@in = dir.OpenInput(fileName, context);
			CodecUtil.CheckHeader(@in, MockSingleIntIndexOutput.CODEC, MockSingleIntIndexOutput
				.VERSION_START, MockSingleIntIndexOutput.VERSION_START);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override IntIndexInput.Reader Reader()
		{
			return new MockSingleIntIndexInput.Reader(((IndexInput)@in.Clone()));
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			@in.Close();
		}

		/// <summary>Just reads a vInt directly from the file.</summary>
		/// <remarks>Just reads a vInt directly from the file.</remarks>
		public class Reader : IntIndexInput.Reader
		{
			private readonly IndexInput @in;

			public Reader(IndexInput @in)
			{
				// clone:
				this.@in = @in;
			}

			/// <summary>Reads next single int</summary>
			/// <exception cref="System.IO.IOException"></exception>
			public override int Next()
			{
				//System.out.println("msii.next() fp=" + in.getFilePointer() + " vs " + in.length());
				return @in.ReadVInt();
			}
		}

		internal class MockSingleIntIndexInputIndex : IntIndexInput.Index
		{
			private long fp;

			/// <exception cref="System.IO.IOException"></exception>
			public override void Read(DataInput indexIn, bool absolute)
			{
				if (absolute)
				{
					this.fp = indexIn.ReadVLong();
				}
				else
				{
					this.fp += indexIn.ReadVLong();
				}
			}

			public override void CopyFrom(IntIndexInput.Index other)
			{
				this.fp = ((MockSingleIntIndexInput.MockSingleIntIndexInputIndex)other).fp;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Seek(IntIndexInput.Reader other)
			{
				((MockSingleIntIndexInput.Reader)other).@in.Seek(this.fp);
			}

			public override string ToString()
			{
				return System.Convert.ToString(this.fp);
			}

			public override IntIndexInput.Index Clone()
			{
				MockSingleIntIndexInput.MockSingleIntIndexInputIndex other = new MockSingleIntIndexInput.MockSingleIntIndexInputIndex
					(this);
				other.fp = this.fp;
				return other;
			}

			internal MockSingleIntIndexInputIndex(MockSingleIntIndexInput _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly MockSingleIntIndexInput _enclosing;
		}

		public override IntIndexInput.Index Index()
		{
			return new MockSingleIntIndexInput.MockSingleIntIndexInputIndex(this);
		}
	}
}
