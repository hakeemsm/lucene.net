/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util.Automaton;
using Sharpen;

namespace Lucene.Net.Search
{
	/// <summary>Tests the FieldcacheRewriteMethod with random regular expressions</summary>
	public class TestFieldCacheRewriteMethod : TestRegexpRandom2
	{
		/// <summary>Test fieldcache rewrite against filter rewrite</summary>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal override void AssertSame(string regexp)
		{
			RegexpQuery fieldCache = new RegexpQuery(new Term(fieldName, regexp), RegExp.NONE
				);
			fieldCache.SetRewriteMethod(new FieldCacheRewriteMethod());
			RegexpQuery filter = new RegexpQuery(new Term(fieldName, regexp), RegExp.NONE);
			filter.SetRewriteMethod(MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE);
			TopDocs fieldCacheDocs = searcher1.Search(fieldCache, 25);
			TopDocs filterDocs = searcher2.Search(filter, 25);
			CheckHits.CheckEqual(fieldCache, fieldCacheDocs.scoreDocs, filterDocs.scoreDocs);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestEquals()
		{
			RegexpQuery a1 = new RegexpQuery(new Term(fieldName, "[aA]"), RegExp.NONE);
			RegexpQuery a2 = new RegexpQuery(new Term(fieldName, "[aA]"), RegExp.NONE);
			RegexpQuery b = new RegexpQuery(new Term(fieldName, "[bB]"), RegExp.NONE);
			AreEqual(a1, a2);
			IsFalse(a1.Equals(b));
			a1.SetRewriteMethod(new FieldCacheRewriteMethod());
			a2.SetRewriteMethod(new FieldCacheRewriteMethod());
			b.SetRewriteMethod(new FieldCacheRewriteMethod());
			AreEqual(a1, a2);
			IsFalse(a1.Equals(b));
			QueryUtils.Check(a1);
		}
	}
}
