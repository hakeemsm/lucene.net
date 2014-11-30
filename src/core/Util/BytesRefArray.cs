using System;
using System.Collections.Generic;
using Lucene.Net.Support;

namespace Lucene.Net.Util
{
	/// <summary>
	/// A simple append only random-access
	/// <see cref="BytesRef">BytesRef</see>
	/// array that stores full
	/// copies of the appended bytes in a
	/// <see cref="ByteBlockPool">ByteBlockPool</see>
	/// .
	/// <b>Note: This class is not Thread-Safe!</b>
	/// </summary>
	/// <lucene.internal></lucene.internal>
	/// <lucene.experimental></lucene.experimental>
	public sealed class BytesRefArray
	{
		private readonly ByteBlockPool pool;

		private int[] offsets = new int[1];

		private int lastElement = 0;

		private int currentOffset = 0;

		private readonly Counter bytesUsed;

		/// <summary>
		/// Creates a new
		/// <see cref="BytesRefArray">BytesRefArray</see>
		/// with a counter to track allocated bytes
		/// </summary>
		public BytesRefArray(Counter bytesUsed)
		{
			this.pool = new ByteBlockPool(new ByteBlockPool.DirectTrackingAllocator(bytesUsed
				));
			pool.NextBuffer();
			bytesUsed.AddAndGet(RamUsageEstimator.NUM_BYTES_ARRAY_HEADER + RamUsageEstimator.
				NUM_BYTES_INT);
			this.bytesUsed = bytesUsed;
		}

		/// <summary>
		/// Clears this
		/// <see cref="BytesRefArray">BytesRefArray</see>
		/// </summary>
		public void Clear()
		{
			lastElement = 0;
			currentOffset = 0;
			Arrays.Fill(offsets, 0);
			pool.Reset(false, true);
		}

		// no need to 0 fill the buffers we control the allocator
		/// <summary>
		/// Appends a copy of the given
		/// <see cref="BytesRef">BytesRef</see>
		/// to this
		/// <see cref="BytesRefArray">BytesRefArray</see>
		/// .
		/// </summary>
		/// <param name="bytes">the bytes to append</param>
		/// <returns>the index of the appended bytes</returns>
		public int Append(BytesRef bytes)
		{
			if (lastElement >= offsets.Length)
			{
				int oldLen = offsets.Length;
				offsets = ArrayUtil.Grow(offsets, offsets.Length + 1);
				bytesUsed.AddAndGet((offsets.Length - oldLen) * RamUsageEstimator.NUM_BYTES_INT);
			}
			pool.Append(bytes);
			offsets[lastElement++] = currentOffset;
			currentOffset += bytes.length;
			return lastElement - 1;
		}

		/// <summary>
		/// Returns the current size of this
		/// <see cref="BytesRefArray">BytesRefArray</see>
		/// </summary>
		/// <returns>
		/// the current size of this
		/// <see cref="BytesRefArray">BytesRefArray</see>
		/// </returns>
		public int Size()
		{
			return lastElement;
		}

		/// <summary>
		/// Returns the <i>n'th</i> element of this
		/// <see cref="BytesRefArray">BytesRefArray</see>
		/// </summary>
		/// <param name="spare">
		/// a spare
		/// <see cref="BytesRef">BytesRef</see>
		/// instance
		/// </param>
		/// <param name="index">the elements index to retrieve</param>
		/// <returns>
		/// the <i>n'th</i> element of this
		/// <see cref="BytesRefArray">BytesRefArray</see>
		/// </returns>
		public BytesRef Get(BytesRef spare, int index)
		{
			if (lastElement > index)
			{
				int offset = offsets[index];
				int length = index == lastElement - 1 ? currentOffset - offset : offsets[index + 
					1] - offset;
				//HM:revisit 
				//assert spare.offset == 0;
				spare.Grow(length);
				spare.length = length;
				pool.ReadBytes(offset, spare.bytes, spare.offset, spare.length);
				return spare;
			}
			throw new IndexOutOfRangeException("index " + index + " must be less than the size: "
				 + lastElement);
		}

