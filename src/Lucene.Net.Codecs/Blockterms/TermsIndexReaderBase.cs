/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Codecs.Blockterms;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Blockterms
{
	/// <summary>
	/// <see cref="BlockTermsReader">BlockTermsReader</see>
	/// interacts with an instance of this class
	/// to manage its terms index.  The writer must accept
	/// indexed terms (many pairs of BytesRef text + long
	/// fileOffset), and then this reader must be able to
	/// retrieve the nearest index term to a provided term
	/// text.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public abstract class TermsIndexReaderBase : IDisposable
	{
		// TODO
		//   - allow for non-regular index intervals?  eg with a
		//     long string of rare terms, you don't need such
		//     frequent indexing
		public abstract TermsIndexReaderBase.FieldIndexEnum GetFieldEnum(FieldInfo fieldInfo
			);

		/// <exception cref="System.IO.IOException"></exception>
		public abstract void Close();

		public abstract bool SupportsOrd();

		public abstract int GetDivisor();

		/// <summary>
		/// Similar to TermsEnum, except, the only "metadata" it
		/// reports for a given indexed term is the long fileOffset
		/// into the main terms dictionary file.
		/// </summary>
		/// <remarks>
		/// Similar to TermsEnum, except, the only "metadata" it
		/// reports for a given indexed term is the long fileOffset
		/// into the main terms dictionary file.
		/// </remarks>
		public abstract class FieldIndexEnum
		{
			/// <summary>
			/// Seeks to "largest" indexed term that's &lt;=
			/// term; returns file pointer index (into the main
			/// terms index file) for that term
			/// </summary>
			/// <exception cref="System.IO.IOException"></exception>
			public abstract long Seek(BytesRef term);

			/// <summary>Returns -1 at end</summary>
			/// <exception cref="System.IO.IOException"></exception>
			public abstract long Next();

			public abstract BytesRef Term();

			/// <summary>
			/// Only implemented if
			/// <see cref="TermsIndexReaderBase.SupportsOrd()">TermsIndexReaderBase.SupportsOrd()
			/// 	</see>
			/// returns true.
			/// </summary>
			/// <exception cref="System.IO.IOException"></exception>
			public abstract long Seek(long ord);

			/// <summary>
			/// Only implemented if
			/// <see cref="TermsIndexReaderBase.SupportsOrd()">TermsIndexReaderBase.SupportsOrd()
			/// 	</see>
			/// returns true.
			/// </summary>
			public abstract long Ord();
		}

		/// <summary>Returns approximate RAM bytes used</summary>
		public abstract long RamBytesUsed();
	}
}
