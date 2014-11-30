/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using Sharpen;

namespace Lucene.Net.Index
{
	/// <summary>
	/// A
	/// <see cref="DocValuesFieldUpdates">DocValuesFieldUpdates</see>
	/// which holds updates of documents, of a single
	/// <see cref="Lucene.Net.Document.NumericDocValuesField">Lucene.Net.Document.NumericDocValuesField
	/// 	</see>
	/// .
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	internal class NumericDocValuesFieldUpdates : DocValuesFieldUpdates
	{
		internal sealed class Iterator : DocValuesFieldUpdates.Iterator
		{
			private readonly int size;

			private readonly PagedGrowableWriter values;

			private readonly FixedBitSet docsWithField;

			private readonly PagedMutable docs;

			private long idx = 0;

			private int doc = -1;

			private long value = null;

			internal Iterator(int size, PagedGrowableWriter values, FixedBitSet docsWithField
				, PagedMutable docs)
			{
				// long so we don't overflow if size == Integer.MAX_VALUE
				this.size = size;
				this.values = values;
				this.docsWithField = docsWithField;
				this.docs = docs;
			}

			internal override object Value()
			{
				return value;
			}

			internal override int NextDoc()
			{
				if (idx >= size)
				{
					value = null;
					return doc = DocIdSetIterator.NO_MORE_DOCS;
				}
				doc = (int)docs.Get(idx);
				++idx;
				while (idx < size && docs.Get(idx) == doc)
				{
					++idx;
				}
				if (!docsWithField.Get((int)(idx - 1)))
				{
					value = null;
				}
				else
				{
					// idx points to the "next" element
					value = Sharpen.Extensions.ValueOf(values.Get(idx - 1));
				}
				return doc;
			}

			internal override int Doc()
			{
				return doc;
			}

			internal override void Reset()
			{
				doc = -1;
				value = null;
				idx = 0;
			}
		}

		private FixedBitSet docsWithField;

		private PagedMutable docs;

		private PagedGrowableWriter values;

		private int size;

		public NumericDocValuesFieldUpdates(string field, int maxDoc) : base(field, DocValuesFieldUpdates.Type
			.NUMERIC)
		{
			docsWithField = new FixedBitSet(64);
			docs = new PagedMutable(1, 1024, PackedInts.BitsRequired(maxDoc - 1), PackedInts.
				COMPACT);
			values = new PagedGrowableWriter(1, 1024, 1, PackedInts.FAST);
			size = 0;
		}

		public override void Add(int doc, object value)
		{
			// TODO: if the Sorter interface changes to take long indexes, we can remove that limitation
			if (size == int.MaxValue)
			{
				throw new InvalidOperationException("cannot support more than Integer.MAX_VALUE doc/value entries"
					);
			}
			long val = (long)value;
			if (val == null)
			{
				val = DocValuesUpdate.NumericDocValuesUpdate.MISSING;
			}
			// grow the structures to have room for more elements
			if (docs.Size() == size)
			{
				docs = docs.Grow(size + 1);
				values = values.Grow(size + 1);
				docsWithField = FixedBitSet.EnsureCapacity(docsWithField, (int)docs.Size());
			}
			if (val != DocValuesUpdate.NumericDocValuesUpdate.MISSING)
			{
				// only mark the document as having a value in that field if the value wasn't set to null (MISSING)
				docsWithField.Set(size);
			}
			docs.Set(size, doc);
			values.Set(size, val);
			++size;
		}

		public override DocValuesFieldUpdates.Iterator GetIterator()
		{
			PagedMutable docs = this.docs;
			PagedGrowableWriter values = this.values;
			FixedBitSet docsWithField = this.docsWithField;
			new _InPlaceMergeSorter_138(docs, values, docsWithField).Sort(0, size);
			return new NumericDocValuesFieldUpdates.Iterator(size, values, docsWithField, docs
				);
		}

		private sealed class _InPlaceMergeSorter_138 : InPlaceMergeSorter
		{
			public _InPlaceMergeSorter_138(PagedMutable docs, PagedGrowableWriter values, FixedBitSet
				 docsWithField)
			{
				this.docs = docs;
				this.values = values;
				this.docsWithField = docsWithField;
			}

			protected internal override void Swap(int i, int j)
			{
				long tmpDoc = docs.Get(j);
				docs.Set(j, docs.Get(i));
				docs.Set(i, tmpDoc);
				long tmpVal = values.Get(j);
				values.Set(j, values.Get(i));
				values.Set(i, tmpVal);
				bool tmpBool = docsWithField.Get(j);
				if (docsWithField.Get(i))
				{
					docsWithField.Set(j);
				}
				else
				{
					docsWithField.Clear(j);
				}
				if (tmpBool)
				{
					docsWithField.Set(i);
				}
				else
				{
					docsWithField.Clear(i);
				}
			}

			protected internal override int Compare(int i, int j)
			{
				int x = (int)docs.Get(i);
				int y = (int)docs.Get(j);
				return (x < y) ? -1 : ((x == y) ? 0 : 1);
			}

			private readonly PagedMutable docs;

			private readonly PagedGrowableWriter values;

			private readonly FixedBitSet docsWithField;
		}

		public override void Merge(DocValuesFieldUpdates other)
		{
			//HM:revisit 
			//assert other instanceof NumericDocValuesFieldUpdates;
			NumericDocValuesFieldUpdates otherUpdates = (NumericDocValuesFieldUpdates)other;
			if (size + otherUpdates.size > int.MaxValue)
			{
				throw new InvalidOperationException("cannot support more than Integer.MAX_VALUE doc/value entries; size="
					 + size + " other.size=" + otherUpdates.size);
			}
			docs = docs.Grow(size + otherUpdates.size);
			values = values.Grow(size + otherUpdates.size);
			docsWithField = FixedBitSet.EnsureCapacity(docsWithField, (int)docs.Size());
			for (int i = 0; i < otherUpdates.size; i++)
			{
				int doc = (int)otherUpdates.docs.Get(i);
				if (otherUpdates.docsWithField.Get(i))
				{
					docsWithField.Set(size);
				}
				docs.Set(size, doc);
				values.Set(size, otherUpdates.values.Get(i));
				++size;
			}
		}

		public override bool Any()
		{
			return size > 0;
		}
	}
}
