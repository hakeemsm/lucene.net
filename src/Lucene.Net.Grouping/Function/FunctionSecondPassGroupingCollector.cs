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
	/// <see cref="AbstractSecondPassGroupingCollector{GROUP_VALUE_TYPE}
	/// 	">Lucene.Net.Search.Grouping.AbstractSecondPassGroupingCollector&lt;GROUP_VALUE_TYPE&gt;
	/// 	</see>
	/// that groups based on
	/// <see cref="Lucene.Net.Queries.Function.ValueSource">Lucene.Net.Queries.Function.ValueSource
	/// 	</see>
	/// instances.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class FunctionSecondPassGroupingCollector : AbstractSecondPassGroupingCollector<MutableValue>
	{
		private readonly ValueSource groupByVS;

		private readonly IDictionary vsContext;

		private FunctionValues.AbstractValueFiller filler;

		private MutableValue mval;

		/// <summary>
		/// Constructs a
		/// <see cref="FunctionSecondPassGroupingCollector">FunctionSecondPassGroupingCollector
		/// 	</see>
		/// instance.
		/// </summary>
		/// <param name="searchGroups">
		/// The
		/// <see cref="SearchGroup{GROUP_VALUE_TYPE}">Lucene.Net.Search.Grouping.SearchGroup&lt;GROUP_VALUE_TYPE&gt;
		/// 	</see>
		/// instances collected during the first phase.
		/// </param>
		/// <param name="groupSort">The group sort</param>
		/// <param name="withinGroupSort">The sort inside a group</param>
		/// <param name="maxDocsPerGroup">The maximum number of documents to collect inside a group
		/// 	</param>
		/// <param name="getScores">Whether to include the scores</param>
		/// <param name="getMaxScores">Whether to include the maximum score</param>
		/// <param name="fillSortFields">
		/// Whether to fill the sort values in
		/// <see cref="TopGroups{GROUP_VALUE_TYPE}.withinGroupSort
		/// 	">Lucene.Net.Search.Grouping.TopGroups&lt;GROUP_VALUE_TYPE&gt;.withinGroupSort
		/// 	</see>
		/// </param>
		/// <param name="groupByVS">
		/// The
		/// <see cref="Lucene.Net.Queries.Function.ValueSource">Lucene.Net.Queries.Function.ValueSource
		/// 	</see>
		/// to group by
		/// </param>
		/// <param name="vsContext">The value source context</param>
		/// <exception cref="System.IO.IOException">IOException When I/O related errors occur
		/// 	</exception>
		public FunctionSecondPassGroupingCollector(ICollection<SearchGroup<MutableValue>>
			 searchGroups, Sort groupSort, Sort withinGroupSort, int maxDocsPerGroup, bool getScores
			, bool getMaxScores, bool fillSortFields, ValueSource groupByVS, IDictionary vsContext) : base(searchGroups, groupSort, withinGroupSort, maxDocsPerGroup
			, getScores, getMaxScores, fillSortFields)
		{
			//javadoc
			this.groupByVS = groupByVS;
			this.vsContext = vsContext;
		}

		
		protected internal override SearchGroupDocs<MutableValue> RetrieveGroup(int doc)
		{
			filler.FillValue(doc);
			return groupMap[mval];
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
