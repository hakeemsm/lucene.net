using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Lucene.Net.Grouping.Term
{
	/// <summary>
	/// Concrete implementation of
	/// <see cref="AbstractFirstPassGroupingCollector{GROUP_VALUE_TYPE}
	/// 	">Lucene.Net.Search.Grouping.AbstractFirstPassGroupingCollector&lt;GROUP_VALUE_TYPE&gt;
	/// 	</see>
	/// that groups based on
	/// field values and more specifically uses
	/// <see cref="Lucene.Net.Index.SortedDocValues">Lucene.Net.Index.SortedDocValues
	/// 	</see>
	/// to collect groups.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class TermFirstPassGroupingCollector : AbstractFirstPassGroupingCollector<BytesRef>
	{
		private readonly BytesRef scratchBytesRef = new BytesRef();

		private SortedDocValues index;

		private string groupField;

		/// <summary>Create the first pass collector.</summary>
		/// <remarks>Create the first pass collector.</remarks>
		/// <param name="groupField">
		/// The field used to group
		/// documents. This field must be single-valued and
		/// indexed (FieldCache is used to access its value
		/// per-document).
		/// </param>
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
		public TermFirstPassGroupingCollector(string groupField, Sort groupSort, int topNGroups
			) : base(groupSort, topNGroups)
		{
			this.groupField = groupField;
		}

		protected internal override BytesRef GetDocGroupValue(int doc)
		{
			int ord = index.GetOrd(doc);
			if (ord == -1)
			{
				return null;
			}
			else
			{
				index.LookupOrd(ord, scratchBytesRef);
				return scratchBytesRef;
			}
		}

		protected internal override BytesRef CopyDocGroupValue(BytesRef groupValue, BytesRef
			 reuse)
		{
			if (groupValue == null)
			{
				return null;
			}
			else
			{
				if (reuse != null)
				{
					reuse.CopyBytes(groupValue);
					return reuse;
				}
				else
				{
					return BytesRef.DeepCopyOf(groupValue);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetNextReader(AtomicReaderContext readerContext)
		{
			base.SetNextReader(readerContext);
			index = FieldCache.DEFAULT.GetTermsIndex(((AtomicReader)readerContext.Reader), 
				groupField);
		}

	    public override AtomicReaderContext NextReader
	    {
	        set { SetNextReader(value); }
	    }
	}
}
