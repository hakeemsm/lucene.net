/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Analysis;
using Sharpen;

namespace Lucene.Net.Analysis
{
	/// <summary>Trivial position class.</summary>
	/// <remarks>Trivial position class.</remarks>
	public class TestPosition : LookaheadTokenFilter.Position
	{
		private string fact;

		public virtual string GetFact()
		{
			return fact;
		}

		public virtual void SetFact(string fact)
		{
			this.fact = fact;
		}
	}
}
