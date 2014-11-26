/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Store;
using Sharpen;

namespace Lucene.Net.Facet.Taxonomy
{
	/// <summary>
	/// TaxonomyReader is the read-only interface with which the faceted-search
	/// library uses the taxonomy during search time.
	/// </summary>
	/// <remarks>
	/// TaxonomyReader is the read-only interface with which the faceted-search
	/// library uses the taxonomy during search time.
	/// <P>
	/// A TaxonomyReader holds a list of categories. Each category has a serial
	/// number which we call an "ordinal", and a hierarchical "path" name:
	/// <UL>
	/// <LI>
	/// The ordinal is an integer that starts at 0 for the first category (which is
	/// always the root category), and grows contiguously as more categories are
	/// added; Note that once a category is added, it can never be deleted.
	/// <LI>
	/// The path is a CategoryPath object specifying the category's position in the
	/// hierarchy.
	/// </UL>
	/// <B>Notes about concurrent access to the taxonomy:</B>
	/// <P>
	/// An implementation must allow multiple readers to be active concurrently
	/// with a single writer. Readers follow so-called "point in time" semantics,
	/// i.e., a TaxonomyReader object will only see taxonomy entries which were
	/// available at the time it was created. What the writer writes is only
	/// available to (new) readers after the writer's commit() is called.
	/// <P>
	/// In faceted search, two separate indices are used: the main Lucene index,
	/// and the taxonomy. Because the main index refers to the categories listed
	/// in the taxonomy, it is important to open the taxonomy *after* opening the
	/// main index, and it is also necessary to reopen() the taxonomy after
	/// reopen()ing the main index.
	/// <P>
	/// This order is important, otherwise it would be possible for the main index
	/// to refer to a category which is not yet visible in the old snapshot of
	/// the taxonomy. Note that it is indeed fine for the the taxonomy to be opened
	/// after the main index - even a long time after. The reason is that once
	/// a category is added to the taxonomy, it can never be changed or deleted,
	/// so there is no danger that a "too new" taxonomy not being consistent with
	/// an older index.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class TaxonomyReader : IDisposable
	{
		/// <summary>An iterator over a category's children.</summary>
		/// <remarks>An iterator over a category's children.</remarks>
		public class ChildrenIterator
		{
			private readonly int[] siblings;

			private int child;

			internal ChildrenIterator(int child, int[] siblings)
			{
				this.siblings = siblings;
				this.child = child;
			}

			/// <summary>
			/// Return the next child ordinal, or
			/// <see cref="TaxonomyReader.INVALID_ORDINAL">TaxonomyReader.INVALID_ORDINAL</see>
			/// if no more children.
			/// </summary>
			public virtual int Next()
			{
				int res = child;
				if (child != TaxonomyReader.INVALID_ORDINAL)
				{
					child = siblings[child];
				}
				return res;
			}
		}

		/// <summary>Sole constructor.</summary>
		/// <remarks>Sole constructor.</remarks>
		public TaxonomyReader()
		{
		}

		/// <summary>
		/// The root category (the category with the empty path) always has the ordinal
		/// 0, to which we give a name ROOT_ORDINAL.
		/// </summary>
		/// <remarks>
		/// The root category (the category with the empty path) always has the ordinal
		/// 0, to which we give a name ROOT_ORDINAL.
		/// <see cref="GetOrdinal(FacetLabel)">GetOrdinal(FacetLabel)</see>
		/// of an empty path will always return
		/// <code>ROOT_ORDINAL</code>
		/// , and
		/// <see cref="GetPath(int)">GetPath(int)</see>
		/// with
		/// <code>ROOT_ORDINAL</code>
		/// will return the empty path.
		/// </remarks>
		public const int ROOT_ORDINAL = 0;

		/// <summary>
		/// Ordinals are always non-negative, so a negative ordinal can be used to
		/// signify an error.
		/// </summary>
		/// <remarks>
		/// Ordinals are always non-negative, so a negative ordinal can be used to
		/// signify an error. Methods here return INVALID_ORDINAL (-1) in this case.
		/// </remarks>
		public const int INVALID_ORDINAL = -1;

		/// <summary>
		/// If the taxonomy has changed since the provided reader was opened, open and
		/// return a new
		/// <see cref="TaxonomyReader">TaxonomyReader</see>
		/// ; else, return
		/// <code>null</code>
		/// . The new
		/// reader, if not
		/// <code>null</code>
		/// , will be the same type of reader as the one
		/// given to this method.
		/// <p>
		/// This method is typically far less costly than opening a fully new
		/// <see cref="TaxonomyReader">TaxonomyReader</see>
		/// as it shares resources with the provided
		/// <see cref="TaxonomyReader">TaxonomyReader</see>
		/// , when possible.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public static T OpenIfChanged<T>(T oldTaxoReader) where T:TaxonomyReader
		{
			T newTaxoReader = (T)oldTaxoReader.DoOpenIfChanged();
			return newTaxoReader != oldTaxoReader;
		}

		private volatile bool closed = false;

		private readonly AtomicInteger refCount = new AtomicInteger(1);

		// set refCount to 1 at start
		/// <summary>
		/// performs the actual task of closing the resources that are used by the
		/// taxonomy reader.
		/// </summary>
		/// <remarks>
		/// performs the actual task of closing the resources that are used by the
		/// taxonomy reader.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal abstract void DoClose();

		/// <summary>
		/// Implements the actual opening of a new
		/// <see cref="TaxonomyReader">TaxonomyReader</see>
		/// instance if
		/// the taxonomy has changed.
		/// </summary>
		/// <seealso cref="OpenIfChanged{T}(TaxonomyReader)">OpenIfChanged&lt;T&gt;(TaxonomyReader)
		/// 	</seealso>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal abstract TaxonomyReader DoOpenIfChanged();

		/// <summary>
		/// Throws
		/// <see cref="Lucene.Net.Store.AlreadyClosedException">Lucene.Net.Store.AlreadyClosedException
		/// 	</see>
		/// if this IndexReader is closed
		/// </summary>
		/// <exception cref="Lucene.Net.Store.AlreadyClosedException"></exception>
		protected internal void EnsureOpen()
		{
			if (GetRefCount() <= 0)
			{
				throw new AlreadyClosedException("this TaxonomyReader is closed");
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public void Close()
		{
			if (!closed)
			{
				lock (this)
				{
					if (!closed)
					{
						DecRef();
						closed = true;
					}
				}
			}
		}

		/// <summary>Expert: decreases the refCount of this TaxonomyReader instance.</summary>
		/// <remarks>
		/// Expert: decreases the refCount of this TaxonomyReader instance. If the
		/// refCount drops to 0 this taxonomy reader is closed.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public void DecRef()
		{
			EnsureOpen();
			int rc = refCount.DecrementAndGet();
			if (rc == 0)
			{
				bool success = false;
				try
				{
					DoClose();
					closed = true;
					success = true;
				}
				finally
				{
					if (!success)
					{
						// Put reference back on failure
						refCount.IncrementAndGet();
					}
				}
			}
			else
			{
				if (rc < 0)
				{
					throw new InvalidOperationException("too many decRef calls: refCount is " + rc + 
						" after decrement");
				}
			}
		}

		/// <summary>
		/// Returns a
		/// <see cref="ParallelTaxonomyArrays">ParallelTaxonomyArrays</see>
		/// object which can be used to
		/// efficiently traverse the taxonomy tree.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract ParallelTaxonomyArrays GetParallelTaxonomyArrays();

		/// <summary>Returns an iterator over the children of the given ordinal.</summary>
		/// <remarks>Returns an iterator over the children of the given ordinal.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual TaxonomyReader.ChildrenIterator GetChildren(int ordinal)
		{
			ParallelTaxonomyArrays arrays = GetParallelTaxonomyArrays();
			int child = ordinal >= 0 ? arrays.Children()[ordinal] : INVALID_ORDINAL;
			return new TaxonomyReader.ChildrenIterator(child, arrays.Siblings());
		}

		/// <summary>Retrieve user committed data.</summary>
		/// <remarks>Retrieve user committed data.</remarks>
		/// <seealso cref="TaxonomyWriter.SetCommitData(System.Collections.Generic.IDictionary{K, V})
		/// 	">TaxonomyWriter.SetCommitData(System.Collections.Generic.IDictionary&lt;K, V&gt;)
		/// 	</seealso>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract IDictionary<string, string> GetCommitUserData();

		/// <summary>Returns the ordinal of the category given as a path.</summary>
		/// <remarks>
		/// Returns the ordinal of the category given as a path. The ordinal is the
		/// category's serial number, an integer which starts with 0 and grows as more
		/// categories are added (note that once a category is added, it can never be
		/// deleted).
		/// </remarks>
		/// <returns>
		/// the category's ordinal or
		/// <see cref="INVALID_ORDINAL">INVALID_ORDINAL</see>
		/// if the category
		/// wasn't foun.
		/// </returns>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract int GetOrdinal(FacetLabel categoryPath);

		/// <summary>Returns ordinal for the dim + path.</summary>
		/// <remarks>Returns ordinal for the dim + path.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual int GetOrdinal(string dim, string[] path)
		{
			string[] fullPath = new string[path.Length + 1];
			fullPath[0] = dim;
			System.Array.Copy(path, 0, fullPath, 1, path.Length);
			return GetOrdinal(new FacetLabel(fullPath));
		}

		/// <summary>Returns the path name of the category with the given ordinal.</summary>
		/// <remarks>Returns the path name of the category with the given ordinal.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract FacetLabel GetPath(int ordinal);

		/// <summary>Returns the current refCount for this taxonomy reader.</summary>
		/// <remarks>Returns the current refCount for this taxonomy reader.</remarks>
		public int GetRefCount()
		{
			return refCount.Get();
		}

		/// <summary>Returns the number of categories in the taxonomy.</summary>
		/// <remarks>
		/// Returns the number of categories in the taxonomy. Note that the number of
		/// categories returned is often slightly higher than the number of categories
		/// inserted into the taxonomy; This is because when a category is added to the
		/// taxonomy, its ancestors are also added automatically (including the root,
		/// which always get ordinal 0).
		/// </remarks>
		public abstract int GetSize();

		/// <summary>Expert: increments the refCount of this TaxonomyReader instance.</summary>
		/// <remarks>
		/// Expert: increments the refCount of this TaxonomyReader instance. RefCounts
		/// can be used to determine when a taxonomy reader can be closed safely, i.e.
		/// as soon as there are no more references. Be sure to always call a
		/// corresponding decRef(), in a finally clause; otherwise the reader may never
		/// be closed.
		/// </remarks>
		public void IncRef()
		{
			EnsureOpen();
			refCount.IncrementAndGet();
		}

		/// <summary>
		/// Expert: increments the refCount of this TaxonomyReader
		/// instance only if it has not been closed yet.
		/// </summary>
		/// <remarks>
		/// Expert: increments the refCount of this TaxonomyReader
		/// instance only if it has not been closed yet.  Returns
		/// true on success.
		/// </remarks>
		public bool TryIncRef()
		{
			int count;
			while ((count = refCount.Get()) > 0)
			{
				if (refCount.CompareAndSet(count, count + 1))
				{
					return true;
				}
			}
			return false;
		}
	}
}
