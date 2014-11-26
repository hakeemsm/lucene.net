/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using Lucene.Net.Replicator;
using Sharpen;

namespace Lucene.Net.Replicator
{
	/// <summary>An interface for replicating files.</summary>
	/// <remarks>
	/// An interface for replicating files. Allows a producer to
	/// <see cref="Publish(Revision)">publish</see>
	/// 
	/// <see cref="Revision">Revision</see>
	/// s and consumers to
	/// <see cref="CheckForUpdate(string)">check for updates</see>
	/// . When a client needs to be
	/// updated, it is given a
	/// <see cref="SessionToken">SessionToken</see>
	/// through which it can
	/// <see cref="ObtainFile(string, string, string)">obtain</see>
	/// the files of that
	/// revision. After the client has finished obtaining all the files, it should
	/// <see cref="Release(string)">release</see>
	/// the given session, so that the files can be
	/// reclaimed if they are not needed anymore.
	/// <p>
	/// A client is always updated to the newest revision available. That is, if a
	/// client is on revision <em>r1</em> and revisions <em>r2</em> and <em>r3</em>
	/// were published, then when the cllient will next check for update, it will
	/// receive <em>r3</em>.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public interface Replicator : IDisposable
	{
		/// <summary>
		/// Publish a new
		/// <see cref="Revision">Revision</see>
		/// for consumption by clients. It is the
		/// caller's responsibility to verify that the revision files exist and can be
		/// read by clients. When the revision is no longer needed, it will be
		/// <see cref="Revision.Release()">released</see>
		/// by the replicator.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		void Publish(Revision revision);

		/// <summary>
		/// Check whether the given version is up-to-date and returns a
		/// <see cref="SessionToken">SessionToken</see>
		/// which can be used for fetching the revision files,
		/// otherwise returns
		/// <code>null</code>
		/// .
		/// <p>
		/// <b>NOTE:</b> when the returned session token is no longer needed, you
		/// should call
		/// <see cref="Release(string)">Release(string)</see>
		/// so that the session resources can be
		/// reclaimed, including the revision files.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		SessionToken CheckForUpdate(string currVersion);

		/// <summary>
		/// Notify that the specified
		/// <see cref="SessionToken">SessionToken</see>
		/// is no longer needed by the
		/// caller.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		void Release(string sessionID);

		/// <summary>
		/// Returns an
		/// <see cref="System.IO.InputStream">System.IO.InputStream</see>
		/// for the requested file and source in the
		/// context of the given
		/// <see cref="SessionToken.id">session</see>
		/// .
		/// <p>
		/// <b>NOTE:</b> it is the caller's responsibility to close the returned
		/// stream.
		/// </summary>
		/// <exception cref="SessionExpiredException">
		/// if the specified session has already
		/// expired
		/// </exception>
		/// <exception cref="System.IO.IOException"></exception>
		InputStream ObtainFile(string sessionID, string source, string fileName);
	}
}
