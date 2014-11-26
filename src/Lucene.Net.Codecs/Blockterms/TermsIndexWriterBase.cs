/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Blockterms;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Blockterms
{
	/// <summary>
	/// Base class for terms index implementations to plug
	/// into
	/// <see cref="BlockTermsWriter">BlockTermsWriter</see>
	/// .
	/// </summary>
	/// <seealso cref="TermsIndexReaderBase">TermsIndexReaderBase</seealso>
	/// <lucene.experimental></lucene.experimental>
	public abstract class TermsIndexWriterBase : IDisposable
	{
		/// <summary>Terms index API for a single field.</summary>
		/// <remarks>Terms index API for a single field.</remarks>
		public abstract class FieldWriter
		{
			/// <exception cref="System.IO.IOException"></exception>
			public abstract bool CheckIndexTerm(BytesRef text, TermStats stats);

			/// <exception cref="System.IO.IOException"></exception>
			public abstract void Add(BytesRef text, TermStats stats, long termsFilePointer);

			/// <exception cref="System.IO.IOException"></exception>
			public abstract void Finish(long termsFilePointer);

			internal FieldWriter(TermsIndexWriterBase _enclosing)
			{
				this._enclosing = _enclosing;
			}

			private readonly TermsIndexWriterBase _enclosing;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public abstract TermsIndexWriterBase.FieldWriter AddField(FieldInfo fieldInfo, long
			 termsFilePointer);

		public abstract void Close();
	}
}