		private int[] Sort(IComparer<BytesRef> comp)
		{
			int[] orderedEntries = new int[Size()];
			for (int i = 0; i < orderedEntries.Length; i++)
			{
				orderedEntries[i] = i;
			}
			new _IntroSorter_125(this, orderedEntries, comp).Sort(0, Size());
			return orderedEntries;
		}

		private sealed class _IntroSorter_125 : IntroSorter
		{
			public _IntroSorter_125(BytesRefArray _enclosing, int[] orderedEntries, IComparer
				<BytesRef> comp)
			{
				this._enclosing = _enclosing;
				this.orderedEntries = orderedEntries;
				this.comp = comp;
				this.pivot = new BytesRef();
				this.scratch1 = new BytesRef();
				this.scratch2 = new BytesRef();
			}

			protected internal override void Swap(int i, int j)
			{
				int o = orderedEntries[i];
				orderedEntries[i] = orderedEntries[j];
				orderedEntries[j] = o;
			}

			protected internal override int Compare(int i, int j)
			{
				int idx1 = orderedEntries[i];
				int idx2 = orderedEntries[j];
				return comp.Compare(this._enclosing.Get(this.scratch1, idx1), this._enclosing.Get
					(this.scratch2, idx2));
			}

			protected internal override void SetPivot(int i)
			{
				int index = orderedEntries[i];
				this._enclosing.Get(this.pivot, index);
			}

			protected internal override int ComparePivot(int j)
			{
				int index = orderedEntries[j];
				return comp.Compare(this.pivot, this._enclosing.Get(this.scratch2, index));
			}

			private readonly BytesRef pivot;

			private readonly BytesRef scratch1;

			private readonly BytesRef scratch2;

			private readonly BytesRefArray _enclosing;

			private readonly int[] orderedEntries;

			private readonly IComparer<BytesRef> comp;
		}

		/// <summary>
		/// sugar for
		/// <see cref="Iterator(System.Collections.Generic.IComparer{T})">Iterator(System.Collections.Generic.IComparer&lt;T&gt;)
		/// 	</see>
		/// with a <code>null</code> comparator
		/// </summary>
		public IBytesRefIterator Iterator()
		{
			return Iterator(null);
		}

		/// <summary>
		/// <p>
		/// Returns a
		/// <see cref="BytesRefIterator">BytesRefIterator</see>
		/// with point in time semantics. The
		/// iterator provides access to all so far appended
		/// <see cref="BytesRef">BytesRef</see>
		/// instances.
		/// </p>
		/// <p>
		/// If a non <code>null</code>
		/// <see cref="System.Collections.IEnumerator{T}">System.Collections.IEnumerator&lt;T&gt;
		/// 	</see>
		/// is provided the iterator will
		/// iterate the byte values in the order specified by the comparator. Otherwise
		/// the order is the same as the values were appended.
		/// </p>
		/// <p>
		/// This is a non-destructive operation.
		/// </p>
		/// </summary>
		public IBytesRefIterator Iterator(IComparer<BytesRef> comp)
		{
			BytesRef spare = new BytesRef();
			int size = Size();
			int[] indices = comp == null ? null : Sort(comp);
			return new BytesRefIteratorImpl(this, size, spare, indices, comp);
		}

		private sealed class BytesRefIteratorImpl : IBytesRefIterator
		{
			public BytesRefIteratorImpl(BytesRefArray _enclosing, int size, BytesRef spare, 
				int[] indices, IComparer<BytesRef> comp)
			{
				this._enclosing = _enclosing;
				this.size = size;
				this.spare = spare;
				this.indices = indices;
				this.comp = comp;
				this.pos = 0;
			}

			internal int pos;

			public BytesRef Next()
			{
				if (this.pos < size)
				{
					return this._enclosing.Get(spare, indices == null ? this.pos++ : indices[this.pos
						++]);
				}
				return null;
			}

			public IComparer<BytesRef> Comparator
			{
			    get { return comp; }
			}

			private readonly BytesRefArray _enclosing;

			private readonly int size;

			private readonly BytesRef spare;

			private readonly int[] indices;

			private readonly IComparer<BytesRef> comp;
		}
	}
}
