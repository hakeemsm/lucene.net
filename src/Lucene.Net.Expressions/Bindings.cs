/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Expressions;
using Lucene.Net.Queries.Function;
using Sharpen;

namespace Lucene.Net.Expressions
{
	/// <summary>Binds variable names in expressions to actual data.</summary>
	/// <remarks>
	/// Binds variable names in expressions to actual data.
	/// <p>
	/// These are typically DocValues fields/FieldCache, the document's
	/// relevance score, or other ValueSources.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class Bindings
	{
		/// <summary>Sole constructor.</summary>
		/// <remarks>
		/// Sole constructor. (For invocation by subclass
		/// constructors, typically implicit.)
		/// </remarks>
		public Bindings()
		{
		}

		/// <summary>Returns a ValueSource bound to the variable name.</summary>
		/// <remarks>Returns a ValueSource bound to the variable name.</remarks>
		public abstract ValueSource GetValueSource(string name);

		/// <summary>
		/// Returns a
		/// <code>ValueSource</code>
		/// over relevance scores
		/// </summary>
		protected internal ValueSource GetScoreValueSource()
		{
			return new ScoreValueSource();
		}
	}
}
