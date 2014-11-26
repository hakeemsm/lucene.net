/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Document;
using Sharpen;

namespace Lucene.Net.Facet
{
	/// <summary>
	/// Add an instance of this to your
	/// <see cref="Lucene.Net.Document.Document">Lucene.Net.Document.Document
	/// 	</see>
	/// for every facet label.
	/// <p>
	/// <b>NOTE:</b> you must call
	/// <see cref="FacetsConfig.Build(Lucene.Net.Document.Document)">FacetsConfig.Build(Lucene.Net.Document.Document)
	/// 	</see>
	/// before
	/// you add the document to IndexWriter.
	/// </summary>
	public class FacetField : Field
	{
		internal static readonly FieldType TYPE = new FieldType();

		static FacetField()
		{
			TYPE.SetIndexed(true);
			TYPE.Freeze();
		}

		/// <summary>Dimension for this field.</summary>
		/// <remarks>Dimension for this field.</remarks>
		public readonly string dim;

		/// <summary>Path for this field.</summary>
		/// <remarks>Path for this field.</remarks>
		public readonly string[] path;

		/// <summary>
		/// Creates the this from
		/// <code>dim</code>
		/// and
		/// <code>path</code>
		/// .
		/// </summary>
		public FacetField(string dim, params string[] path) : base("dummy", TYPE)
		{
			VerifyLabel(dim);
			foreach (string label in path)
			{
				VerifyLabel(label);
			}
			this.dim = dim;
			if (path.Length == 0)
			{
				throw new ArgumentException("path must have at least one element");
			}
			this.path = path;
		}

		public override string ToString()
		{
			return "FacetField(dim=" + dim + " path=" + Arrays.ToString(path) + ")";
		}

		/// <summary>Verifies the label is not null or empty string.</summary>
		/// <remarks>Verifies the label is not null or empty string.</remarks>
		/// <lucene.internal></lucene.internal>
		public static void VerifyLabel(string label)
		{
			if (label == null || label.IsEmpty())
			{
				throw new ArgumentException("empty or null components not allowed; got: " + label
					);
			}
		}
	}
}
