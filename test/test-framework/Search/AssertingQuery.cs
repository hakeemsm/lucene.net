/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Search;
using Sharpen;

namespace Lucene.Net.TestFramework.Search
{
	/// <summary>Assertion-enabled query.</summary>
	/// <remarks>Assertion-enabled query.</remarks>
	public class AssertingQuery : Query
	{
		private readonly Random random;

		private readonly Query @in;

		/// <summary>Sole constructor.</summary>
		/// <remarks>Sole constructor.</remarks>
		public AssertingQuery(Random random, Query @in)
		{
			this.random = random;
			this.@in = @in;
		}

		/// <summary>Wrap a query if necessary.</summary>
		/// <remarks>Wrap a query if necessary.</remarks>
		public static Query Wrap(Random random, Query query)
		{
			return query isLucene.Net.TestFramework.Search.AssertingQuery ? query : newLucene.Net.TestFramework.Search.AssertingQuery
				(random, query);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Weight CreateWeight(IndexSearcher searcher)
		{
			return AssertingWeight.Wrap(new Random(random.NextLong()), @in.CreateWeight(searcher
				));
		}

		public override void ExtractTerms(ICollection<Term> terms)
		{
			@in.ExtractTerms(terms);
		}

		public override string ToString(string field)
		{
			return @in.ToString(field);
		}

		public override bool Equals(object obj)
		{
			if (obj == null || !(obj isLucene.Net.TestFramework.Search.AssertingQuery))
			{
				return false;
			}
			Lucene.NetSearch.AssertingQuery that = (Lucene.NetSearch.AssertingQuery
				)obj;
			return this.@in.Equals(that.@in);
		}

		public override int GetHashCode()
		{
			return -@in.GetHashCode();
		}

		public override Query Clone()
		{
			return Wrap(new Random(random.NextLong()), @in.Clone());
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Query Rewrite(IndexReader reader)
		{
			Query rewritten = @in.Rewrite(reader);
			if (rewritten == @in)
			{
				return this;
			}
			else
			{
				return Wrap(new Random(random.NextLong()), rewritten);
			}
		}

		public override float GetBoost()
		{
			return @in.GetBoost();
		}

		public override void SetBoost(float b)
		{
			@in.SetBoost(b);
		}
	}
}
