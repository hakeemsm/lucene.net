/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.Valuesource;
using Sharpen;

namespace Lucene.Net.Queries.Function.Valuesource
{
	/// <summary>
	/// A
	/// <see cref="ValueSource">Lucene.Net.Queries.Function.ValueSource
	/// 	</see>
	/// that abstractly represents
	/// <see cref="ValueSource">Lucene.Net.Queries.Function.ValueSource
	/// 	</see>
	/// s for
	/// poly fields, and other things.
	/// </summary>
	public abstract class MultiValueSource : ValueSource
	{
		public abstract int Dimension();
	}
}
