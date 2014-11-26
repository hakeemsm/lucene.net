/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.IO;
using Lucene.Net.Replicator;
using Sharpen;

namespace Lucene.Net.Replicator
{
	/// <summary>
	/// A revision comprises lists of files that come from different sources and need
	/// to be replicated together to e.g.
	/// </summary>
	/// <remarks>
	/// A revision comprises lists of files that come from different sources and need
	/// to be replicated together to e.g. guarantee that all resources are in sync.
	/// In most cases an application will replicate a single index, and so the
	/// revision will contain files from a single source. However, some applications
	/// may require to treat a collection of indexes as a single entity so that the
	/// files from all sources are replicated together, to guarantee consistency
	/// beween them. For example, an application which indexes facets will need to
	/// replicate both the search and taxonomy indexes together, to guarantee that
	/// they match at the client side.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public interface Revision : Comparable<Revision>
	{
		/// <summary>Compares the revision to the given version string.</summary>
		/// <remarks>
		/// Compares the revision to the given version string. Behaves like
		/// <see cref="System.IComparable{T}.CompareTo(object)">System.IComparable&lt;T&gt;.CompareTo(object)
		/// 	</see>
		/// .
		/// </remarks>
		int CompareTo(string version);

		/// <summary>Returns a string representation of the version of this revision.</summary>
		/// <remarks>
		/// Returns a string representation of the version of this revision. The
		/// version is used by
		/// <see cref="CompareTo(string)">CompareTo(string)</see>
		/// as well as to
		/// serialize/deserialize revision information. Therefore it must be self
		/// descriptive as well as be able to identify one revision from another.
		/// </remarks>
		string GetVersion();

		/// <summary>
		/// Returns the files that comprise this revision, as a mapping from a source
		/// to a list of files.
		/// </summary>
		/// <remarks>
		/// Returns the files that comprise this revision, as a mapping from a source
		/// to a list of files.
		/// </remarks>
		IDictionary<string, IList<RevisionFile>> GetSourceFiles();

		/// <summary>
		/// Returns an
		/// <see cref="Lucene.Net.Store.IndexInput">Lucene.Net.Store.IndexInput
		/// 	</see>
		/// for the given fileName and source. It is the
		/// caller's respnsibility to close the
		/// <see cref="Lucene.Net.Store.IndexInput">Lucene.Net.Store.IndexInput
		/// 	</see>
		/// when it has been
		/// consumed.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		InputStream Open(string source, string fileName);

		/// <summary>Called when this revision can be safely released, i.e.</summary>
		/// <remarks>
		/// Called when this revision can be safely released, i.e. where there are no
		/// more references to it.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		void Release();
	}
}
