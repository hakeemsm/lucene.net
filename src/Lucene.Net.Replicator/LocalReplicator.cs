/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Replicator;
using Lucene.Net.Store;
using Sharpen;

namespace Lucene.Net.Replicator
{
	/// <summary>
	/// A
	/// <see cref="Replicator">Replicator</see>
	/// implementation for use by the side that publishes
	/// <see cref="Revision">Revision</see>
	/// s, as well for clients to
	/// <see cref="CheckForUpdate(string)">check for updates</see>
	/// . When a client needs to be updated, it is returned a
	/// <see cref="SessionToken">SessionToken</see>
	/// through which it can
	/// <see cref="ObtainFile(string, string, string)">obtain</see>
	/// the files of that
	/// revision. As long as a revision is being replicated, this replicator
	/// guarantees that it will not be
	/// <see cref="Revision.Release()">released</see>
	/// .
	/// <p>
	/// Replication sessions expire by default after
	/// <see cref="DEFAULT_SESSION_EXPIRATION_THRESHOLD">DEFAULT_SESSION_EXPIRATION_THRESHOLD
	/// 	</see>
	/// , and the threshold can be
	/// configured through
	/// <see cref="SetExpirationThreshold(long)">SetExpirationThreshold(long)</see>
	/// .
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class LocalReplicator : Lucene.Net.Replicator.Replicator
	{
		private class RefCountedRevision
		{
			private readonly AtomicInteger refCount = new AtomicInteger(1);

			public readonly Revision revision;

			public RefCountedRevision(Revision revision)
			{
				this.revision = revision;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual void DecRef()
			{
				if (refCount.Get() <= 0)
				{
					throw new InvalidOperationException("this revision is already released");
				}
				int rc = refCount.DecrementAndGet();
				if (rc == 0)
				{
					bool success = false;
					try
					{
						revision.Release();
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

			public virtual void IncRef()
			{
				refCount.IncrementAndGet();
			}
		}

		private class ReplicationSession
		{
			public readonly SessionToken session;

			public readonly LocalReplicator.RefCountedRevision revision;

			private volatile long lastAccessTime;

			internal ReplicationSession(SessionToken session, LocalReplicator.RefCountedRevision
				 revision)
			{
				this.session = session;
				this.revision = revision;
				lastAccessTime = Runtime.CurrentTimeMillis();
			}

			internal virtual bool IsExpired(long expirationThreshold)
			{
				return lastAccessTime < (Runtime.CurrentTimeMillis() - expirationThreshold);
			}

			internal virtual void MarkAccessed()
			{
				lastAccessTime = Runtime.CurrentTimeMillis();
			}
		}

		/// <summary>Threshold for expiring inactive sessions.</summary>
		/// <remarks>Threshold for expiring inactive sessions. Defaults to 30 minutes.</remarks>
		public const long DEFAULT_SESSION_EXPIRATION_THRESHOLD = 1000 * 60 * 30;

		private long expirationThresholdMilllis = LocalReplicator.DEFAULT_SESSION_EXPIRATION_THRESHOLD;

		private volatile LocalReplicator.RefCountedRevision currentRevision;

		private volatile bool closed = false;

		private readonly AtomicInteger sessionToken = new AtomicInteger(0);

		private readonly IDictionary<string, LocalReplicator.ReplicationSession> sessions
			 = new Dictionary<string, LocalReplicator.ReplicationSession>();

		/// <exception cref="System.IO.IOException"></exception>
		private void CheckExpiredSessions()
		{
			// make a "to-delete" list so we don't risk deleting from the map while iterating it
			AList<LocalReplicator.ReplicationSession> toExpire = new AList<LocalReplicator.ReplicationSession
				>();
			foreach (LocalReplicator.ReplicationSession token in sessions.Values)
			{
				if (token.IsExpired(expirationThresholdMilllis))
				{
					toExpire.AddItem(token);
				}
			}
			foreach (LocalReplicator.ReplicationSession token_1 in toExpire)
			{
				ReleaseSession(token_1.session.id);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ReleaseSession(string sessionID)
		{
			LocalReplicator.ReplicationSession session = Sharpen.Collections.Remove(sessions, 
				sessionID);
			// if we're called concurrently by close() and release(), could be that one
			// thread beats the other to release the session.
			if (session != null)
			{
				session.revision.DecRef();
			}
		}

		/// <summary>
		/// Ensure that replicator is still open, or throw
		/// <see cref="Lucene.Net.Store.AlreadyClosedException">Lucene.Net.Store.AlreadyClosedException
		/// 	</see>
		/// otherwise.
		/// </summary>
		protected internal void EnsureOpen()
		{
			lock (this)
			{
				if (closed)
				{
					throw new AlreadyClosedException("This replicator has already been closed");
				}
			}
		}

		public virtual SessionToken CheckForUpdate(string currentVersion)
		{
			lock (this)
			{
				EnsureOpen();
				if (currentRevision == null)
				{
					// no published revisions yet
					return null;
				}
				if (currentVersion != null && currentRevision.revision.CompareTo(currentVersion) 
					<= 0)
				{
					// currentVersion is newer or equal to latest published revision
					return null;
				}
				// currentVersion is either null or older than latest published revision
				currentRevision.IncRef();
				string sessionID = Sharpen.Extensions.ToString(sessionToken.IncrementAndGet());
				SessionToken sessionToken = new SessionToken(sessionID, currentRevision.revision);
				LocalReplicator.ReplicationSession timedSessionToken = new LocalReplicator.ReplicationSession
					(sessionToken, currentRevision);
				sessions.Put(sessionID, timedSessionToken);
				return sessionToken;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Close()
		{
			lock (this)
			{
				if (!closed)
				{
					// release all managed revisions
					foreach (LocalReplicator.ReplicationSession session in sessions.Values)
					{
						session.revision.DecRef();
					}
					sessions.Clear();
					closed = true;
				}
			}
		}

		/// <summary>Returns the expiration threshold.</summary>
		/// <remarks>Returns the expiration threshold.</remarks>
		/// <seealso cref="SetExpirationThreshold(long)">SetExpirationThreshold(long)</seealso>
		public virtual long GetExpirationThreshold()
		{
			return expirationThresholdMilllis;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual InputStream ObtainFile(string sessionID, string source, string fileName
			)
		{
			lock (this)
			{
				EnsureOpen();
				LocalReplicator.ReplicationSession session = sessions.Get(sessionID);
				if (session != null && session.IsExpired(expirationThresholdMilllis))
				{
					ReleaseSession(sessionID);
					session = null;
				}
				// session either previously expired, or we just expired it
				if (session == null)
				{
					throw new SessionExpiredException("session (" + sessionID + ") expired while obtaining file: source="
						 + source + " file=" + fileName);
				}
				sessions.Get(sessionID).MarkAccessed();
				return session.revision.revision.Open(source, fileName);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Publish(Revision revision)
		{
			lock (this)
			{
				EnsureOpen();
				if (currentRevision != null)
				{
					int compare = revision.CompareTo(currentRevision.revision);
					if (compare == 0)
					{
						// same revision published again, ignore but release it
						revision.Release();
						return;
					}
					if (compare < 0)
					{
						revision.Release();
						throw new ArgumentException("Cannot publish an older revision: rev=" + revision +
							 " current=" + currentRevision);
					}
				}
				// swap revisions
				LocalReplicator.RefCountedRevision oldRevision = currentRevision;
				currentRevision = new LocalReplicator.RefCountedRevision(revision);
				if (oldRevision != null)
				{
					oldRevision.DecRef();
				}
				// check for expired sessions
				CheckExpiredSessions();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Release(string sessionID)
		{
			lock (this)
			{
				EnsureOpen();
				ReleaseSession(sessionID);
			}
		}

		/// <summary>
		/// Modify session expiration time - if a replication session is inactive that
		/// long it is automatically expired, and further attempts to operate within
		/// this session will throw a
		/// <see cref="SessionExpiredException">SessionExpiredException</see>
		/// .
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void SetExpirationThreshold(long expirationThreshold)
		{
			lock (this)
			{
				EnsureOpen();
				this.expirationThresholdMilllis = expirationThreshold;
				CheckExpiredSessions();
			}
		}
	}
}
