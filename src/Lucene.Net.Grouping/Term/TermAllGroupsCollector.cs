using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Grouping;
using Lucene.Net.Util;

namespace Lucene.Net.Grouping.Term
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
	/// Implementation detail: an int hash set (SentinelIntSet)
	/// is used to detect if a group is already added to the
	/// total count.  For each segment the int set is cleared and filled
	/// with previous counted groups that occur in the new
	/// segment.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class TermAllGroupsCollector : AbstractAllGroupsCollector<BytesRef>
	{
		private const int DEFAULT_INITIAL_SIZE = 128;

		private readonly string groupField;

		private readonly SentinelIntSet ordSet;

		private readonly IList<BytesRef> groups;

		private SortedDocValues index;

		/// <summary>
		/// Expert: Constructs a
		/// <see cref="AbstractAllGroupsCollector{GROUP_VALUE_TYPE}
		/// 	">Lucene.Net.Search.Grouping.AbstractAllGroupsCollector&lt;GROUP_VALUE_TYPE&gt;
		/// 	</see>
		/// </summary>
		/// <param name="groupField">The field to group by</param>
		/// <param name="initialSize">
		/// The initial allocation size of the
		/// internal int set and group list
		/// which should roughly match the total
		/// number of expected unique groups. Be aware that the
		/// heap usage is 4 bytes * initialSize.
		/// </param>
		public TermAllGroupsCollector(string groupField, int initialSize)
		{
			ordSet = new SentinelIntSet(initialSize, -2);
			groups = new List<BytesRef>(initialSize);
			this.groupField = groupField;
		}

		/// <summary>
		/// Constructs a
		/// <see cref="AbstractAllGroupsCollector{GROUP_VALUE_TYPE}
		/// 	">Lucene.Net.Search.Grouping.AbstractAllGroupsCollector&lt;GROUP_VALUE_TYPE&gt;
		/// 	</see>
		/// . This sets the
		/// initial allocation size for the internal int set and group
		/// list to 128.
		/// </summary>
		/// <param name="groupField">The field to group by</param>
		public TermAllGroupsCollector(string groupField) : this(groupField, DEFAULT_INITIAL_SIZE
			)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Collect(int doc)
		{
			int key = index.GetOrd(doc);
			if (!ordSet.Exists(key))
			{
				ordSet.Put(key);
				BytesRef term;
				if (key == -1)
				{
					term = null;
				}
				else
				{
					term = new BytesRef();
					index.LookupOrd(key, term);
				}
				groups.Add(term);
			}
		}

		public override ICollection<BytesRef> Groups
		{
		    get { return groups; }
		}

		/// <exception cref="System.IO.IOException"></exception>
        public override AtomicReaderContext NextReader
		{
		    set
		    {
		        index = FieldCache.DEFAULT.GetTermsIndex(((AtomicReader) value.Reader), groupField
		            );
		        // Clear ordSet and fill it with previous encountered groups that can occur in the current segment.
		        ordSet.Clear();
		        foreach (BytesRef countedGroup in groups)
		        {
		            if (countedGroup == null)
		            {
		                ordSet.Put(-1);
		            }
		            else
		            {
		                int ord = index.LookupTerm(countedGroup);
		                if (ord >= 0)
		                {
		                    ordSet.Put(ord);
		                }
		            }
		        }
		    }
		}
	}
}
