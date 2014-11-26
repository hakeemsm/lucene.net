/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using Sharpen;

namespace Lucene.Net.Replicator
{
	/// <summary>
	/// Exception indicating that a revision update session was expired due to lack
	/// of activity.
	/// </summary>
	/// <remarks>
	/// Exception indicating that a revision update session was expired due to lack
	/// of activity.
	/// </remarks>
	/// <seealso cref="LocalReplicator.DEFAULT_SESSION_EXPIRATION_THRESHOLD">LocalReplicator.DEFAULT_SESSION_EXPIRATION_THRESHOLD
	/// 	</seealso>
	/// <seealso cref="LocalReplicator.SetExpirationThreshold(long)">LocalReplicator.SetExpirationThreshold(long)
	/// 	</seealso>
	/// <lucene.experimental></lucene.experimental>
	[System.Serializable]
	public class SessionExpiredException : IOException
	{
		/// <seealso cref="System.IO.IOException.IOException(string, System.Exception)">System.IO.IOException.IOException(string, System.Exception)
		/// 	</seealso>
		public SessionExpiredException(string message, Exception cause) : base(message, cause
			)
		{
		}

		/// <seealso cref="System.IO.IOException.IOException(string)">System.IO.IOException.IOException(string)
		/// 	</seealso>
		public SessionExpiredException(string message) : base(message)
		{
		}

		/// <seealso cref="System.IO.IOException.IOException(System.Exception)">System.IO.IOException.IOException(System.Exception)
		/// 	</seealso>
		public SessionExpiredException(Exception cause) : base(cause)
		{
		}
	}
}
