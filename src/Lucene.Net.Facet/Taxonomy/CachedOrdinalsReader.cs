/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Facet.Taxonomy
{
	/// <summary>A per-segment cache of documents' facet ordinals.</summary>
	/// <remarks>
	/// A per-segment cache of documents' facet ordinals. Every
	/// <see cref="CachedOrds">CachedOrds</see>
	/// holds the ordinals in a raw
	/// <code>int[]</code>
	/// , and therefore consumes as much RAM as the total
	/// number of ordinals found in the segment, but saves the
	/// CPU cost of decoding ordinals during facet counting.
	/// <p>
	/// <b>NOTE:</b> every
	/// <see cref="CachedOrds">CachedOrds</see>
	/// is limited to 2.1B
	/// total ordinals. If that is a limitation for you then
	/// consider limiting the segment size to fewer documents, or
	/// use an alternative cache which pages through the category
	/// ordinals.
	/// <p>
	/// <b>NOTE:</b> when using this cache, it is advised to use
	/// a
	/// <see cref="Lucene.Net.Codecs.DocValuesFormat">Lucene.Net.Codecs.DocValuesFormat
	/// 	</see>
	/// that does not cache the data in
	/// memory, at least for the category lists fields, or
	/// otherwise you'll be doing double-caching.
	/// <p>
	/// <b>NOTE:</b> create one instance of this and re-use it
	/// for all facet implementations (the cache is per-instance,
	/// not static).
	/// </remarks>
	public class CachedOrdinalsReader : OrdinalsReader
	{
		private readonly OrdinalsReader source;

		private readonly IDictionary<object, CachedOrdinalsReader.CachedOrds> ordsCache = 
			new WeakHashMap<object, CachedOrdinalsReader.CachedOrds>();

		/// <summary>Sole constructor.</summary>
		/// <remarks>Sole constructor.</remarks>
		public CachedOrdinalsReader(OrdinalsReader source)
		{
			this.source = source;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private CachedOrdinalsReader.CachedOrds GetCachedOrds(AtomicReaderContext context
			)
		{
			lock (this)
			{
				object cacheKey = ((AtomicReader)context.Reader()).GetCoreCacheKey();
				CachedOrdinalsReader.CachedOrds ords = ordsCache.Get(cacheKey);
				if (ords == null)
				{
					ords = new CachedOrdinalsReader.CachedOrds(source.GetReader(context), ((AtomicReader
						)context.Reader()).MaxDoc());
					ordsCache.Put(cacheKey, ords);
				}
				return ords;
			}
		}

		public override string GetIndexFieldName()
		{
			return source.GetIndexFieldName();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override OrdinalsReader.OrdinalsSegmentReader GetReader(AtomicReaderContext
			 context)
		{
			CachedOrdinalsReader.CachedOrds cachedOrds = GetCachedOrds(context);
			return new _OrdinalsSegmentReader_86(cachedOrds);
		}

		private sealed class _OrdinalsSegmentReader_86 : OrdinalsReader.OrdinalsSegmentReader
		{
			public _OrdinalsSegmentReader_86(CachedOrdinalsReader.CachedOrds cachedOrds)
			{
				this.cachedOrds = cachedOrds;
			}

			public override void Get(int docID, IntsRef ordinals)
			{
				ordinals.ints = cachedOrds.ordinals;
				ordinals.offset = cachedOrds.offsets[docID];
				ordinals.length = cachedOrds.offsets[docID + 1] - ordinals.offset;
			}

			private readonly CachedOrdinalsReader.CachedOrds cachedOrds;
		}

		/// <summary>
		/// Holds the cached ordinals in two parallel
		/// <code>int[]</code>
		/// arrays.
		/// </summary>
		public sealed class CachedOrds
		{
			/// <summary>
			/// Index into
			/// <see cref="ordinals">ordinals</see>
			/// for each document.
			/// </summary>
			public readonly int[] offsets;

			/// <summary>Holds ords for all docs.</summary>
			/// <remarks>Holds ords for all docs.</remarks>
			public readonly int[] ordinals;

			/// <summary>
			/// Creates a new
			/// <see cref="CachedOrds">CachedOrds</see>
			/// from the
			/// <see cref="Lucene.Net.Index.BinaryDocValues">Lucene.Net.Index.BinaryDocValues
			/// 	</see>
			/// .
			/// Assumes that the
			/// <see cref="Lucene.Net.Index.BinaryDocValues">Lucene.Net.Index.BinaryDocValues
			/// 	</see>
			/// is not
			/// <code>null</code>
			/// .
			/// </summary>
			/// <exception cref="System.IO.IOException"></exception>
			public CachedOrds(OrdinalsReader.OrdinalsSegmentReader source, int maxDoc)
			{
				offsets = new int[maxDoc + 1];
				int[] ords = new int[maxDoc];
				// let's assume one ordinal per-document as an initial size
				// this aggregator is limited to Integer.MAX_VALUE total ordinals.
				long totOrds = 0;
				IntsRef values = new IntsRef(32);
				for (int docID = 0; docID < maxDoc; docID++)
				{
					offsets[docID] = (int)totOrds;
					source.Get(docID, values);
					long nextLength = totOrds + values.length;
					if (nextLength > ords.Length)
					{
						if (nextLength > ArrayUtil.MAX_ARRAY_LENGTH)
						{
							throw new InvalidOperationException("too many ordinals (>= " + nextLength + ") to cache"
								);
						}
						ords = ArrayUtil.Grow(ords, (int)nextLength);
					}
					System.Array.Copy(values.ints, 0, ords, (int)totOrds, values.length);
					totOrds = nextLength;
				}
				offsets[maxDoc] = (int)totOrds;
				// if ords array is bigger by more than 10% of what we really need, shrink it
				if ((double)totOrds / ords.Length < 0.9)
				{
					this.ordinals = new int[(int)totOrds];
					System.Array.Copy(ords, 0, this.ordinals, 0, (int)totOrds);
				}
				else
				{
					this.ordinals = ords;
				}
			}

			/// <summary>Returns number of bytes used by this cache entry</summary>
			public long RamBytesUsed()
			{
				long mem = RamUsageEstimator.ShallowSizeOf(this) + RamUsageEstimator.SizeOf(offsets
					);
				if (offsets != ordinals)
				{
					mem += RamUsageEstimator.SizeOf(ordinals);
				}
				return mem;
			}
		}

		/// <summary>How many bytes is this cache using?</summary>
		public virtual long RamBytesUsed()
		{
			lock (this)
			{
				long bytes = 0;
				foreach (CachedOrdinalsReader.CachedOrds ords in ordsCache.Values)
				{
					bytes += ords.RamBytesUsed();
				}
				return bytes;
			}
		}
	}
}
