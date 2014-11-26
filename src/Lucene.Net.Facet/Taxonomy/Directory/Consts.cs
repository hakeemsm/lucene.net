/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Facet.Taxonomy.Directory
{
	/// <lucene.experimental></lucene.experimental>
	internal abstract class Consts
	{
		internal static readonly string FULL = "$full_path$";

		internal static readonly string FIELD_PAYLOADS = "$payloads$";

		internal static readonly string PAYLOAD_PARENT = "p";

		internal static readonly BytesRef PAYLOAD_PARENT_BYTES_REF = new BytesRef(PAYLOAD_PARENT
			);
	}
}
