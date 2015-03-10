using System.Collections;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Search.Grouping;
using Lucene.Net.Util.Mutable;

namespace Lucene.Net.Grouping.Function
{
	/// <summary>
	/// Concrete implementation of
	/// <see cref="AbstractFirstPassGroupingCollector{GROUP_VALUE_TYPE}
	/// 	">Lucene.Net.Search.Grouping.AbstractFirstPassGroupingCollector&lt;GROUP_VALUE_TYPE&gt;
	/// 	</see>
	/// that groups based on
	/// <see cref="Lucene.Net.Queries.Function.ValueSource">Lucene.Net.Queries.Function.ValueSource
	/// 	</see>
	/// instances.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class FunctionFirstPassGroupingCollector : AbstractFirstPassGroupingCollector<IMutableValue>
	{
		private readonly ValueSource groupByVS;

		private readonly IDictionary vsContext;

		private FunctionValues.AbstractValueFiller filler;

		private MutableValue mval;

		/// <summary>Creates a first pass collector.</summary>
		/// <remarks>Creates a first pass collector.</remarks>
		/// <param name="groupByVS">
		/// The
		/// <see cref="Lucene.Net.Queries.Function.ValueSource">Lucene.Net.Queries.Function.ValueSource
		/// 	</see>
		/// instance to group by
		/// </param>
		/// <param name="vsContext">The ValueSource context</param>
		/// <param name="groupSort">
		/// The
		/// <see cref="Lucene.Net.Search.Sort">Lucene.Net.Search.Sort</see>
		/// used to sort the
		/// groups.  The top sorted document within each group
		/// according to groupSort, determines how that group
		/// sorts against other groups.  This must be non-null,
		/// ie, if you want to groupSort by relevance use
		/// Sort.RELEVANCE.
		/// </param>
		/// <param name="topNGroups">How many top groups to keep.</param>
		/// <exception cref="System.IO.IOException">When I/O related errors occur</exception>
		public FunctionFirstPassGroupingCollector(ValueSource groupByVS, IDictionary vsContext, Sort groupSort, int topNGroups) : base(groupSort, topNGroups
			)
		{
			this.groupByVS = groupByVS;
			this.vsContext = vsContext;
		}

		protected internal override IMutableValue GetDocGroupValue(int doc)
		{
			filler.FillValue(doc);
			return mval;
		}

		protected internal override IMutableValue CopyDocGroupValue(IMutableValue groupValue
			, IMutableValue reuse)
		{
			if (reuse != null)
			{
				reuse.Copy(groupValue);
				return reuse;
			}
			return groupValue.Duplicate();
		}

		
		public override void SetNextReader(AtomicReaderContext readerContext)
		{
			base.SetNextReader(readerContext);
			FunctionValues values = groupByVS.GetValues(vsContext, readerContext);
			filler = values.ValueFiller;
			mval = filler.Value;
		}

	    public override Scorer Scorer
	    {
	        set { throw new System.NotImplementedException(); }
	    }

	    public override AtomicReaderContext NextReader
	    {
	        set { throw new System.NotImplementedException(); }
	    }
	}
}
