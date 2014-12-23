/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Payloads;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Search.Spans;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search.Payloads
{
	/// <summary>TestExplanations subclass focusing on payload queries</summary>
	public class TestPayloadExplanations : TestExplanations
	{
		private PayloadFunction functions = new PayloadFunction[] { new AveragePayloadFunction
			(), new MinPayloadFunction(), new MaxPayloadFunction() };

		/// <exception cref="System.Exception"></exception>
		public override void SetUp()
		{
			base.SetUp();
			searcher.SetSimilarity(new _DefaultSimilarity_40());
		}

		private sealed class _DefaultSimilarity_40 : DefaultSimilarity
		{
			public _DefaultSimilarity_40()
			{
			}

			public override float ScorePayload(int doc, int start, int end, BytesRef payload)
			{
				return 1 + (payload.GetHashCode() % 10);
			}
		}

		/// <summary>macro for payloadtermquery</summary>
		private SpanQuery Pt(string s, PayloadFunction fn, bool includeSpanScore)
		{
			return new PayloadTermQuery(new Term(FIELD, s), fn, includeSpanScore);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestPT1()
		{
			foreach (PayloadFunction fn in functions)
			{
				Qtest(Pt("w1", fn, false), new int[] { 0, 1, 2, 3 });
				Qtest(Pt("w1", fn, true), new int[] { 0, 1, 2, 3 });
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestPT2()
		{
			foreach (PayloadFunction fn in functions)
			{
				SpanQuery q = Pt("w1", fn, false);
				q.SetBoost(1000);
				Qtest(q, new int[] { 0, 1, 2, 3 });
				q = Pt("w1", fn, true);
				q.SetBoost(1000);
				Qtest(q, new int[] { 0, 1, 2, 3 });
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestPT4()
		{
			foreach (PayloadFunction fn in functions)
			{
				Qtest(Pt("xx", fn, false), new int[] { 2, 3 });
				Qtest(Pt("xx", fn, true), new int[] { 2, 3 });
			}
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestPT5()
		{
			foreach (PayloadFunction fn in functions)
			{
				SpanQuery q = Pt("xx", fn, false);
				q.SetBoost(1000);
				Qtest(q, new int[] { 2, 3 });
				q = Pt("xx", fn, true);
				q.SetBoost(1000);
				Qtest(q, new int[] { 2, 3 });
			}
		}
		// TODO: test the payloadnear query too!
	}
}
