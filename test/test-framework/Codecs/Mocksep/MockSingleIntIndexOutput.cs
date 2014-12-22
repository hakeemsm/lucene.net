/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Lucene.Net.Codecs.Mocksep;
using Lucene.Net.Codecs.Sep;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Mocksep
{
	/// <summary>
	/// Writes ints directly to the file (not in blocks) as
	/// vInt.
	/// </summary>
	/// <remarks>
	/// Writes ints directly to the file (not in blocks) as
	/// vInt.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class MockSingleIntIndexOutput : IntIndexOutput
	{
		private readonly IndexOutput @out;

		internal static readonly string CODEC = "SINGLE_INTS";

		internal const int VERSION_START = 0;

		internal const int VERSION_CURRENT = VERSION_START;

		/// <exception cref="System.IO.IOException"></exception>
		public MockSingleIntIndexOutput(Directory dir, string fileName, IOContext context
			)
		{
			@out = dir.CreateOutput(fileName, context);
			bool success = false;
			try
			{
				CodecUtil.WriteHeader(@out, CODEC, VERSION_CURRENT);
				success = true;
			}
			finally
			{
				if (!success)
				{
					IOUtils.CloseWhileHandlingException(@out);
				}
			}
		}

		/// <summary>Write an int to the primary file</summary>
		/// <exception cref="System.IO.IOException"></exception>
		public override void Write(int v)
		{
			@out.WriteVInt(v);
		}

		public override IntIndexOutput.Index Index()
		{
			return new MockSingleIntIndexOutput.MockSingleIntIndexOutputIndex(this);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			@out.Close();
		}

		public override string ToString()
		{
			return "MockSingleIntIndexOutput fp=" + @out.GetFilePointer();
		}

		private class MockSingleIntIndexOutputIndex : IntIndexOutput.Index
		{
			internal long fp;

			internal long lastFP;

			public override void Mark()
			{
				this.fp = this._enclosing.@out.GetFilePointer();
			}

			public override void CopyFrom(IntIndexOutput.Index other, bool copyLast)
			{
				this.fp = ((MockSingleIntIndexOutput.MockSingleIntIndexOutputIndex)other).fp;
				if (copyLast)
				{
					this.lastFP = ((MockSingleIntIndexOutput.MockSingleIntIndexOutputIndex)other).fp;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Write(DataOutput indexOut, bool absolute)
			{
				if (absolute)
				{
					indexOut.WriteVLong(this.fp);
				}
				else
				{
					indexOut.WriteVLong(this.fp - this.lastFP);
				}
				this.lastFP = this.fp;
			}

			public override string ToString()
			{
				return System.Convert.ToString(this.fp);
			}

			internal MockSingleIntIndexOutputIndex(MockSingleIntIndexOutput _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly MockSingleIntIndexOutput _enclosing;
		}
	}
}
