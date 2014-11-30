using System;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;

namespace Lucene.Net.Index
{
	
	internal class BinaryDocValuesFieldUpdates : DocValuesFieldUpdates
	{
		internal sealed class Iterator : DocValuesFieldUpdates.Iterator
		{
			private readonly PagedGrowableWriter offsets;

			private readonly int size;

			private readonly PagedGrowableWriter lengths;

			private readonly PagedMutable docs;

			private readonly FixedBitSet docsWithField;

			private long idx = 0;

			private int doc = -1;

			private readonly BytesRef value;

			private int offset;

			private int length;

			internal Iterator(int size, PagedGrowableWriter offsets, PagedGrowableWriter lengths
				, PagedMutable docs, BytesRef values, FixedBitSet docsWithField)
			{
				// long so we don't overflow if size == Integer.MAX_VALUE
				this.offsets = offsets;
				this.size = size;
				this.lengths = lengths;
				this.docs = docs;
				this.docsWithField = docsWithField;
				value = (BytesRef) values.Clone();
			}

			internal override object Value()
			{
				if (offset == -1)
				{
					return null;
				}
				else
				{
					value.offset = offset;
					value.length = length;
					return value;
				}
			}

			internal override int NextDoc()
			{
				if (idx >= size)
				{
					offset = -1;
					return doc = DocIdSetIterator.NO_MORE_DOCS;
				}
				doc = (int)docs.Get(idx);
				++idx;
				while (idx < size && docs.Get(idx) == doc)
				{
					++idx;
				}
				// idx points to the "next" element
				long prevIdx = idx - 1;
				if (!docsWithField[(int)prevIdx])
				{
					offset = -1;
				}
				else
				{
					// cannot change 'value' here because nextDoc is called before the
					// value is used, and it's a waste to clone the BytesRef when we
					// obtain the value
					offset = (int)offsets.Get(prevIdx);
					length = (int)lengths.Get(prevIdx);
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
				offset = -1;
				idx = 0;
			}
		}

		private FixedBitSet docsWithField;

		private PagedMutable docs;

		private PagedGrowableWriter offsets;

		private PagedGrowableWriter lengths;

		private BytesRef values;

		private int size;

		public BinaryDocValuesFieldUpdates(string field, int maxDoc) : base(field, DocValuesFieldUpdates.Type
			.BINARY)
		{
			docsWithField = new FixedBitSet(64);
			docs = new PagedMutable(1, 1024, PackedInts.BitsRequired(maxDoc - 1), PackedInts.
				COMPACT);
			offsets = new PagedGrowableWriter(1, 1024, 1, PackedInts.FAST);
			lengths = new PagedGrowableWriter(1, 1024, 1, PackedInts.FAST);
			values = new BytesRef(16);
			// start small
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
			BytesRef val = (BytesRef)value;
			if (val == null)
			{
				val = DocValuesUpdate.BinaryDocValuesUpdate.MISSING;
			}
			// grow the structures to have room for more elements
			if (docs.Size() == size)
			{
				docs = docs.Grow(size + 1);
				offsets = offsets.Grow(size + 1);
				lengths = lengths.Grow(size + 1);
				docsWithField = FixedBitSet.EnsureCapacity(docsWithField, (int)docs.Size());
			}
			if (val != DocValuesUpdate.BinaryDocValuesUpdate.MISSING)
			{
				// only mark the document as having a value in that field if the value wasn't set to null (MISSING)
				docsWithField.Set(size);
			}
			docs.Set(size, doc);
			offsets.Set(size, values.length);
			lengths.Set(size, val.length);
			values.Append(val);
			++size;
		}

		public override DocValuesFieldUpdates.Iterator GetIterator()
		{
			PagedMutable docs = this.docs;
			PagedGrowableWriter offsets = this.offsets;
			PagedGrowableWriter lengths = this.lengths;
			BytesRef values = this.values;
			FixedBitSet docsWithField = this.docsWithField;
			new _InPlaceMergeSorter_163(docs, offsets, lengths, docsWithField).Sort(0, size);
			return new BinaryDocValuesFieldUpdates.Iterator(size, offsets, lengths, docs, values
				, docsWithField);
		}

		private sealed class _InPlaceMergeSorter_163 : InPlaceMergeSorter
		{
			public _InPlaceMergeSorter_163(PagedMutable docs, PagedGrowableWriter offsets, PagedGrowableWriter
				 lengths, FixedBitSet docsWithField)
			{
				this.docs = docs;
				this.offsets = offsets;
				this.lengths = lengths;
				this.docsWithField = docsWithField;
			}

			protected internal override void Swap(int i, int j)
			{
				long tmpDoc = docs.Get(j);
				docs.Set(j, docs.Get(i));
				docs.Set(i, tmpDoc);
				long tmpOffset = offsets.Get(j);
				offsets.Set(j, offsets.Get(i));
				offsets.Set(i, tmpOffset);
				long tmpLength = lengths.Get(j);
				lengths.Set(j, lengths.Get(i));
				lengths.Set(i, tmpLength);
				bool tmpBool = docsWithField[j];
				if (docsWithField[i])
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

			private readonly PagedGrowableWriter offsets;

			private readonly PagedGrowableWriter lengths;

			private readonly FixedBitSet docsWithField;
		}

		public override void Merge(DocValuesFieldUpdates other)
		{
			BinaryDocValuesFieldUpdates otherUpdates = (BinaryDocValuesFieldUpdates)other;
			int newSize = size + otherUpdates.size;
			if (newSize > int.MaxValue)
			{
				throw new InvalidOperationException("cannot support more than Integer.MAX_VALUE doc/value entries; size="
					 + size + " other.size=" + otherUpdates.size);
			}
			docs = docs.Grow(newSize);
			offsets = offsets.Grow(newSize);
			lengths = lengths.Grow(newSize);
			docsWithField = FixedBitSet.EnsureCapacity(docsWithField, (int)docs.Size());
			for (int i = 0; i < otherUpdates.size; i++)
			{
				int doc = (int)otherUpdates.docs.Get(i);
				if (otherUpdates.docsWithField.Get(i))
				{
					docsWithField.Set(size);
				}
				docs.Set(size, doc);
				offsets.Set(size, values.length + otherUpdates.offsets.Get(i));
				// correct relative offset
				lengths.Set(size, otherUpdates.lengths.Get(i));
				++size;
			}
			values.Append(otherUpdates.values);
		}

		public override bool Any()
		{
			return size > 0;
		}
	}
}
