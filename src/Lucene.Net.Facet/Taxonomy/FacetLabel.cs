/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Facet.Taxonomy
{
	/// <summary>
	/// Holds a sequence of string components, specifying the hierarchical name of a
	/// category.
	/// </summary>
	/// <remarks>
	/// Holds a sequence of string components, specifying the hierarchical name of a
	/// category.
	/// </remarks>
	/// <lucene.internal></lucene.internal>
	public class FacetLabel : Comparable<Lucene.Net.Facet.Taxonomy.FacetLabel>
	{
		/// <summary>
		/// The maximum number of characters a
		/// <see cref="FacetLabel">FacetLabel</see>
		/// can have.
		/// </summary>
		public const int MAX_CATEGORY_PATH_LENGTH = (ByteBlockPool.BYTE_BLOCK_SIZE - 2) /
			 4;

		/// <summary>
		/// The components of this
		/// <see cref="FacetLabel">FacetLabel</see>
		/// . Note that this array may be
		/// shared with other
		/// <see cref="FacetLabel">FacetLabel</see>
		/// instances, e.g. as a result of
		/// <see cref="Subpath(int)">Subpath(int)</see>
		/// , therefore you should traverse the array up to
		/// <see cref="length">length</see>
		/// for this path's components.
		/// </summary>
		public readonly string[] components;

		/// <summary>
		/// The number of components of this
		/// <see cref="FacetLabel">FacetLabel</see>
		/// .
		/// </summary>
		public readonly int length;

		private FacetLabel(Lucene.Net.Facet.Taxonomy.FacetLabel copyFrom, int prefixLen
			)
		{
			// javadocs
			// javadocs
			// Used by subpath
			// while the code which calls this method is safe, at some point a test
			// tripped on AIOOBE in toString, but we failed to reproduce. adding the
			// assert as a safety check.
			//HM:revisit
			this.components = copyFrom.components;
			length = prefixLen;
		}

		/// <summary>Construct from the given path components.</summary>
		/// <remarks>Construct from the given path components.</remarks>
		public FacetLabel(params string[] components)
		{
			this.components = components;
			length = components.Length;
			CheckComponents();
		}

		/// <summary>Construct from the dimension plus the given path components.</summary>
		/// <remarks>Construct from the dimension plus the given path components.</remarks>
		public FacetLabel(string dim, string[] path)
		{
			components = new string[1 + path.Length];
			components[0] = dim;
			System.Array.Copy(path, 0, components, 1, path.Length);
			length = components.Length;
			CheckComponents();
		}

		private void CheckComponents()
		{
			long len = 0;
			foreach (string comp in components)
			{
				if (comp == null || comp.IsEmpty())
				{
					throw new ArgumentException("empty or null components not allowed: " + Arrays.ToString
						(components));
				}
				len += comp.Length;
			}
			len += components.Length - 1;
			// add separators
			if (len > MAX_CATEGORY_PATH_LENGTH)
			{
				throw new ArgumentException("category path exceeds maximum allowed path length: max="
					 + MAX_CATEGORY_PATH_LENGTH + " len=" + len + " path=" + Sharpen.Runtime.Substring
					(Arrays.ToString(components), 0, 30) + "...");
			}
		}

		/// <summary>
		/// Compares this path with another
		/// <see cref="FacetLabel">FacetLabel</see>
		/// for lexicographic
		/// order.
		/// </summary>
		public virtual int CompareTo(Lucene.Net.Facet.Taxonomy.FacetLabel other)
		{
			int len = length < other.length ? length : other.length;
			for (int i = 0; i < len; i++, j++)
			{
				int cmp = Sharpen.Runtime.CompareOrdinal(components[i], other.components[j]);
				if (cmp < 0)
				{
					return -1;
				}
				// this is 'before'
				if (cmp > 0)
				{
					return 1;
				}
			}
			// this is 'after'
			// one is a prefix of the other
			return length - other.length;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Lucene.Net.Facet.Taxonomy.FacetLabel))
			{
				return false;
			}
			Lucene.Net.Facet.Taxonomy.FacetLabel other = (Lucene.Net.Facet.Taxonomy.FacetLabel
				)obj;
			if (length != other.length)
			{
				return false;
			}
			// not same length, cannot be equal
			// CategoryPaths are more likely to differ at the last components, so start
			// from last-first
			for (int i = length - 1; i >= 0; i--)
			{
				if (!components[i].Equals(other.components[i]))
				{
					return false;
				}
			}
			return true;
		}

		public override int GetHashCode()
		{
			if (length == 0)
			{
				return 0;
			}
			int hash = length;
			for (int i = 0; i < length; i++)
			{
				hash = hash * 31 + components[i].GetHashCode();
			}
			return hash;
		}

		/// <summary>Calculate a 64-bit hash function for this path.</summary>
		/// <remarks>
		/// Calculate a 64-bit hash function for this path.  This
		/// is necessary for
		/// <see cref="Lucene.Net.Facet.Taxonomy.Writercache.NameHashIntCacheLRU">Lucene.Net.Facet.Taxonomy.Writercache.NameHashIntCacheLRU
		/// 	</see>
		/// (the
		/// default cache impl for
		/// <see cref="Lucene.Net.Facet.Taxonomy.Writercache.LruTaxonomyWriterCache">Lucene.Net.Facet.Taxonomy.Writercache.LruTaxonomyWriterCache
		/// 	</see>
		/// ) to reduce the chance of
		/// "silent but deadly" collisions.
		/// </remarks>
		public virtual long LongHashCode()
		{
			if (length == 0)
			{
				return 0;
			}
			long hash = length;
			for (int i = 0; i < length; i++)
			{
				hash = hash * 65599 + components[i].GetHashCode();
			}
			return hash;
		}

		/// <summary>
		/// Returns a sub-path of this path up to
		/// <code>length</code>
		/// components.
		/// </summary>
		public virtual Lucene.Net.Facet.Taxonomy.FacetLabel Subpath(int length)
		{
			if (length >= this.length || length < 0)
			{
				return this;
			}
			else
			{
				return new Lucene.Net.Facet.Taxonomy.FacetLabel(this, length);
			}
		}

		/// <summary>Returns a string representation of the path.</summary>
		/// <remarks>Returns a string representation of the path.</remarks>
		public override string ToString()
		{
			if (length == 0)
			{
				return "FacetLabel: []";
			}
			string[] parts = new string[length];
			System.Array.Copy(components, 0, parts, 0, length);
			return "FacetLabel: " + Arrays.ToString(parts);
		}
	}
}
