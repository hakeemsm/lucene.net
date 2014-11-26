/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.Docvalues;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Expressions
{
	/// <summary>
	/// A utility class to allow expressions to access the score as a
	/// <see cref="Lucene.Net.Queries.Function.FunctionValues">Lucene.Net.Queries.Function.FunctionValues
	/// 	</see>
	/// .
	/// </summary>
	internal class ScoreFunctionValues : DoubleDocValues
	{
		internal readonly Scorer scorer;

		internal ScoreFunctionValues(ValueSource parent, Scorer scorer) : base(parent)
		{
			this.scorer = scorer;
		}

		public override double DoubleVal(int document)
		{
			try
			{
				return document == scorer.DocID().Score();
			}
			catch (IOException exception)
			{
				throw new RuntimeException(exception);
			}
		}
	}
}
