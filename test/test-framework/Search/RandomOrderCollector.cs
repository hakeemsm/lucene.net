/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.TestFramework.Index;
using Lucene.Net.TestFramework.Search;
using Sharpen;

namespace Lucene.Net.TestFramework.Search
{
	/// <summary>Randomize collection order.</summary>
	/// <remarks>
	/// Randomize collection order. Don't forget to call
	/// <see cref="Flush()">Flush()</see>
	/// when
	/// collection is finished to collect buffered documents.
	/// </remarks>
	internal sealed class RandomOrderCollector : Collector
	{
		internal readonly Random random;

		internal readonly Collector @in;

		internal Scorer scorer;

		internal FakeScorer fakeScorer;

		internal int buffered;

		internal readonly int bufferSize;

		internal readonly int[] docIDs;

		internal readonly float[] scores;

		internal readonly int[] freqs;

		internal RandomOrderCollector(Random random, Collector @in)
		{
			if (!@in.AcceptsDocsOutOfOrder())
			{
				throw new ArgumentException();
			}
			this.@in = @in;
			this.random = random;
			bufferSize = 1 + random.Next(100);
			docIDs = new int[bufferSize];
			scores = new float[bufferSize];
			freqs = new int[bufferSize];
			buffered = 0;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetScorer(Scorer scorer)
		{
			this.scorer = scorer;
			fakeScorer = new FakeScorer();
			@in.SetScorer(fakeScorer);
		}

		private void Shuffle()
		{
			for (int i = buffered - 1; i > 0; --i)
			{
				int other = random.Next(i + 1);
				int tmpDoc = docIDs[i];
				docIDs[i] = docIDs[other];
				docIDs[other] = tmpDoc;
				float tmpScore = scores[i];
				scores[i] = scores[other];
				scores[other] = tmpScore;
				int tmpFreq = freqs[i];
				freqs[i] = freqs[other];
				freqs[other] = tmpFreq;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public void Flush()
		{
			Shuffle();
			for (int i = 0; i < buffered; ++i)
			{
				fakeScorer.doc = docIDs[i];
				fakeScorer.freq = freqs[i];
				fakeScorer.score = scores[i];
				@in.Collect(fakeScorer.doc);
			}
			buffered = 0;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Collect(int doc)
		{
			docIDs[buffered] = doc;
			scores[buffered] = scorer.Score();
			try
			{
				freqs[buffered] = scorer.Freq();
			}
			catch (NotSupportedException)
			{
				freqs[buffered] = -1;
			}
			if (++buffered == bufferSize)
			{
				Flush();
			}
		}

		public override bool AcceptsDocsOutOfOrder()
		{
			return @in.AcceptsDocsOutOfOrder();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetNextReader(AtomicReaderContext context)
		{
			throw new NotSupportedException();
		}
	}
}
