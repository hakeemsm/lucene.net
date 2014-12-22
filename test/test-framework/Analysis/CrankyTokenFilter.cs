using System;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Randomized.Generators;

namespace Lucene.Net.TestFramework.Analysis
{
	/// <summary>Throws IOException from random Tokenstream methods.</summary>
	/// <remarks>
	/// Throws IOException from random Tokenstream methods.
	/// <p>
	/// This can be used to simulate a buggy analyzer in IndexWriter,
	/// where we must delete the document but not abort everything in the buffer.
	/// </remarks>
	public sealed class CrankyTokenFilter : TokenFilter
	{
		internal readonly Random random;

		internal int thingToDo;

		/// <summary>Creates a new CrankyTokenFilter</summary>
		public CrankyTokenFilter(TokenStream input, Random random) : base(input)
		{
			this.random = random;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool IncrementToken()
		{
			if (thingToDo == 0 && random.NextBoolean())
			{
				throw new IOException("Fake IOException from TokenStream.incrementToken()");
			}
			return input.IncrementToken();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void End()
		{
			base.End();
			if (thingToDo == 1 && random.NextBoolean())
			{
				throw new IOException("Fake IOException from TokenStream.end()");
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Reset()
		{
			base.Reset();
			thingToDo = random.Next(100);
			if (thingToDo == 2 && random.NextBoolean())
			{
				throw new IOException("Fake IOException from TokenStream.reset()");
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Dispose()
		{
			base.Dispose();
			if (thingToDo == 3 && random.NextBoolean())
			{
				throw new IOException("Fake IOException from TokenStream.close()");
			}
		}
	}
}
