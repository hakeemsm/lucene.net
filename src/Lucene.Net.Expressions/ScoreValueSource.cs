/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections;
using Lucene.Net.Expressions;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Expressions
{
	/// <summary>
	/// A
	/// <see cref="Lucene.Net.Queries.Function.ValueSource">Lucene.Net.Queries.Function.ValueSource
	/// 	</see>
	/// which uses the
	/// <see cref="Lucene.Net.Search.Scorer">Lucene.Net.Search.Scorer</see>
	/// passed through
	/// the context map by
	/// <see cref="ExpressionComparator">ExpressionComparator</see>
	/// .
	/// </summary>
	internal class ScoreValueSource : ValueSource
	{
		/// <summary>
		/// <code>context</code> must contain a key "scorer" which is a
		/// <see cref="Lucene.Net.Search.Scorer">Lucene.Net.Search.Scorer</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			Scorer v = (Scorer)context.Get("scorer");
			if (v == null)
			{
				throw new InvalidOperationException("Expressions referencing the score can only be used for sorting"
					);
			}
			return new ScoreFunctionValues(this, v);
		}

		public override bool Equals(object o)
		{
			return o == this;
		}

		public override int GetHashCode()
		{
			return Runtime.IdentityHashCode(this);
		}

		public override string Description()
		{
			return "score()";
		}
	}
}
