/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Sharpen;

namespace Lucene.Net.Facet.Taxonomy
{
	/// <summary>
	/// LRUHashMap is an extension of Java's HashMap, which has a bounded size();
	/// When it reaches that size, each time a new element is added, the least
	/// recently used (LRU) entry is removed.
	/// </summary>
	/// <remarks>
	/// LRUHashMap is an extension of Java's HashMap, which has a bounded size();
	/// When it reaches that size, each time a new element is added, the least
	/// recently used (LRU) entry is removed.
	/// <p>
	/// Java makes it very easy to implement LRUHashMap - all its functionality is
	/// already available from
	/// <see cref="Sharpen.LinkedHashMap{K, V}">Sharpen.LinkedHashMap&lt;K, V&gt;</see>
	/// , and we just need to
	/// configure that properly.
	/// <p>
	/// Note that like HashMap, LRUHashMap is unsynchronized, and the user MUST
	/// synchronize the access to it if used from several threads. Moreover, while
	/// with HashMap this is only a concern if one of the threads is modifies the
	/// map, with LURHashMap every read is a modification (because the LRU order
	/// needs to be remembered) so proper synchronization is always necessary.
	/// <p>
	/// With the usual synchronization mechanisms available to the user, this
	/// unfortunately means that LRUHashMap will probably perform sub-optimally under
	/// heavy contention: while one thread uses the hash table (reads or writes), any
	/// other thread will be blocked from using it - or even just starting to use it
	/// (e.g., calculating the hash function). A more efficient approach would be not
	/// to use LinkedHashMap at all, but rather to use a non-locking (as much as
	/// possible) thread-safe solution, something along the lines of
	/// java.util.concurrent.ConcurrentHashMap (though that particular class does not
	/// support the additional LRU semantics, which will need to be added separately
	/// using a concurrent linked list or additional storage of timestamps (in an
	/// array or inside the entry objects), or whatever).
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	[System.Serializable]
	public class LRUHashMap<K, V> : LinkedHashMap<K, V>
	{
		private int maxSize;

		/// <summary>
		/// Create a new hash map with a bounded size and with least recently
		/// used entries removed.
		/// </summary>
		/// <remarks>
		/// Create a new hash map with a bounded size and with least recently
		/// used entries removed.
		/// </remarks>
		/// <param name="maxSize">
		/// the maximum size (in number of entries) to which the map can grow
		/// before the least recently used entries start being removed.<BR>
		/// Setting maxSize to a very large value, like
		/// <see cref="int.MaxValue">int.MaxValue</see>
		/// is allowed, but is less efficient than
		/// using
		/// <see cref="System.Collections.Hashtable{K, V}">System.Collections.Hashtable&lt;K, V&gt;
		/// 	</see>
		/// because our class needs
		/// to keep track of the use order (via an additional doubly-linked
		/// list) which is not used when the map's size is always below the
		/// maximum size.
		/// </param>
		public LRUHashMap(int maxSize) : base(16, 0.75f, true)
		{
			this.maxSize = maxSize;
		}

		/// <summary>Return the max size</summary>
		public virtual int GetMaxSize()
		{
			return maxSize;
		}

		/// <summary>
		/// setMaxSize() allows changing the map's maximal number of elements
		/// which was defined at construction time.
		/// </summary>
		/// <remarks>
		/// setMaxSize() allows changing the map's maximal number of elements
		/// which was defined at construction time.
		/// <P>
		/// Note that if the map is already larger than maxSize, the current
		/// implementation does not shrink it (by removing the oldest elements);
		/// Rather, the map remains in its current size as new elements are
		/// added, and will only start shrinking (until settling again on the
		/// give maxSize) if existing elements are explicitly deleted.
		/// </remarks>
		public virtual void SetMaxSize(int maxSize)
		{
			this.maxSize = maxSize;
		}

		// We override LinkedHashMap's removeEldestEntry() method. This method
		// is called every time a new entry is added, and if we return true
		// here, the eldest element will be deleted automatically. In our case,
		// we return true if the size of the map grew beyond our limit - ignoring
		// what is that eldest element that we'll be deleting.
		protected override bool RemoveEldestEntry(KeyValuePair<K, V> eldest)
		{
			return Count > maxSize;
		}

		protected override object Clone()
		{
			return (Lucene.Net.Facet.Taxonomy.LRUHashMap<K, V>)base.Clone();
		}
	}
}
