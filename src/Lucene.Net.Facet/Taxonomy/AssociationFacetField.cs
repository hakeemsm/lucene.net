/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Document;
using Lucene.Net.Facet;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Facet.Taxonomy
{
	/// <summary>
	/// Add an instance of this to your
	/// <see cref="Lucene.Net.Document.Document">Lucene.Net.Document.Document
	/// 	</see>
	/// to add
	/// a facet label associated with an arbitrary byte[].
	/// This will require a custom
	/// <see cref="Lucene.Net.Facet.Facets">Lucene.Net.Facet.Facets</see>
	/// implementation at search time; see
	/// <see cref="IntAssociationFacetField">IntAssociationFacetField</see>
	/// and
	/// <see cref="FloatAssociationFacetField">FloatAssociationFacetField</see>
	/// to use existing
	/// <see cref="Lucene.Net.Facet.Facets">Lucene.Net.Facet.Facets</see>
	/// implementations.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class AssociationFacetField : Field
	{
		/// <summary>
		/// Indexed
		/// <see cref="Lucene.Net.Document.FieldType">Lucene.Net.Document.FieldType
		/// 	</see>
		/// .
		/// </summary>
		public static readonly FieldType TYPE = new FieldType();

		static AssociationFacetField()
		{
			// javadocs
			TYPE.SetIndexed(true);
			TYPE.Freeze();
		}

		/// <summary>Dimension for this field.</summary>
		/// <remarks>Dimension for this field.</remarks>
		public readonly string dim;

		/// <summary>Facet path for this field.</summary>
		/// <remarks>Facet path for this field.</remarks>
		public readonly string[] path;

		/// <summary>Associated value.</summary>
		/// <remarks>Associated value.</remarks>
		public readonly BytesRef assoc;

		/// <summary>
		/// Creates this from
		/// <code>dim</code>
		/// and
		/// <code>path</code>
		/// and an
		/// association
		/// </summary>
		public AssociationFacetField(BytesRef assoc, string dim, params string[] path) : 
			base("dummy", TYPE)
		{
			FacetField.VerifyLabel(dim);
			foreach (string label in path)
			{
				FacetField.VerifyLabel(label);
			}
			this.dim = dim;
			this.assoc = assoc;
			if (path.Length == 0)
			{
				throw new ArgumentException("path must have at least one element");
			}
			this.path = path;
		}

		public override string ToString()
		{
			return "AssociationFacetField(dim=" + dim + " path=" + Arrays.ToString(path) + " bytes="
				 + assoc + ")";
		}
	}
}
