/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Search;
using Lucene.Net.Search.Spans;


namespace Lucene.Net.Search.Spans
{
	/// <summary>TestExplanations subclass focusing on span queries</summary>
	public class TestSpanExplanations : TestExplanations
	{
		/// <exception cref="System.Exception"></exception>
		public virtual void TestST1()
		{
			SpanQuery q = St("w1");
			Qtest(q, new int[] { 0, 1, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestST2()
		{
			SpanQuery q = St("w1");
			q.SetBoost(1000);
			Qtest(q, new int[] { 0, 1, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestST4()
		{
			SpanQuery q = St("xx");
			Qtest(q, new int[] { 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestST5()
		{
			SpanQuery q = St("xx");
			q.SetBoost(1000);
			Qtest(q, new int[] { 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSF1()
		{
			SpanQuery q = Sf(("w1"), 1);
			Qtest(q, new int[] { 0, 1, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSF2()
		{
			SpanQuery q = Sf(("w1"), 1);
			q.SetBoost(1000);
			Qtest(q, new int[] { 0, 1, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSF4()
		{
			SpanQuery q = Sf(("xx"), 2);
			Qtest(q, new int[] { 2 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSF5()
		{
			SpanQuery q = Sf(("yy"), 2);
			Qtest(q, new int[] {  });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSF6()
		{
			SpanQuery q = Sf(("yy"), 4);
			q.SetBoost(1000);
			Qtest(q, new int[] { 2 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSO1()
		{
			SpanQuery q = Sor("w1", "QQ");
			Qtest(q, new int[] { 0, 1, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSO2()
		{
			SpanQuery q = Sor("w1", "w3", "zz");
			Qtest(q, new int[] { 0, 1, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSO3()
		{
			SpanQuery q = Sor("w5", "QQ", "yy");
			Qtest(q, new int[] { 0, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSO4()
		{
			SpanQuery q = Sor("w5", "QQ", "yy");
			Qtest(q, new int[] { 0, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSNear1()
		{
			SpanQuery q = Snear("w1", "QQ", 100, true);
			Qtest(q, new int[] {  });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSNear2()
		{
			SpanQuery q = Snear("w1", "xx", 100, true);
			Qtest(q, new int[] { 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSNear3()
		{
			SpanQuery q = Snear("w1", "xx", 0, true);
			Qtest(q, new int[] { 2 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSNear4()
		{
			SpanQuery q = Snear("w1", "xx", 1, true);
			Qtest(q, new int[] { 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSNear5()
		{
			SpanQuery q = Snear("xx", "w1", 0, false);
			Qtest(q, new int[] { 2 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSNear6()
		{
			SpanQuery q = Snear("w1", "w2", "QQ", 100, true);
			Qtest(q, new int[] {  });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSNear7()
		{
			SpanQuery q = Snear("w1", "xx", "w2", 100, true);
			Qtest(q, new int[] { 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSNear8()
		{
			SpanQuery q = Snear("w1", "xx", "w2", 0, true);
			Qtest(q, new int[] { 2 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSNear9()
		{
			SpanQuery q = Snear("w1", "xx", "w2", 1, true);
			Qtest(q, new int[] { 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSNear10()
		{
			SpanQuery q = Snear("xx", "w1", "w2", 0, false);
			Qtest(q, new int[] { 2 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSNear11()
		{
			SpanQuery q = Snear("w1", "w2", "w3", 1, true);
			Qtest(q, new int[] { 0, 1 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSNot1()
		{
			SpanQuery q = Snot(Sf("w1", 10), St("QQ"));
			Qtest(q, new int[] { 0, 1, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSNot2()
		{
			SpanQuery q = Snot(Sf("w1", 10), St("QQ"));
			q.SetBoost(1000);
			Qtest(q, new int[] { 0, 1, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSNot4()
		{
			SpanQuery q = Snot(Sf("w1", 10), St("xx"));
			Qtest(q, new int[] { 0, 1, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSNot5()
		{
			SpanQuery q = Snot(Sf("w1", 10), St("xx"));
			q.SetBoost(1000);
			Qtest(q, new int[] { 0, 1, 2, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSNot7()
		{
			SpanQuery f = Snear("w1", "w3", 10, true);
			f.SetBoost(1000);
			SpanQuery q = Snot(f, St("xx"));
			Qtest(q, new int[] { 0, 1, 3 });
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSNot10()
		{
			SpanQuery t = St("xx");
			t.SetBoost(10000);
			SpanQuery q = Snot(Snear("w1", "w3", 10, true), t);
			Qtest(q, new int[] { 0, 1, 3 });
		}
	}
}
