using System;
using System.Collections.Generic;
using Lucene.Net.Search;
using Lucene.Net.Util.Mutable;

namespace Lucene.Net.Grouping
{
    /// <summary>A second pass grouping collector that keeps track of distinct values for a specified field for the top N group.
    /// 	</summary>
    /// <remarks>A second pass grouping collector that keeps track of distinct values for a specified field for the top N group.
    /// 	</remarks>
    /// <lucene.experimental></lucene.experimental>
    public abstract class AbstractDistinctValuesCollector<GC> : Collector where GC : GroupCount<IComparable>
    {
        /// <summary>Returns all unique values for each top N group.</summary>
        /// <remarks>Returns all unique values for each top N group.</remarks>
        /// <returns>all unique values for each top N group</returns>
        public abstract IList<GC> GetGroups();

        public override bool AcceptsDocsOutOfOrder()
        {
            return true;
        }

        public override Scorer Scorer
        {
            set {  }
        }

       
    }

    /// <summary>
		/// Returned by
		/// <see cref="AbstractDistinctValuesCollector{GC}.GetGroups()">AbstractDistinctValuesCollector&lt;GC&gt;.GetGroups()
		/// 	</see>
		/// ,
		/// representing the value and set of distinct values for the group.
		/// </summary>
		public abstract class GroupCount<GROUP_VALUE_TYPE> where GROUP_VALUE_TYPE:IComparable
		{
			public readonly GROUP_VALUE_TYPE groupValue;

			public readonly ICollection<GROUP_VALUE_TYPE> uniqueValues;

			public GroupCount(GROUP_VALUE_TYPE groupValue)
			{
				this.groupValue = groupValue;
				this.uniqueValues = new HashSet<GROUP_VALUE_TYPE>();
			}
		}
	
}
