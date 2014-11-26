/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Sortedset;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Facet.Sortedset
{
	/// <summary>
	/// Default implementation of
	/// <see cref="SortedSetDocValuesFacetCounts">SortedSetDocValuesFacetCounts</see>
	/// </summary>
	public class DefaultSortedSetDocValuesReaderState : SortedSetDocValuesReaderState
	{
		private readonly string field;

		private readonly AtomicReader topReader;

		private readonly int valueCount;

		/// <summary>
		/// <see cref="Lucene.Net.Index.IndexReader">Lucene.Net.Index.IndexReader
		/// 	</see>
		/// passed to the constructor.
		/// </summary>
		public readonly IndexReader origReader;

		private readonly IDictionary<string, SortedSetDocValuesReaderState.OrdRange> prefixToOrdRange
			 = new Dictionary<string, SortedSetDocValuesReaderState.OrdRange>();

		/// <summary>
		/// Creates this, pulling doc values from the default
		/// <see cref="Lucene.Net.Facet.FacetsConfig.DEFAULT_INDEX_FIELD_NAME">Lucene.Net.Facet.FacetsConfig.DEFAULT_INDEX_FIELD_NAME
		/// 	</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public DefaultSortedSetDocValuesReaderState(IndexReader reader) : this(reader, FacetsConfig
			.DEFAULT_INDEX_FIELD_NAME)
		{
		}

		/// <summary>
		/// Creates this, pulling doc values from the specified
		/// field.
		/// </summary>
		/// <remarks>
		/// Creates this, pulling doc values from the specified
		/// field.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public DefaultSortedSetDocValuesReaderState(IndexReader reader, string field)
		{
			this.field = field;
			this.origReader = reader;
			// We need this to create thread-safe MultiSortedSetDV
			// per collector:
			topReader = SlowCompositeReaderWrapper.Wrap(reader);
			SortedSetDocValues dv = topReader.GetSortedSetDocValues(field);
			if (dv == null)
			{
				throw new ArgumentException("field \"" + field + "\" was not indexed with SortedSetDocValues"
					);
			}
			if (dv.GetValueCount() > int.MaxValue)
			{
				throw new ArgumentException("can only handle valueCount < Integer.MAX_VALUE; got "
					 + dv.GetValueCount());
			}
			valueCount = (int)dv.GetValueCount();
			// TODO: we can make this more efficient if eg we can be
			// "involved" when OrdinalMap is being created?  Ie see
			// each term/ord it's assigning as it goes...
			string lastDim = null;
			int startOrd = -1;
			BytesRef spare = new BytesRef();
			// TODO: this approach can work for full hierarchy?;
			// TaxoReader can't do this since ords are not in
			// "sorted order" ... but we should generalize this to
			// support arbitrary hierarchy:
			for (int ord = 0; ord < valueCount; ord++)
			{
				dv.LookupOrd(ord, spare);
				string[] components = FacetsConfig.StringToPath(spare.Utf8ToString());
				if (components.Length != 2)
				{
					throw new ArgumentException("this class can only handle 2 level hierarchy (dim/value); got: "
						 + Arrays.ToString(components) + " " + spare.Utf8ToString());
				}
				if (!components[0].Equals(lastDim))
				{
					if (lastDim != null)
					{
						prefixToOrdRange.Put(lastDim, new SortedSetDocValuesReaderState.OrdRange(startOrd
							, ord - 1));
					}
					startOrd = ord;
					lastDim = components[0];
				}
			}
			if (lastDim != null)
			{
				prefixToOrdRange.Put(lastDim, new SortedSetDocValuesReaderState.OrdRange(startOrd
					, valueCount - 1));
			}
		}

		/// <summary>Return top-level doc values.</summary>
		/// <remarks>Return top-level doc values.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public override SortedSetDocValues GetDocValues()
		{
			return topReader.GetSortedSetDocValues(field);
		}

		/// <summary>
		/// Returns mapping from prefix to
		/// <see cref="OrdRange">OrdRange</see>
		/// .
		/// </summary>
		public override IDictionary<string, SortedSetDocValuesReaderState.OrdRange> GetPrefixToOrdRange
			()
		{
			return prefixToOrdRange;
		}

		/// <summary>
		/// Returns the
		/// <see cref="OrdRange">OrdRange</see>
		/// for this dimension.
		/// </summary>
		public override SortedSetDocValuesReaderState.OrdRange GetOrdRange(string dim)
		{
			return prefixToOrdRange.Get(dim);
		}

		/// <summary>Indexed field we are reading.</summary>
		/// <remarks>Indexed field we are reading.</remarks>
		public override string GetField()
		{
			return field;
		}

		public override IndexReader GetOrigReader()
		{
			return origReader;
		}

		/// <summary>Number of unique labels.</summary>
		/// <remarks>Number of unique labels.</remarks>
		public override int GetSize()
		{
			return valueCount;
		}
	}
}
