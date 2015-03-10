/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Search.Grouping;
using Org.Apache.Lucene.Util.Mutable;
using Sharpen;

namespace Org.Apache.Lucene.Search.Grouping.Function
{
	/// <summary>
	/// Concrete implementation of
	/// <see cref="Org.Apache.Lucene.Search.Grouping.AbstractSecondPassGroupingCollector{GROUP_VALUE_TYPE}
	/// 	">Org.Apache.Lucene.Search.Grouping.AbstractSecondPassGroupingCollector&lt;GROUP_VALUE_TYPE&gt;
	/// 	</see>
	/// that groups based on
	/// <see cref="Org.Apache.Lucene.Queries.Function.ValueSource">Org.Apache.Lucene.Queries.Function.ValueSource
	/// 	</see>
	/// instances.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class FunctionSecondPassGroupingCollector : AbstractSecondPassGroupingCollector
		<MutableValue>
	{
		private readonly ValueSource groupByVS;

		private readonly IDictionary<object, object> vsContext;

		private FunctionValues.ValueFiller filler;

		private MutableValue mval;

		/// <summary>
		/// Constructs a
		/// <see cref="FunctionSecondPassGroupingCollector">FunctionSecondPassGroupingCollector
		/// 	</see>
		/// instance.
		/// </summary>
		/// <param name="searchGroups">
		/// The
		/// <see cref="Org.Apache.Lucene.Search.Grouping.SearchGroup{GROUP_VALUE_TYPE}">Org.Apache.Lucene.Search.Grouping.SearchGroup&lt;GROUP_VALUE_TYPE&gt;
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
		/// <see cref="Org.Apache.Lucene.Search.Grouping.TopGroups{GROUP_VALUE_TYPE}.withinGroupSort
		/// 	">Org.Apache.Lucene.Search.Grouping.TopGroups&lt;GROUP_VALUE_TYPE&gt;.withinGroupSort
		/// 	</see>
		/// </param>
		/// <param name="groupByVS">
		/// The
		/// <see cref="Org.Apache.Lucene.Queries.Function.ValueSource">Org.Apache.Lucene.Queries.Function.ValueSource
		/// 	</see>
		/// to group by
		/// </param>
		/// <param name="vsContext">The value source context</param>
		/// <exception cref="System.IO.IOException">IOException When I/O related errors occur
		/// 	</exception>
		public FunctionSecondPassGroupingCollector(ICollection<SearchGroup<MutableValue>>
			 searchGroups, Sort groupSort, Sort withinGroupSort, int maxDocsPerGroup, bool getScores
			, bool getMaxScores, bool fillSortFields, ValueSource groupByVS, IDictionary<object
			, object> vsContext) : base(searchGroups, groupSort, withinGroupSort, maxDocsPerGroup
			, getScores, getMaxScores, fillSortFields)
		{
			//javadoc
			this.groupByVS = groupByVS;
			this.vsContext = vsContext;
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override AbstractSecondPassGroupingCollector.SearchGroupDocs<MutableValue
			> RetrieveGroup(int doc)
		{
			filler.FillValue(doc);
			return groupMap.Get(mval);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetNextReader(AtomicReaderContext readerContext)
		{
			base.SetNextReader(readerContext);
			FunctionValues values = groupByVS.GetValues(vsContext, readerContext);
			filler = values.GetValueFiller();
			mval = filler.GetValue();
		}
	}
}
