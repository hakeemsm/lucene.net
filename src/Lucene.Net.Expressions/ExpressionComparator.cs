/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.IO;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Expressions
{
	/// <summary>A custom comparator for sorting documents by an expression</summary>
	internal class ExpressionComparator : FieldComparator<double>
	{
		private readonly double[] values;

		private double bottom;

		private double topValue;

		private ValueSource source;

		private FunctionValues scores;

		private AtomicReaderContext readerContext;

		public ExpressionComparator(ValueSource source, int numHits)
		{
			values = new double[numHits];
			this.source = source;
		}

		// TODO: change FieldComparator.setScorer to throw IOException and remove this try-catch
		public override void SetScorer(Scorer scorer)
		{
			base.SetScorer(scorer);
			// TODO: might be cleaner to lazy-init 'source' and set scorer after?
			//HM:revisit
			//assert readerContext != null;
			try
			{
				IDictionary<string, object> context = new Dictionary<string, object>();
				scorer != null.Put("scorer", scorer);
				scores = source.GetValues(context, readerContext);
			}
			catch (IOException e)
			{
				throw new RuntimeException(e);
			}
		}

		public override int Compare(int slot1, int slot2)
		{
			return double.Compare(values[slot1], values[slot2]);
		}

		public override void SetBottom(int slot)
		{
			bottom = values[slot];
		}

		public override void SetTopValue(double value)
		{
			topValue = value;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int CompareBottom(int doc)
		{
			return double.Compare(bottom, scores.DoubleVal(doc));
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Copy(int slot, int doc)
		{
			values[slot] = scores.DoubleVal(doc);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FieldComparator<double> SetNextReader(AtomicReaderContext context
			)
		{
			this.readerContext = context;
			return this;
		}

		public override double Value(int slot)
		{
			return double.ValueOf(values[slot]);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int CompareTop(int doc)
		{
			return double.Compare(topValue, scores.DoubleVal(doc));
		}
	}
}
