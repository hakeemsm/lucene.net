/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Codecs.Sep;
using Lucene.Net.Store;
using Sharpen;

namespace Lucene.Net.Codecs.Sep
{
	/// <summary>Defines basic API for writing ints to an IndexOutput.</summary>
	/// <remarks>
	/// Defines basic API for writing ints to an IndexOutput.
	/// IntBlockCodec interacts with this API. @see
	/// IntBlockReader
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class IntIndexInput : IDisposable
	{
		/// <exception cref="System.IO.IOException"></exception>
		public abstract IntIndexInput.Reader Reader();

		/// <exception cref="System.IO.IOException"></exception>
		public abstract void Close();

		/// <exception cref="System.IO.IOException"></exception>
		public abstract IntIndexInput.Index Index();

		/// <summary>
		/// Records a single skip-point in the
		/// <see cref="Reader">Reader</see>
		/// .
		/// </summary>
		public abstract class Index
		{
			/// <exception cref="System.IO.IOException"></exception>
			public abstract void Read(DataInput indexIn, bool absolute);

			/// <summary>Seeks primary stream to the last read offset</summary>
			/// <exception cref="System.IO.IOException"></exception>
			public abstract void Seek(IntIndexInput.Reader stream);

			public abstract void CopyFrom(IntIndexInput.Index other);

			public abstract IntIndexInput.Index Clone();
		}

		/// <summary>Reads int values.</summary>
		/// <remarks>Reads int values.</remarks>
		public abstract class Reader
		{
			/// <summary>Reads next single int</summary>
			/// <exception cref="System.IO.IOException"></exception>
			public abstract int Next();
		}
	}
}
