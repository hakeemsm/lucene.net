/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using System.Collections.Generic;
using Sharpen;

namespace Lucene.Net.Spatial.Util
{
	/// <summary>Bounded Cache of Shapes associated with docIds.</summary>
	/// <remarks>
	/// Bounded Cache of Shapes associated with docIds.  Note, multiple Shapes can be
	/// associated with a given docId.
	/// <p>
	/// WARNING: This class holds the data in an extremely inefficient manner as all Points are in memory as objects and they
	/// are stored in many ArrayLists (one per document).  So it works but doesn't scale.  It will be replaced in the future.
	/// </remarks>
	/// <lucene.internal></lucene.internal>
	public class ShapeFieldCache<T> where T:Com.Spatial4j.Core.Shape.Shape
	{
		private IList<T>[] cache;

		public int defaultLength;

		public ShapeFieldCache(int length, int defaultLength)
		{
			cache = new IList[length];
			this.defaultLength = defaultLength;
		}

		public virtual void Add(int docid, T s)
		{
			IList<T> list = cache[docid];
			if (list == null)
			{
				list = cache[docid] = new AList<T>(defaultLength);
			}
			list.AddItem(s);
		}

		public virtual IList<T> GetShapes(int docid)
		{
			return cache[docid];
		}
	}
}
