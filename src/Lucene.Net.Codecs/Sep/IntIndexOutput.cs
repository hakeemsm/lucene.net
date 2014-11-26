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
	/// IntBlockReader.
	/// <p>NOTE: block sizes could be variable
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class IntIndexOutput : IDisposable
	{
		// TODO: we may want tighter integration w/ IndexOutput --
		// may give better perf:
		/// <summary>Write an int to the primary file.</summary>
		/// <remarks>
		/// Write an int to the primary file.  The value must be
		/// &gt;= 0.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract void Write(int v);

		/// <summary>Records a single skip-point in the IndexOutput.</summary>
		/// <remarks>Records a single skip-point in the IndexOutput.</remarks>
		public abstract class Index
		{
			/// <summary>Internally records the current location</summary>
			/// <exception cref="System.IO.IOException"></exception>
			public abstract void Mark();

			/// <summary>Copies index from other</summary>
			/// <exception cref="System.IO.IOException"></exception>
			public abstract void CopyFrom(IntIndexOutput.Index other, bool copyLast);

			/// <summary>
			/// Writes "location" of current output pointer of primary
			/// output to different output (out)
			/// </summary>
			/// <exception cref="System.IO.IOException"></exception>
			public abstract void Write(DataOutput indexOut, bool absolute);
		}

		/// <summary>
		/// If you are indexing the primary output file, call
		/// this and interact with the returned IndexWriter.
		/// </summary>
		/// <remarks>
		/// If you are indexing the primary output file, call
		/// this and interact with the returned IndexWriter.
		/// </remarks>
		public abstract IntIndexOutput.Index Index();

		/// <exception cref="System.IO.IOException"></exception>
		public abstract void Close();
	}
}
