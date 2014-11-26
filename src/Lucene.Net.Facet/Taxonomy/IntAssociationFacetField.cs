/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Facet.Taxonomy
{
	/// <summary>
	/// Add an instance of this to your
	/// <see cref="Lucene.Net.Document.Document">Lucene.Net.Document.Document
	/// 	</see>
	/// to add
	/// a facet label associated with an int.  Use
	/// <see cref="TaxonomyFacetSumIntAssociations">TaxonomyFacetSumIntAssociations</see>
	/// to aggregate int values
	/// per facet label at search time.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class IntAssociationFacetField : AssociationFacetField
	{
		/// <summary>
		/// Creates this from
		/// <code>dim</code>
		/// and
		/// <code>path</code>
		/// and an
		/// int association
		/// </summary>
		public IntAssociationFacetField(int assoc, string dim, params string[] path) : base
			(IntToBytesRef(assoc), dim, path)
		{
		}

		/// <summary>
		/// Encodes an
		/// <code>int</code>
		/// as a 4-byte
		/// <see cref="Lucene.Net.Util.BytesRef">Lucene.Net.Util.BytesRef</see>
		/// ,
		/// big-endian.
		/// </summary>
		public static BytesRef IntToBytesRef(int v)
		{
			byte[] bytes = new byte[4];
			// big-endian:
			bytes[0] = unchecked((byte)(v >> 24));
			bytes[1] = unchecked((byte)(v >> 16));
			bytes[2] = unchecked((byte)(v >> 8));
			bytes[3] = unchecked((byte)v);
			return new BytesRef(bytes);
		}

		/// <summary>
		/// Decodes a previously encoded
		/// <code>int</code>
		/// .
		/// </summary>
		public static int BytesRefToInt(BytesRef b)
		{
			return ((b.bytes[b.offset] & unchecked((int)(0xFF))) << 24) | ((b.bytes[b.offset 
				+ 1] & unchecked((int)(0xFF))) << 16) | ((b.bytes[b.offset + 2] & unchecked((int
				)(0xFF))) << 8) | (b.bytes[b.offset + 3] & unchecked((int)(0xFF)));
		}

		public override string ToString()
		{
			return "IntAssociationFacetField(dim=" + dim + " path=" + Arrays.ToString(path) +
				 " value=" + BytesRefToInt(assoc) + ")";
		}
	}
}
