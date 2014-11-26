/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Writercache;
using Sharpen;

namespace Lucene.Net.Facet.Taxonomy.Writercache
{
	/// <summary>HashMap to store colliding labels.</summary>
	/// <remarks>
	/// HashMap to store colliding labels. See
	/// <see cref="CompactLabelToOrdinal">CompactLabelToOrdinal</see>
	/// for
	/// details.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class CollisionMap
	{
		private int capacity;

		private float loadFactor;

		private int size;

		private int threshold;

		internal class Entry
		{
			internal int offset;

			internal int cid;

			internal CollisionMap.Entry next;

			internal int hash;

			internal Entry(int offset, int cid, int h, CollisionMap.Entry e)
			{
				this.offset = offset;
				this.cid = cid;
				this.next = e;
				this.hash = h;
			}
		}

		private CharBlockArray labelRepository;

		private CollisionMap.Entry[] entries;

		internal CollisionMap(CharBlockArray labelRepository) : this(16 * 1024, 0.75f, labelRepository
			)
		{
		}

		internal CollisionMap(int initialCapacity, CharBlockArray labelRepository) : this
			(initialCapacity, 0.75f, labelRepository)
		{
		}

		private CollisionMap(int initialCapacity, float loadFactor, CharBlockArray labelRepository
			)
		{
			this.labelRepository = labelRepository;
			this.loadFactor = loadFactor;
			this.capacity = CompactLabelToOrdinal.DetermineCapacity(2, initialCapacity);
			this.entries = new CollisionMap.Entry[this.capacity];
			this.threshold = (int)(this.capacity * this.loadFactor);
		}

		/// <summary>How many mappings.</summary>
		/// <remarks>How many mappings.</remarks>
		public virtual int Size()
		{
			return this.size;
		}

		/// <summary>How many slots are allocated.</summary>
		/// <remarks>How many slots are allocated.</remarks>
		public virtual int Capacity()
		{
			return this.capacity;
		}

		private void Grow()
		{
			int newCapacity = this.capacity * 2;
			CollisionMap.Entry[] newEntries = new CollisionMap.Entry[newCapacity];
			CollisionMap.Entry[] src = this.entries;
			for (int j = 0; j < src.Length; j++)
			{
				CollisionMap.Entry e = src[j];
				if (e != null)
				{
					src[j] = null;
					do
					{
						CollisionMap.Entry next = e.next;
						int hash = e.hash;
						int i = IndexFor(hash, newCapacity);
						e.next = newEntries[i];
						newEntries[i] = e;
						e = next;
					}
					while (e != null);
				}
			}
			this.capacity = newCapacity;
			this.entries = newEntries;
			this.threshold = (int)(this.capacity * this.loadFactor);
		}

		/// <summary>
		/// Return the mapping, or
		/// <see cref="LabelToOrdinal.INVALID_ORDINAL">LabelToOrdinal.INVALID_ORDINAL</see>
		/// if the label isn't
		/// recognized.
		/// </summary>
		public virtual int Get(FacetLabel label, int hash)
		{
			int bucketIndex = IndexFor(hash, this.capacity);
			CollisionMap.Entry e = this.entries[bucketIndex];
			while (e != null && !(hash == e.hash && CategoryPathUtils.EqualsToSerialized(label
				, labelRepository, e.offset)))
			{
				e = e.next;
			}
			if (e == null)
			{
				return LabelToOrdinal.INVALID_ORDINAL;
			}
			return e.cid;
		}

		/// <summary>Add another mapping.</summary>
		/// <remarks>Add another mapping.</remarks>
		public virtual int AddLabel(FacetLabel label, int hash, int cid)
		{
			int bucketIndex = IndexFor(hash, this.capacity);
			for (CollisionMap.Entry e = this.entries[bucketIndex]; e != null; e = e.next)
			{
				if (e.hash == hash && CategoryPathUtils.EqualsToSerialized(label, labelRepository
					, e.offset))
				{
					return e.cid;
				}
			}
			// new string; add to label repository
			int offset = labelRepository.Length;
			CategoryPathUtils.Serialize(label, labelRepository);
			AddEntry(offset, cid, hash, bucketIndex);
			return cid;
		}

		/// <summary>
		/// This method does not check if the same value is already in the map because
		/// we pass in an char-array offset, so so we now that we're in resize-mode
		/// here.
		/// </summary>
		/// <remarks>
		/// This method does not check if the same value is already in the map because
		/// we pass in an char-array offset, so so we now that we're in resize-mode
		/// here.
		/// </remarks>
		public virtual void AddLabelOffset(int hash, int offset, int cid)
		{
			int bucketIndex = IndexFor(hash, this.capacity);
			AddEntry(offset, cid, hash, bucketIndex);
		}

		private void AddEntry(int offset, int cid, int hash, int bucketIndex)
		{
			CollisionMap.Entry e = this.entries[bucketIndex];
			this.entries[bucketIndex] = new CollisionMap.Entry(offset, cid, hash, e);
			if (this.size++ >= this.threshold)
			{
				Grow();
			}
		}

		internal virtual Iterator<CollisionMap.Entry> EntryIterator()
		{
			return new CollisionMap.EntryIterator(this, entries, size);
		}

		/// <summary>Returns index for hash code h.</summary>
		/// <remarks>Returns index for hash code h.</remarks>
		internal static int IndexFor(int h, int length)
		{
			return h & (length - 1);
		}

		/// <summary>Returns an estimate of the memory usage of this CollisionMap.</summary>
		/// <remarks>Returns an estimate of the memory usage of this CollisionMap.</remarks>
		/// <returns>The approximate number of bytes used by this structure.</returns>
		internal virtual int GetMemoryUsage()
		{
			int memoryUsage = 0;
			if (this.entries != null)
			{
				foreach (CollisionMap.Entry e in this.entries)
				{
					if (e != null)
					{
						memoryUsage += (4 * 4);
						for (CollisionMap.Entry ee = e.next; ee != null; ee = ee.next)
						{
							memoryUsage += (4 * 4);
						}
					}
				}
			}
			return memoryUsage;
		}

		private class EntryIterator : Iterator<CollisionMap.Entry>
		{
			internal CollisionMap.Entry next;

			internal int index;

			internal CollisionMap.Entry[] ents;

			internal EntryIterator(CollisionMap _enclosing, CollisionMap.Entry[] entries, int
				 size)
			{
				this._enclosing = _enclosing;
				// next entry to return
				// current slot 
				this.ents = entries;
				CollisionMap.Entry[] t = entries;
				int i = t.Length;
				CollisionMap.Entry n = null;
				if (size != 0)
				{
					// advance to first entry
					while (i > 0 && (n = t[--i]) == null)
					{
					}
				}
				// advance
				this.next = n;
				this.index = i;
			}

			public override bool HasNext()
			{
				return this.next != null;
			}

			public override CollisionMap.Entry Next()
			{
				CollisionMap.Entry e = this.next;
				if (e == null)
				{
					throw new NoSuchElementException();
				}
				CollisionMap.Entry n = e.next;
				CollisionMap.Entry[] t = this.ents;
				int i = this.index;
				while (n == null && i > 0)
				{
					n = t[--i];
				}
				this.index = i;
				this.next = n;
				return e;
			}

			public override void Remove()
			{
				throw new NotSupportedException();
			}

			private readonly CollisionMap _enclosing;
		}
	}
}
