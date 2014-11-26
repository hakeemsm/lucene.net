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
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Replicator
{
	/// <summary>
	/// A client which monitors and obtains new revisions from a
	/// <see cref="Replicator">Replicator</see>
	/// .
	/// It can be used to either periodically check for updates by invoking
	/// <see cref="StartUpdateThread(long, string)">StartUpdateThread(long, string)</see>
	/// , or manually by calling
	/// <see cref="UpdateNow()">UpdateNow()</see>
	/// .
	/// <p>
	/// Whenever a new revision is available, the
	/// <see cref="RequiredFiles(System.Collections.Generic.IDictionary{K, V})">RequiredFiles(System.Collections.Generic.IDictionary&lt;K, V&gt;)
	/// 	</see>
	/// are
	/// copied to the
	/// <see cref="Lucene.Net.Store.Directory">Lucene.Net.Store.Directory</see>
	/// specified by
	/// <see cref="PerSessionDirectoryFactory">PerSessionDirectoryFactory</see>
	/// and
	/// a handler is notified.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class ReplicationClient : IDisposable
	{
		private class ReplicationThread : Sharpen.Thread
		{
			private readonly long interval;

			internal readonly CountDownLatch stop = new CountDownLatch(1);

			public ReplicationThread(ReplicationClient _enclosing, long interval)
			{
				this._enclosing = _enclosing;
				// client uses this to stop us
				this.interval = interval;
			}

			public override void Run()
			{
				while (true)
				{
					long time = Runtime.CurrentTimeMillis();
					this._enclosing.updateLock.Lock();
					try
					{
						this._enclosing.DoUpdate();
					}
					catch (Exception t)
					{
						this._enclosing.HandleUpdateException(t);
					}
					finally
					{
						this._enclosing.updateLock.Unlock();
					}
					time = Runtime.CurrentTimeMillis() - time;
					// adjust timeout to compensate the time spent doing the replication.
					long timeout = this.interval - time;
					if (timeout > 0)
					{
						try
						{
							// this will return immediately if we were ordered to stop (count=0)
							// or the timeout has elapsed. if it returns true, it means count=0,
							// so terminate.
							if (this.stop.Await(timeout, TimeUnit.MILLISECONDS))
							{
								return;
							}
						}
						catch (Exception e)
						{
							// if we were interruted, somebody wants to terminate us, so just
							// throw the exception further.
							Sharpen.Thread.CurrentThread().Interrupt();
							throw new ThreadInterruptedException(e);
						}
					}
				}
			}

			private readonly ReplicationClient _enclosing;
		}

		/// <summary>Handler for revisions obtained by the client.</summary>
		/// <remarks>Handler for revisions obtained by the client.</remarks>
		public interface ReplicationHandler
		{
			/// <summary>Returns the current revision files held by the handler.</summary>
			/// <remarks>Returns the current revision files held by the handler.</remarks>
			IDictionary<string, IList<RevisionFile>> CurrentRevisionFiles();

			/// <summary>Returns the current revision version held by the handler.</summary>
			/// <remarks>Returns the current revision version held by the handler.</remarks>
			string CurrentVersion();

			/// <summary>Called when a new revision was obtained and is available (i.e.</summary>
			/// <remarks>
			/// Called when a new revision was obtained and is available (i.e. all needed
			/// files were successfully copied).
			/// </remarks>
			/// <param name="version">
			/// the version of the
			/// <see cref="Revision">Revision</see>
			/// that was copied
			/// </param>
			/// <param name="revisionFiles">
			/// the files contained by this
			/// <see cref="Revision">Revision</see>
			/// </param>
			/// <param name="copiedFiles">the files that were actually copied</param>
			/// <param name="sourceDirectory">
			/// a mapping from a source of files to the
			/// <see cref="Lucene.Net.Store.Directory">Lucene.Net.Store.Directory</see>
			/// they
			/// were copied into
			/// </param>
			/// <exception cref="System.IO.IOException"></exception>
			void RevisionReady(string version, IDictionary<string, IList<RevisionFile>> revisionFiles
				, IDictionary<string, IList<string>> copiedFiles, IDictionary<string, Directory>
				 sourceDirectory);
		}

		/// <summary>
		/// Resolves a session and source into a
		/// <see cref="Lucene.Net.Store.Directory">Lucene.Net.Store.Directory</see>
		/// to use for copying
		/// the session files to.
		/// </summary>
		public interface SourceDirectoryFactory
		{
			/// <summary>Called to denote that the replication actions for this session were finished and the directory is no longer needed.
			/// 	</summary>
			/// <remarks>Called to denote that the replication actions for this session were finished and the directory is no longer needed.
			/// 	</remarks>
			/// <exception cref="System.IO.IOException"></exception>
			void CleanupSession(string sessionID);

			/// <summary>
			/// Returns the
			/// <see cref="Lucene.Net.Store.Directory">Lucene.Net.Store.Directory</see>
			/// to use for the given session and source.
			/// Implementations may e.g. return different directories for different
			/// sessions, or the same directory for all sessions. In that case, it is
			/// advised to clean the directory before it is used for a new session.
			/// </summary>
			/// <seealso cref="CleanupSession(string)">CleanupSession(string)</seealso>
			/// <exception cref="System.IO.IOException"></exception>
			Directory GetDirectory(string sessionID, string source);
		}

		/// <summary>
		/// The component name to use with
		/// <see cref="Lucene.Net.Util.InfoStream.IsEnabled(string)">Lucene.Net.Util.InfoStream.IsEnabled(string)
		/// 	</see>
		/// .
		/// </summary>
		public static readonly string INFO_STREAM_COMPONENT = "ReplicationThread";

		private readonly Lucene.Net.Replicator.Replicator replicator;

		private readonly ReplicationClient.ReplicationHandler handler;

		private readonly ReplicationClient.SourceDirectoryFactory factory;

		private readonly byte[] copyBuffer = new byte[16384];

		private readonly Lock updateLock = new ReentrantLock();

		private volatile ReplicationClient.ReplicationThread updateThread;

		private volatile bool closed = false;

		private volatile InfoStream infoStream = InfoStream.GetDefault();

		/// <summary>Constructor.</summary>
		/// <remarks>Constructor.</remarks>
		/// <param name="replicator">
		/// the
		/// <see cref="Replicator">Replicator</see>
		/// used for checking for updates
		/// </param>
		/// <param name="handler">notified when new revisions are ready</param>
		/// <param name="factory">
		/// returns a
		/// <see cref="Lucene.Net.Store.Directory">Lucene.Net.Store.Directory</see>
		/// for a given source and session
		/// </param>
		public ReplicationClient(Lucene.Net.Replicator.Replicator replicator, ReplicationClient.ReplicationHandler
			 handler, ReplicationClient.SourceDirectoryFactory factory)
		{
			this.replicator = replicator;
			this.handler = handler;
			this.factory = factory;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CopyBytes(IndexOutput @out, InputStream @in)
		{
			int numBytes;
			while ((numBytes = @in.Read(copyBuffer)) > 0)
			{
				@out.WriteBytes(copyBuffer, 0, numBytes);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void DoUpdate()
		{
			SessionToken session = null;
			IDictionary<string, Directory> sourceDirectory = new Dictionary<string, Directory
				>();
			IDictionary<string, IList<string>> copiedFiles = new Dictionary<string, IList<string
				>>();
			bool notify = false;
			try
			{
				string version = handler.CurrentVersion();
				session = replicator.CheckForUpdate(version);
				if (infoStream.IsEnabled(INFO_STREAM_COMPONENT))
				{
					infoStream.Message(INFO_STREAM_COMPONENT, "doUpdate(): handlerVersion=" + version
						 + " session=" + session);
				}
				if (session == null)
				{
					// already up to date
					return;
				}
				IDictionary<string, IList<RevisionFile>> requiredFiles = RequiredFiles(session.sourceFiles
					);
				if (infoStream.IsEnabled(INFO_STREAM_COMPONENT))
				{
					infoStream.Message(INFO_STREAM_COMPONENT, "doUpdate(): requiredFiles=" + requiredFiles
						);
				}
				foreach (KeyValuePair<string, IList<RevisionFile>> e in requiredFiles.EntrySet())
				{
					string source = e.Key;
					Directory dir = factory.GetDirectory(session.id, source);
					sourceDirectory.Put(source, dir);
					IList<string> cpFiles = new AList<string>();
					copiedFiles.Put(source, cpFiles);
					foreach (RevisionFile file in e.Value)
					{
						if (closed)
						{
							// if we're closed, abort file copy
							if (infoStream.IsEnabled(INFO_STREAM_COMPONENT))
							{
								infoStream.Message(INFO_STREAM_COMPONENT, "doUpdate(): detected client was closed); abort file copy"
									);
							}
							return;
						}
						InputStream @in = null;
						IndexOutput @out = null;
						try
						{
							@in = replicator.ObtainFile(session.id, source, file.fileName);
							@out = dir.CreateOutput(file.fileName, IOContext.DEFAULT);
							CopyBytes(@out, @in);
							cpFiles.AddItem(file.fileName);
						}
						finally
						{
							// TODO add some validation, on size / checksum
							IOUtils.Close(@in, @out);
						}
					}
				}
				// only notify if all required files were successfully obtained.
				notify = true;
			}
			finally
			{
				if (session != null)
				{
					try
					{
						replicator.Release(session.id);
					}
					finally
					{
						if (!notify)
						{
							// cleanup after ourselves
							IOUtils.Close(sourceDirectory.Values);
							factory.CleanupSession(session.id);
						}
					}
				}
			}
			// notify outside the try-finally above, so the session is released sooner.
			// the handler may take time to finish acting on the copied files, but the
			// session itself is no longer needed.
			try
			{
				if (notify && !closed)
				{
					// no use to notify if we are closed already
					handler.RevisionReady(session.version, session.sourceFiles, copiedFiles, sourceDirectory
						);
				}
			}
			finally
			{
				IOUtils.Close(sourceDirectory.Values);
				if (session != null)
				{
					factory.CleanupSession(session.id);
				}
			}
		}

		/// <summary>
		/// Throws
		/// <see cref="Lucene.Net.Store.AlreadyClosedException">Lucene.Net.Store.AlreadyClosedException
		/// 	</see>
		/// if the client has already been closed.
		/// </summary>
		protected internal void EnsureOpen()
		{
			if (closed)
			{
				throw new AlreadyClosedException("this update client has already been closed");
			}
		}

		/// <summary>Called when an exception is hit by the replication thread.</summary>
		/// <remarks>
		/// Called when an exception is hit by the replication thread. The default
		/// implementation prints the full stacktrace to the
		/// <see cref="Lucene.Net.Util.InfoStream">Lucene.Net.Util.InfoStream</see>
		/// set in
		/// <see cref="SetInfoStream(Lucene.Net.Util.InfoStream)">SetInfoStream(Lucene.Net.Util.InfoStream)
		/// 	</see>
		/// , or the
		/// <see cref="Lucene.Net.Util.InfoStream.GetDefault()">default</see>
		/// one. You can override to log the exception elswhere.
		/// <p>
		/// <b>NOTE:</b> if you override this method to throw the exception further,
		/// the replication thread will be terminated. The only way to restart it is to
		/// call
		/// <see cref="StopUpdateThread()">StopUpdateThread()</see>
		/// followed by
		/// <see cref="StartUpdateThread(long, string)">StartUpdateThread(long, string)</see>
		/// .
		/// </remarks>
		protected internal virtual void HandleUpdateException(Exception t)
		{
			StringWriter sw = new StringWriter();
			Sharpen.Runtime.PrintStackTrace(t, new PrintWriter(sw));
			if (infoStream.IsEnabled(INFO_STREAM_COMPONENT))
			{
				infoStream.Message(INFO_STREAM_COMPONENT, "an error occurred during revision update: "
					 + sw.ToString());
			}
		}

		/// <summary>Returns the files required for replication.</summary>
		/// <remarks>
		/// Returns the files required for replication. By default, this method returns
		/// all files that exist in the new revision, but not in the handler.
		/// </remarks>
		protected internal virtual IDictionary<string, IList<RevisionFile>> RequiredFiles
			(IDictionary<string, IList<RevisionFile>> newRevisionFiles)
		{
			IDictionary<string, IList<RevisionFile>> handlerRevisionFiles = handler.CurrentRevisionFiles
				();
			if (handlerRevisionFiles == null)
			{
				return newRevisionFiles;
			}
			IDictionary<string, IList<RevisionFile>> requiredFiles = new Dictionary<string, IList
				<RevisionFile>>();
			foreach (KeyValuePair<string, IList<RevisionFile>> e in handlerRevisionFiles.EntrySet
				())
			{
				// put the handler files in a Set, for faster contains() checks later
				ICollection<string> handlerFiles = new HashSet<string>();
				foreach (RevisionFile file in e.Value)
				{
					handlerFiles.AddItem(file.fileName);
				}
				// make sure to preserve revisionFiles order
				AList<RevisionFile> res = new AList<RevisionFile>();
				string source = e.Key;
				//HM:revisit
				//assert newRevisionFiles.containsKey(source) : "source not found in newRevisionFiles: " + newRevisionFiles;
				foreach (RevisionFile file_1 in newRevisionFiles.Get(source))
				{
					if (!handlerFiles.Contains(file_1.fileName))
					{
						res.AddItem(file_1);
					}
				}
				requiredFiles.Put(source, res);
			}
			return requiredFiles;
		}

		public virtual void Close()
		{
			lock (this)
			{
				if (!closed)
				{
					StopUpdateThread();
					closed = true;
				}
			}
		}

		/// <summary>Start the update thread with the specified interval in milliseconds.</summary>
		/// <remarks>
		/// Start the update thread with the specified interval in milliseconds. For
		/// debugging purposes, you can optionally set the name to set on
		/// <see cref="Sharpen.Thread.SetName(string)">Sharpen.Thread.SetName(string)</see>
		/// . If you pass
		/// <code>null</code>
		/// , a default name
		/// will be set.
		/// </remarks>
		/// <exception cref="System.InvalidOperationException">if the thread has already been started
		/// 	</exception>
		public virtual void StartUpdateThread(long intervalMillis, string threadName)
		{
			lock (this)
			{
				EnsureOpen();
				if (updateThread != null && updateThread.IsAlive())
				{
					throw new InvalidOperationException("cannot start an update thread when one is running, must first call 'stopUpdateThread()'"
						);
				}
				threadName = threadName == null ? INFO_STREAM_COMPONENT : "ReplicationThread-" + 
					threadName;
				updateThread = new ReplicationClient.ReplicationThread(this, intervalMillis);
				updateThread.SetName(threadName);
				updateThread.Start();
			}
		}

		// we rely on isAlive to return true in isUpdateThreadAlive, assert to be on the safe side
		//HM:revisit
		//assert updateThread.isAlive() : "updateThread started but not alive?";
		/// <summary>Stop the update thread.</summary>
		/// <remarks>
		/// Stop the update thread. If the update thread is not running, silently does
		/// nothing. This method returns after the update thread has stopped.
		/// </remarks>
		public virtual void StopUpdateThread()
		{
			lock (this)
			{
				if (updateThread != null)
				{
					// this will trigger the thread to terminate if it awaits the lock.
					// otherwise, if it's in the middle of replication, we wait for it to
					// stop.
					updateThread.stop.CountDown();
					try
					{
						updateThread.Join();
					}
					catch (Exception e)
					{
						Sharpen.Thread.CurrentThread().Interrupt();
						throw new ThreadInterruptedException(e);
					}
					updateThread = null;
				}
			}
		}

		/// <summary>Returns true if the update thread is alive.</summary>
		/// <remarks>
		/// Returns true if the update thread is alive. The update thread is alive if
		/// it has been
		/// <see cref="StartUpdateThread(long, string)">started</see>
		/// and not
		/// <see cref="StopUpdateThread()">stopped</see>
		/// , as well as didn't hit an error which
		/// caused it to terminate (i.e.
		/// <see cref="HandleUpdateException(System.Exception)">HandleUpdateException(System.Exception)
		/// 	</see>
		/// threw the exception further).
		/// </remarks>
		public virtual bool IsUpdateThreadAlive()
		{
			lock (this)
			{
				return updateThread != null && updateThread.IsAlive();
			}
		}

		public override string ToString()
		{
			string res = "ReplicationClient";
			if (updateThread != null)
			{
				res += " (" + updateThread.GetName() + ")";
			}
			return res;
		}

		/// <summary>
		/// Executes the update operation immediately, irregardess if an update thread
		/// is running or not.
		/// </summary>
		/// <remarks>
		/// Executes the update operation immediately, irregardess if an update thread
		/// is running or not.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void UpdateNow()
		{
			EnsureOpen();
			updateLock.Lock();
			try
			{
				DoUpdate();
			}
			finally
			{
				updateLock.Unlock();
			}
		}

		/// <summary>
		/// Sets the
		/// <see cref="Lucene.Net.Util.InfoStream">Lucene.Net.Util.InfoStream</see>
		/// to use for logging messages.
		/// </summary>
		public virtual void SetInfoStream(InfoStream infoStream)
		{
			if (infoStream == null)
			{
				infoStream = InfoStream.NO_OUTPUT;
			}
			this.infoStream = infoStream;
		}
	}
}
