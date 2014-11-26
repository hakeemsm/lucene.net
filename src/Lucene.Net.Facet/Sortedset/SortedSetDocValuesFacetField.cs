/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Document;
using Lucene.Net.Facet;
using Sharpen;

namespace Lucene.Net.Facet.Sortedset
{
	/// <summary>
	/// Add an instance of this to your Document for every facet
	/// label to be indexed via SortedSetDocValues.
	/// </summary>
	/// <remarks>
	/// Add an instance of this to your Document for every facet
	/// label to be indexed via SortedSetDocValues.
	/// </remarks>
	public class SortedSetDocValuesFacetField : Field
	{
		/// <summary>
		/// Indexed
		/// <see cref="Lucene.Net.Document.FieldType">Lucene.Net.Document.FieldType
		/// 	</see>
		/// .
		/// </summary>
		public static readonly FieldType TYPE = new FieldType();

		static SortedSetDocValuesFacetField()
		{
			TYPE.SetIndexed(true);
			TYPE.Freeze();
		}

		/// <summary>Dimension.</summary>
		/// <remarks>Dimension.</remarks>
		public readonly string dim;

		/// <summary>Label.</summary>
		/// <remarks>Label.</remarks>
		public readonly string label;

		/// <summary>Sole constructor.</summary>
		/// <remarks>Sole constructor.</remarks>
		public SortedSetDocValuesFacetField(string dim, string label) : base("dummy", TYPE
			)
		{
			FacetField.VerifyLabel(label);
			FacetField.VerifyLabel(dim);
			this.dim = dim;
			this.label = label;
		}

		public override string ToString()
		{
			return "SortedSetDocValuesFacetField(dim=" + dim + " label=" + label + ")";
		}
	}
}
