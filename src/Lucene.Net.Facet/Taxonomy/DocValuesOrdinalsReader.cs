/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Facet;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Facet.Taxonomy
{
	/// <summary>Decodes ordinals previously indexed into a BinaryDocValues field</summary>
	public class DocValuesOrdinalsReader : OrdinalsReader
	{
		private readonly string field;

		/// <summary>Default constructor.</summary>
		/// <remarks>Default constructor.</remarks>
		public DocValuesOrdinalsReader() : this(FacetsConfig.DEFAULT_INDEX_FIELD_NAME)
		{
		}

		/// <summary>Create this, with the specified indexed field name.</summary>
		/// <remarks>Create this, with the specified indexed field name.</remarks>
		public DocValuesOrdinalsReader(string field)
		{
			this.field = field;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override OrdinalsReader.OrdinalsSegmentReader GetReader(AtomicReaderContext
			 context)
		{
			BinaryDocValues values0 = ((AtomicReader)context.Reader()).GetBinaryDocValues(field
				);
			if (values0 == null)
			{
				values0 = DocValues.EMPTY_BINARY;
			}
			BinaryDocValues values = values0;
			return new _OrdinalsSegmentReader_54(this, values);
		}

		private sealed class _OrdinalsSegmentReader_54 : OrdinalsReader.OrdinalsSegmentReader
		{
			public _OrdinalsSegmentReader_54(DocValuesOrdinalsReader _enclosing, BinaryDocValues
				 values)
			{
				this._enclosing = _enclosing;
				this.values = values;
				this.bytes = new BytesRef(32);
			}

			private readonly BytesRef bytes;

			/// <exception cref="System.IO.IOException"></exception>
			public override void Get(int docID, IntsRef ordinals)
			{
				values.Get(docID, this.bytes);
				this._enclosing.Decode(this.bytes, ordinals);
			}

			private readonly DocValuesOrdinalsReader _enclosing;

			private readonly BinaryDocValues values;
		}

		public override string GetIndexFieldName()
		{
			return field;
		}

		/// <summary>Subclass & override if you change the encoding.</summary>
		/// <remarks>Subclass & override if you change the encoding.</remarks>
		protected internal virtual void Decode(BytesRef buf, IntsRef ordinals)
		{
			// grow the buffer up front, even if by a large number of values (buf.length)
			// that saves the need to check inside the loop for every decoded value if
			// the buffer needs to grow.
			if (ordinals.ints.Length < buf.length)
			{
				ordinals.ints = ArrayUtil.Grow(ordinals.ints, buf.length);
			}
			ordinals.offset = 0;
			ordinals.length = 0;
			// it is better if the decoding is inlined like so, and not e.g.
			// in a utility method
			int upto = buf.offset + buf.length;
			int value = 0;
			int offset = buf.offset;
			int prev = 0;
			while (offset < upto)
			{
				byte b = buf.bytes[offset++];
				if (b >= 0)
				{
					ordinals.ints[ordinals.length] = ((value << 7) | b) + prev;
					value = 0;
					prev = ordinals.ints[ordinals.length];
					ordinals.length++;
				}
				else
				{
					value = (value << 7) | (b & unchecked((int)(0x7F)));
				}
			}
		}
	}
}
