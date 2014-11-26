/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Util;
using Lucene.Net.Util;
using Sharpen;
using Sharpen.Logging;

namespace Lucene.Net.Spatial.Util
{
	/// <summary>
	/// Provides access to a
	/// <see cref="ShapeFieldCache{T}">ShapeFieldCache&lt;T&gt;</see>
	/// for a given
	/// <see cref="Lucene.Net.Index.AtomicReader">Lucene.Net.Index.AtomicReader
	/// 	</see>
	/// .
	/// If a Cache does not exist for the Reader, then it is built by iterating over
	/// the all terms for a given field, reconstructing the Shape from them, and adding
	/// them to the Cache.
	/// </summary>
	/// <lucene.internal></lucene.internal>
	public abstract class ShapeFieldCacheProvider<T> where T:Com.Spatial4j.Core.Shape.Shape
	{
		private Logger log = Logger.GetLogger(GetType().FullName);

		internal WeakHashMap<IndexReader, ShapeFieldCache<T>> sidx = new WeakHashMap<IndexReader
			, ShapeFieldCache<T>>();

		protected internal readonly int defaultSize;

		protected internal readonly string shapeField;

		public ShapeFieldCacheProvider(string shapeField, int defaultSize)
		{
			// it may be a List<T> or T
			this.shapeField = shapeField;
			this.defaultSize = defaultSize;
		}

		protected internal abstract T ReadShape(BytesRef term);

		/// <exception cref="System.IO.IOException"></exception>
		public virtual ShapeFieldCache<T> GetCache(AtomicReader reader)
		{
			lock (this)
			{
				ShapeFieldCache<T> idx = sidx.Get(reader);
				if (idx != null)
				{
					return idx;
				}
				long startTime = Runtime.CurrentTimeMillis();
				log.Fine("Building Cache [" + reader.MaxDoc() + "]");
				idx = new ShapeFieldCache<T>(reader.MaxDoc(), defaultSize);
				int count = 0;
				DocsEnum docs = null;
				Terms terms = reader.Terms(shapeField);
				TermsEnum te = null;
				if (terms != null)
				{
					te = terms.Iterator(te);
					BytesRef term = te.Next();
					while (term != null)
					{
						T shape = ReadShape(term);
						if (shape != null)
						{
							docs = te.Docs(null, docs, DocsEnum.FLAG_NONE);
							int docid = docs.NextDoc();
							while (docid != DocIdSetIterator.NO_MORE_DOCS)
							{
								idx.Add(docid, shape);
								docid = docs.NextDoc();
								count++;
							}
						}
						term = te.Next();
					}
				}
				sidx.Put(reader, idx);
				long elapsed = Runtime.CurrentTimeMillis() - startTime;
				log.Fine("Cached: [" + count + " in " + elapsed + "ms] " + idx);
				return idx;
			}
		}
	}
}
