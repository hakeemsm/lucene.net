using System.Collections.Generic;
using Lucene.Net.Search;

namespace Lucene.Net.Grouping
{
	/// <summary>
	/// A collector that collects all groups that match the
	/// query.
	/// </summary>
	/// <remarks>
	/// A collector that collects all groups that match the
	/// query. Only the group value is collected, and the order
	/// is undefined.  This collector does not determine
	/// the most relevant document of a group.
	/// <p/>
	/// This is an abstract version. Concrete implementations define
	/// what a group actually is and how it is internally collected.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class AbstractAllGroupsCollector<GROUP_VALUE_TYPE> : Collector
	{
		/// <summary>Returns the total number of groups for the executed search.</summary>
		/// <remarks>
		/// Returns the total number of groups for the executed search.
		/// This is a convenience method. The following code snippet has the same effect: <pre>getGroups().size()</pre>
		/// </remarks>
		/// <returns>The total number of groups for the executed search</returns>
		public virtual int GroupCount
		{
		    get { return Groups.Count; }
		}

		/// <summary>
		/// Returns the group values
		/// <p/>
		/// This is an unordered collections of group values.
		/// </summary>
		/// <remarks>
		/// Returns the group values
		/// <p/>
		/// This is an unordered collections of group values. For each group that matched the query there is a
		/// <see cref="Lucene.Net.Util.BytesRef">Lucene.Net.Util.BytesRef</see>
		/// representing a group value.
		/// </remarks>
		/// <returns>the group values</returns>
		public abstract ICollection<GROUP_VALUE_TYPE> Groups { get; }

		// Empty not necessary
		/// <exception cref="System.IO.IOException"></exception>
        public override Scorer Scorer { set{} }

		public override bool AcceptsDocsOutOfOrder()
		{
			return true;
		}
	}
}
