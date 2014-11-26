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
	/// a facet label associated with a float.  Use
	/// <see cref="TaxonomyFacetSumFloatAssociations">TaxonomyFacetSumFloatAssociations</see>
	/// to aggregate float values
	/// per facet label at search time.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class FloatAssociationFacetField : AssociationFacetField
	{
		/// <summary>
		/// Creates this from
		/// <code>dim</code>
		/// and
		/// <code>path</code>
		/// and a
		/// float association
		/// </summary>
		public FloatAssociationFacetField(float assoc, string dim, params string[] path) : 
			base(FloatToBytesRef(assoc), dim, path)
		{
		}

		/// <summary>
		/// Encodes a
		/// <code>float</code>
		/// as a 4-byte
		/// <see cref="Lucene.Net.Util.BytesRef">Lucene.Net.Util.BytesRef</see>
		/// .
		/// </summary>
		public static BytesRef FloatToBytesRef(float v)
		{
			return IntAssociationFacetField.IntToBytesRef(Sharpen.Runtime.FloatToIntBits(v));
		}

		/// <summary>
		/// Decodes a previously encoded
		/// <code>float</code>
		/// .
		/// </summary>
		public static float BytesRefToFloat(BytesRef b)
		{
			return Sharpen.Runtime.IntBitsToFloat(IntAssociationFacetField.BytesRefToInt(b));
		}

		public override string ToString()
		{
			return "FloatAssociationFacetField(dim=" + dim + " path=" + Arrays.ToString(path)
				 + " value=" + BytesRefToFloat(assoc) + ")";
		}
	}
}
