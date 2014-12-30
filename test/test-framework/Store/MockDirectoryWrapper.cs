/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Store
{
    public class MockDirectoryWrapper : BaseDirectoryWrapper
    {
		internal long maxSize;

		internal long maxUsedSize;

		internal double randomIOExceptionRate;

		internal double randomIOExceptionRateOnOpen;

		internal Random randomState;

		internal bool noDeleteOpenFile = true;

		internal bool assertNoDeleteOpenFile = false;

		internal bool preventDoubleWrite = true;

		internal bool trackDiskUsage = false;

		internal bool wrapLockFactory = true;

		internal bool useSlowOpenClosers = true;

		internal bool allowRandomFileNotFoundException = true;

		internal bool allowReadingFilesStillOpenForWrite = false;

		private ICollection<string> unSyncedFiles;

		private ICollection<string> createdFiles;

		private ICollection<string> openFilesForWrite = new HashSet<string>();

		internal ICollection<string> openLocks = Sharpen.Collections.SynchronizedSet(new 
			HashSet<string>());

		internal volatile bool crashed;

		private ThrottledIndexOutput throttledOutput;

		private MockDirectoryWrapper.Throttling throttling = MockDirectoryWrapper.Throttling
			.SOMETIMES;

		protected internal LockFactory lockFactory;

		internal readonly AtomicInteger inputCloneCount = new AtomicInteger();

		private IDictionary<IDisposable, Exception> openFileHandles = Sharpen.Collections
			.SynchronizedMap(new IdentityHashMap<IDisposable, Exception>());

		private IDictionary<string, int> openFiles;

		private ICollection<string> openFilesDeleted;

		// Max actual bytes used. This is set by MockRAMOutputStream:
		// use this for tracking files for crash.
		// additionally: provides debugging information in case you leave one open
		// NOTE: we cannot initialize the Map here due to the
		// order in which our constructor actually does this
		// member initialization vs when it calls super.  It seems
		// like super is called, then our members are initialized:
		// Only tracked if noDeleteOpenFile is true: if an attempt
		// is made to delete an open file, we enroll it here.
		private void Init()
		{
			lock (this)
			{
				if (openFiles == null)
				{
					openFiles = new Dictionary<string, int>();
					openFilesDeleted = new HashSet<string>();
				}
				if (createdFiles == null)
				{
					createdFiles = new HashSet<string>();
				}
				if (unSyncedFiles == null)
				{
					unSyncedFiles = new HashSet<string>();
				}
			}
		}

		public MockDirectoryWrapper(Random random, Directory delegate_) : base(delegate_)
		{
			// must make a private random since our methods are
			// called from different threads; else test failures may
			// not be reproducible from the original seed
			this.randomState = new Random(random.Next());
			this.throttledOutput = new ThrottledIndexOutput(ThrottledIndexOutput.MBitsToBytes
				(40 + randomState.Next(10)), 5 + randomState.Next(5), null);
			// force wrapping of lockfactory
			this.lockFactory = new MockLockFactoryWrapper(this, delegate_.GetLockFactory());
			Init();
		}

		public virtual int GetInputCloneCount()
		{
			return inputCloneCount.Get();
		}

		public virtual void SetTrackDiskUsage(bool v)
		{
			trackDiskUsage = v;
		}

		/// <summary>
		/// If set to true, we throw an IOException if the same
		/// file is opened by createOutput, ever.
		/// </summary>
		/// <remarks>
		/// If set to true, we throw an IOException if the same
		/// file is opened by createOutput, ever.
		/// </remarks>
		public virtual void SetPreventDoubleWrite(bool value)
		{
			preventDoubleWrite = value;
		}

		/// <summary>
		/// If set to true (the default), when we throw random
		/// IOException on openInput or createOutput, we may
		/// sometimes throw FileNotFoundException or
		/// NoSuchFileException.
		/// </summary>
		/// <remarks>
		/// If set to true (the default), when we throw random
		/// IOException on openInput or createOutput, we may
		/// sometimes throw FileNotFoundException or
		/// NoSuchFileException.
		/// </remarks>
		public virtual void SetAllowRandomFileNotFoundException(bool value)
		{
			allowRandomFileNotFoundException = value;
		}

		/// <summary>
		/// If set to true, you can open an inputstream on a file
		/// that is still open for writes.
		/// </summary>
		/// <remarks>
		/// If set to true, you can open an inputstream on a file
		/// that is still open for writes.
		/// </remarks>
		public virtual void SetAllowReadingFilesStillOpenForWrite(bool value)
		{
			allowReadingFilesStillOpenForWrite = value;
		}

		/// <summary>Enum for controlling hard disk throttling.</summary>
		/// <remarks>
		/// Enum for controlling hard disk throttling.
		/// Set via
		/// <see cref="SetThrottling(Throttling)">SetThrottling(Throttling)</see>
		/// <p>
		/// WARNING: can make tests very slow.
		/// </remarks>
		public enum Throttling
		{
			ALWAYS,
			SOMETIMES,
			NEVER
		}

		public virtual void SetThrottling(MockDirectoryWrapper.Throttling throttling)
		{
			this.throttling = throttling;
		}

		/// <summary>
		/// By default, opening and closing has a rare small sleep to catch race conditions
		/// <p>
		/// You can disable this if you dont need it
		/// </summary>
		public virtual void SetUseSlowOpenClosers(bool v)
		{
			useSlowOpenClosers = v;
		}

		/// <summary>
		/// Returns true if
		/// <see cref="FilterDirectory.@in">FilterDirectory.@in</see>
		/// must sync its files.
		/// Currently, only
		/// <see cref="NRTCachingDirectory">NRTCachingDirectory</see>
		/// requires sync'ing its files
		/// because otherwise they are cached in an internal
		/// <see cref="RAMDirectory">RAMDirectory</see>
		/// . If
		/// other directories require that too, they should be added to this method.
		/// </summary>
		private bool MustSync()
		{
			Directory delegate_ = @in;
			while (delegate_ is FilterDirectory)
			{
				delegate_ = ((FilterDirectory)delegate_).GetDelegate();
			}
			return delegate_ is NRTCachingDirectory;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Sync(ICollection<string> names)
		{
			lock (this)
			{
				MaybeYield();
				MaybeThrowDeterministicException();
				if (crashed)
				{
					throw new IOException("cannot sync after crash");
				}
				// don't wear out our hardware so much in tests.
				if (LuceneTestCase.Rarely(randomState) || MustSync())
				{
					foreach (string name in names)
					{
						// randomly fail with IOE on any file
						MaybeThrowIOException(name);
						@in.Sync(Sharpen.Collections.Singleton(name));
						unSyncedFiles.Remove(name);
					}
				}
				else
				{
					unSyncedFiles.RemoveAll(names);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public long SizeInBytes()
		{
			lock (this)
			{
				if (@in is RAMDirectory)
				{
					return ((RAMDirectory)@in).SizeInBytes();
				}
				else
				{
					// hack
					long size = 0;
					foreach (string file in @in.ListAll())
					{
						size += @in.FileLength(file);
					}
					return size;
				}
			}
		}

		/// <summary>
		/// Simulates a crash of OS or machine by overwriting
		/// unsynced files.
		/// </summary>
		/// <remarks>
		/// Simulates a crash of OS or machine by overwriting
		/// unsynced files.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Crash()
		{
			lock (this)
			{
				crashed = true;
				openFiles = new Dictionary<string, int>();
				openFilesForWrite = new HashSet<string>();
				openFilesDeleted = new HashSet<string>();
				Iterator<string> it = unSyncedFiles.Iterator();
				unSyncedFiles = new HashSet<string>();
				// first force-close all files, so we can corrupt on windows etc.
				// clone the file map, as these guys want to remove themselves on close.
				IDictionary<IDisposable, Exception> m = new IdentityHashMap<IDisposable, Exception
					>(openFileHandles);
				foreach (IDisposable f in m.Keys)
				{
					try
					{
						f.Close();
					}
					catch (Exception)
					{
					}
				}
				while (it.HasNext())
				{
					string name = it.Next();
					int damage = randomState.Next(5);
					string action = null;
					if (damage == 0)
					{
						action = "deleted";
						DeleteFile(name, true);
					}
					else
					{
						if (damage == 1)
						{
							action = "zeroed";
							// Zero out file entirely
							long length = FileLength(name);
							byte[] zeroes = new byte[256];
							long upto = 0;
							IndexOutput @out = @in.CreateOutput(name, LuceneTestCase.NewIOContext(randomState
								));
							while (upto < length)
							{
								int limit = (int)Math.Min(length - upto, zeroes.Length);
								@out.WriteBytes(zeroes, 0, limit);
								upto += limit;
							}
							@out.Close();
						}
						else
						{
							if (damage == 2)
							{
								action = "partially truncated";
								// Partially Truncate the file:
								// First, make temp file and copy only half this
								// file over:
								string tempFileName;
								while (true)
								{
									tempFileName = string.Empty + randomState.Next();
									if (!LuceneTestCase.SlowFileExists(@in, tempFileName))
									{
										break;
									}
								}
								IndexOutput tempOut = @in.CreateOutput(tempFileName, LuceneTestCase.NewIOContext(
									randomState));
								IndexInput ii = @in.OpenInput(name, LuceneTestCase.NewIOContext(randomState));
								tempOut.CopyBytes(ii, ii.Length() / 2);
								tempOut.Close();
								ii.Close();
								// Delete original and copy bytes back:
								DeleteFile(name, true);
								IndexOutput @out = @in.CreateOutput(name, LuceneTestCase.NewIOContext(randomState
									));
								ii = @in.OpenInput(tempFileName, LuceneTestCase.NewIOContext(randomState));
								@out.CopyBytes(ii, ii.Length());
								@out.Close();
								ii.Close();
								DeleteFile(tempFileName, true);
							}
							else
							{
								if (damage == 3)
								{
									// The file survived intact:
									action = "didn't change";
								}
								else
								{
									action = "fully truncated";
									// Totally truncate the file to zero bytes
									DeleteFile(name, true);
									IndexOutput @out = @in.CreateOutput(name, LuceneTestCase.NewIOContext(randomState
										));
									@out.SetLength(0);
									@out.Close();
								}
							}
						}
					}
					if (LuceneTestCase.VERBOSE)
					{
						System.Console.Out.WriteLine("MockDirectoryWrapper: " + action + " unsynced file: "
							 + name);
					}
				}
			}
		}

		public virtual void ClearCrash()
		{
			lock (this)
			{
				crashed = false;
				openLocks.Clear();
			}
		}

		public virtual void SetMaxSizeInBytes(long maxSize)
		{
			this.maxSize = maxSize;
		}

		public virtual long GetMaxSizeInBytes()
		{
			return this.maxSize;
		}

		/// <summary>
		/// Returns the peek actual storage used (bytes) in this
		/// directory.
		/// </summary>
		/// <remarks>
		/// Returns the peek actual storage used (bytes) in this
		/// directory.
		/// </remarks>
		public virtual long GetMaxUsedSizeInBytes()
		{
			return this.maxUsedSize;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void ResetMaxUsedSizeInBytes()
		{
			this.maxUsedSize = GetRecomputedActualSizeInBytes();
		}

		/// <summary>
		/// Emulate windows whereby deleting an open file is not
		/// allowed (raise IOException).
		/// </summary>
		/// <remarks>
		/// Emulate windows whereby deleting an open file is not
		/// allowed (raise IOException).
		/// </remarks>
		public virtual void SetNoDeleteOpenFile(bool value)
		{
			this.noDeleteOpenFile = value;
		}

		public virtual bool GetNoDeleteOpenFile()
		{
			return noDeleteOpenFile;
		}

		/// <summary>
		/// Trip a test
		/// 
		/// //assert if there is an attempt
		/// to delete an open file.
		/// </summary>
		/// <remarks>
		/// Trip a test
		/// 
		/// //assert if there is an attempt
		/// to delete an open file.
		/// </remarks>
		public virtual void SetAssertNoDeleteOpenFile(bool value)
		{
			this.assertNoDeleteOpenFile = value;
		}

		public virtual bool GetAssertNoDeleteOpenFile()
		{
			return assertNoDeleteOpenFile;
		}

		/// <summary>If 0.0, no exceptions will be thrown.</summary>
		/// <remarks>
		/// If 0.0, no exceptions will be thrown.  Else this should
		/// be a double 0.0 - 1.0.  We will randomly throw an
		/// IOException on the first write to an OutputStream based
		/// on this probability.
		/// </remarks>
		public virtual void SetRandomIOExceptionRate(double rate)
		{
			randomIOExceptionRate = rate;
		}

		public virtual double GetRandomIOExceptionRate()
		{
			return randomIOExceptionRate;
		}

		/// <summary>
		/// If 0.0, no exceptions will be thrown during openInput
		/// and createOutput.
		/// </summary>
		/// <remarks>
		/// If 0.0, no exceptions will be thrown during openInput
		/// and createOutput.  Else this should
		/// be a double 0.0 - 1.0 and we will randomly throw an
		/// IOException in openInput and createOutput with
		/// this probability.
		/// </remarks>
		public virtual void SetRandomIOExceptionRateOnOpen(double rate)
		{
			randomIOExceptionRateOnOpen = rate;
		}

		public virtual double GetRandomIOExceptionRateOnOpen()
		{
			return randomIOExceptionRateOnOpen;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void MaybeThrowIOException(string message)
		{
			if (randomState.NextDouble() < randomIOExceptionRate)
			{
				if (LuceneTestCase.VERBOSE)
				{
					System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": MockDirectoryWrapper: now throw random exception"
						 + (message == null ? string.Empty : " (" + message + ")"));
					Sharpen.Runtime.PrintStackTrace(new Exception(), System.Console.Out);
				}
				throw new IOException("a random IOException" + (message == null ? string.Empty : 
					" (" + message + ")"));
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void MaybeThrowIOExceptionOnOpen(string name)
		{
			if (randomState.NextDouble() < randomIOExceptionRateOnOpen)
			{
				if (LuceneTestCase.VERBOSE)
				{
					System.Console.Out.WriteLine(Sharpen.Thread.CurrentThread().GetName() + ": MockDirectoryWrapper: now throw random exception during open file="
						 + name);
					Sharpen.Runtime.PrintStackTrace(new Exception(), System.Console.Out);
				}
				if (allowRandomFileNotFoundException == false || randomState.NextBoolean())
				{
					throw new IOException("a random IOException (" + name + ")");
				}
				else
				{
					throw randomState.NextBoolean() ? new FileNotFoundException("a random IOException ("
						 + name + ")") : new NoSuchFileException("a random IOException (" + name + ")");
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void DeleteFile(string name)
		{
			lock (this)
			{
				MaybeYield();
				DeleteFile(name, false);
			}
		}

		// sets the cause of the incoming ioe to be the stack
		// trace when the offending file name was opened
		private Exception FillOpenTrace(Exception t, string name, bool input)
		{
			lock (this)
			{
				foreach (KeyValuePair<IDisposable, Exception> ent in openFileHandles.EntrySet())
				{
					if (input && ent.Key is MockIndexInputWrapper && ((MockIndexInputWrapper)ent.Key)
						.name.Equals(name))
					{
						Sharpen.Extensions.InitCause(t, ent.Value);
						break;
					}
					else
					{
						if (!input && ent.Key is MockIndexOutputWrapper && ((MockIndexOutputWrapper)ent.Key
							).name.Equals(name))
						{
							Sharpen.Extensions.InitCause(t, ent.Value);
							break;
						}
					}
				}
				return t;
			}
		}

		private void MaybeYield()
		{
			if (randomState.NextBoolean())
			{
				Sharpen.Thread.Yield();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void DeleteFile(string name, bool forced)
		{
			lock (this)
			{
				MaybeYield();
				MaybeThrowDeterministicException();
				if (crashed && !forced)
				{
					throw new IOException("cannot delete after crash");
				}
				if (unSyncedFiles.Contains(name))
				{
					unSyncedFiles.Remove(name);
				}
				if (!forced && (noDeleteOpenFile || assertNoDeleteOpenFile))
				{
					if (openFiles.ContainsKey(name))
					{
						openFilesDeleted.AddItem(name);
						if (!assertNoDeleteOpenFile)
						{
							throw (IOException)FillOpenTrace(new IOException("MockDirectoryWrapper: file \"" 
								+ name + "\" is still open: cannot delete"), name, true);
						}
						else
						{
							throw (Exception)FillOpenTrace(new Exception("MockDirectoryWrapper: file \"" + name
								 + "\" is still open: cannot delete"), name, true);
						}
					}
					else
					{
						openFilesDeleted.Remove(name);
					}
				}
				@in.DeleteFile(name);
			}
		}

		public virtual ICollection<string> GetOpenDeletedFiles()
		{
			lock (this)
			{
				return new HashSet<string>(openFilesDeleted);
			}
		}

		private bool failOnCreateOutput = true;

		public virtual void SetFailOnCreateOutput(bool v)
		{
			failOnCreateOutput = v;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override IndexOutput CreateOutput(string name, IOContext context)
		{
			lock (this)
			{
				MaybeThrowDeterministicException();
				MaybeThrowIOExceptionOnOpen(name);
				MaybeYield();
				if (failOnCreateOutput)
				{
					MaybeThrowDeterministicException();
				}
				if (crashed)
				{
					throw new IOException("cannot createOutput after crash");
				}
				Init();
				lock (this)
				{
					if (preventDoubleWrite && createdFiles.Contains(name) && !name.Equals("segments.gen"
						))
					{
						throw new IOException("file \"" + name + "\" was already written to");
					}
				}
				if ((noDeleteOpenFile || assertNoDeleteOpenFile) && openFiles.ContainsKey(name))
				{
					if (!assertNoDeleteOpenFile)
					{
						throw new IOException("MockDirectoryWrapper: file \"" + name + "\" is still open: cannot overwrite"
							);
					}
					else
					{
						throw new Exception("MockDirectoryWrapper: file \"" + name + "\" is still open: cannot overwrite"
							);
					}
				}
				if (crashed)
				{
					throw new IOException("cannot createOutput after crash");
				}
				unSyncedFiles.AddItem(name);
				createdFiles.AddItem(name);
				if (@in is RAMDirectory)
				{
					RAMDirectory ramdir = (RAMDirectory)@in;
					RAMFile file = new RAMFile(ramdir);
					RAMFile existing = ramdir.fileMap.Get(name);
					// Enforce write once:
					if (existing != null && !name.Equals("segments.gen") && preventDoubleWrite)
					{
						throw new IOException("file " + name + " already exists");
					}
					else
					{
						if (existing != null)
						{
							ramdir.sizeInBytes.GetAndAdd(-existing.sizeInBytes);
							existing.directory = null;
						}
						ramdir.fileMap.Put(name, file);
					}
				}
				//System.out.println(Thread.currentThread().getName() + ": MDW: create " + name);
				IndexOutput delegateOutput = @in.CreateOutput(name, LuceneTestCase.NewIOContext(randomState
					, context));
				if (randomState.Next(10) == 0)
				{
					// once in a while wrap the IO in a Buffered IO with random buffer sizes
					delegateOutput = new MockDirectoryWrapper.BufferedIndexOutputWrapper(this, 1 + randomState
						.Next(BufferedIndexOutput.DEFAULT_BUFFER_SIZE), delegateOutput);
				}
				IndexOutput io = new MockIndexOutputWrapper(this, delegateOutput, name);
				AddFileHandle(io, name, MockDirectoryWrapper.Handle.Output);
				openFilesForWrite.AddItem(name);
				// throttling REALLY slows down tests, so don't do it very often for SOMETIMES.
				if (throttling == MockDirectoryWrapper.Throttling.ALWAYS || (throttling == MockDirectoryWrapper.Throttling
					.SOMETIMES && randomState.Next(200) == 0) && !(@in is RateLimitedDirectoryWrapper
					))
				{
					if (LuceneTestCase.VERBOSE)
					{
						System.Console.Out.WriteLine("MockDirectoryWrapper: throttling indexOutput (" + name
							 + ")");
					}
					return throttledOutput.NewFromDelegate(io);
				}
				else
				{
					return io;
				}
			}
		}

		private enum Handle
		{
			Input,
			Output,
			Slice
		}

		internal virtual void AddFileHandle(IDisposable c, string name, MockDirectoryWrapper.Handle
			 handle)
		{
			lock (this)
			{
				int v = openFiles.Get(name);
				if (v != null)
				{
					v = Sharpen.Extensions.ValueOf(v + 1);
					openFiles.Put(name, v);
				}
				else
				{
					openFiles.Put(name, Sharpen.Extensions.ValueOf(1));
				}
				openFileHandles.Put(c, new RuntimeException("unclosed Index" + handle.ToString() 
					+ ": " + name));
			}
		}

		private bool failOnOpenInput = true;

		public virtual void SetFailOnOpenInput(bool v)
		{
			failOnOpenInput = v;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override IndexInput OpenInput(string name, IOContext context)
		{
			lock (this)
			{
				MaybeThrowDeterministicException();
				MaybeThrowIOExceptionOnOpen(name);
				MaybeYield();
				if (failOnOpenInput)
				{
					MaybeThrowDeterministicException();
				}
				if (!LuceneTestCase.SlowFileExists(@in, name))
				{
					throw randomState.NextBoolean() ? new FileNotFoundException(name + " in dir=" + @in
						) : new NoSuchFileException(name + " in dir=" + @in);
				}
				// cannot open a file for input if it's still open for
				// output, except for segments.gen and segments_N
				if (!allowReadingFilesStillOpenForWrite && openFilesForWrite.Contains(name) && !name
					.StartsWith("segments"))
				{
					throw (IOException)FillOpenTrace(new IOException("MockDirectoryWrapper: file \"" 
						+ name + "\" is still open for writing"), name, false);
				}
				IndexInput delegateInput = @in.OpenInput(name, LuceneTestCase.NewIOContext(randomState
					, context));
				IndexInput ii;
				int randomInt = randomState.Next(500);
				if (useSlowOpenClosers && randomInt == 0)
				{
					if (LuceneTestCase.VERBOSE)
					{
						System.Console.Out.WriteLine("MockDirectoryWrapper: using SlowClosingMockIndexInputWrapper for file "
							 + name);
					}
					ii = new SlowClosingMockIndexInputWrapper(this, name, delegateInput);
				}
				else
				{
					if (useSlowOpenClosers && randomInt == 1)
					{
						if (LuceneTestCase.VERBOSE)
						{
							System.Console.Out.WriteLine("MockDirectoryWrapper: using SlowOpeningMockIndexInputWrapper for file "
								 + name);
						}
						ii = new SlowOpeningMockIndexInputWrapper(this, name, delegateInput);
					}
					else
					{
						ii = new MockIndexInputWrapper(this, name, delegateInput);
					}
				}
				AddFileHandle(ii, name, MockDirectoryWrapper.Handle.Input);
				return ii;
			}
		}

		/// <summary>Provided for testing purposes.</summary>
		/// <remarks>Provided for testing purposes.  Use sizeInBytes() instead.</remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public long GetRecomputedSizeInBytes()
		{
			lock (this)
			{
				if (!(@in is RAMDirectory))
				{
					return SizeInBytes();
				}
				long size = 0;
				foreach (RAMFile file in ((RAMDirectory)@in).fileMap.Values)
				{
					size += file.GetSizeInBytes();
				}
				return size;
			}
		}

		/// <summary>
		/// Like getRecomputedSizeInBytes(), but, uses actual file
		/// lengths rather than buffer allocations (which are
		/// quantized up to nearest
		/// RAMOutputStream.BUFFER_SIZE (now 1024) bytes.
		/// </summary>
		/// <remarks>
		/// Like getRecomputedSizeInBytes(), but, uses actual file
		/// lengths rather than buffer allocations (which are
		/// quantized up to nearest
		/// RAMOutputStream.BUFFER_SIZE (now 1024) bytes.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public long GetRecomputedActualSizeInBytes()
		{
			lock (this)
			{
				if (!(@in is RAMDirectory))
				{
					return SizeInBytes();
				}
				long size = 0;
				foreach (RAMFile file in ((RAMDirectory)@in).fileMap.Values)
				{
					size += file.length;
				}
				return size;
			}
		}

		private bool assertNoUnreferencedFilesOnClose;

		// NOTE: This is off by default; see LUCENE-5574
		public virtual void SetAssertNoUnrefencedFilesOnClose(bool v)
		{
			assertNoUnreferencedFilesOnClose = v;
		}

		/// <summary>
		/// Set to false if you want to return the pure lockfactory
		/// and not wrap it with MockLockFactoryWrapper.
		/// </summary>
		/// <remarks>
		/// Set to false if you want to return the pure lockfactory
		/// and not wrap it with MockLockFactoryWrapper.
		/// <p>
		/// Be careful if you turn this off: MockDirectoryWrapper might
		/// no longer be able to detect if you forget to close an IndexWriter,
		/// and spit out horribly scary confusing exceptions instead of
		/// simply telling you that.
		/// </remarks>
		public virtual void SetWrapLockFactory(bool v)
		{
			this.wrapLockFactory = v;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Close()
		{
			lock (this)
			{
				// files that we tried to delete, but couldn't because readers were open.
				// all that matters is that we tried! (they will eventually go away)
				ICollection<string> pendingDeletions = new HashSet<string>(openFilesDeleted);
				MaybeYield();
				if (openFiles == null)
				{
					openFiles = new Dictionary<string, int>();
					openFilesDeleted = new HashSet<string>();
				}
				if (openFiles.Count > 0)
				{
					// print the first one as its very verbose otherwise
					Exception cause = null;
					Iterator<Exception> stacktraces = openFileHandles.Values.Iterator();
					if (stacktraces.HasNext())
					{
						cause = stacktraces.Next();
					}
					// RuntimeException instead of IOException because
					// super() does not throw IOException currently:
					throw new RuntimeException("MockDirectoryWrapper: cannot close: there are still open files: "
						 + openFiles, cause);
				}
				if (openLocks.Count > 0)
				{
					throw new RuntimeException("MockDirectoryWrapper: cannot close: there are still open locks: "
						 + openLocks);
				}
				isOpen = false;
				if (GetCheckIndexOnClose())
				{
					randomIOExceptionRate = 0.0;
					randomIOExceptionRateOnOpen = 0.0;
					if (DirectoryReader.IndexExists(this))
					{
						if (LuceneTestCase.VERBOSE)
						{
							System.Console.Out.WriteLine("\nNOTE: MockDirectoryWrapper: now crush");
						}
						Crash();
						// corrupt any unsynced-files
						if (LuceneTestCase.VERBOSE)
						{
							System.Console.Out.WriteLine("\nNOTE: MockDirectoryWrapper: now run CheckIndex");
						}
						TestUtil.CheckIndex(this, GetCrossCheckTermVectorsOnClose());
						// TODO: factor this out / share w/ TestIW.assertNoUnreferencedFiles
						if (assertNoUnreferencedFilesOnClose)
						{
							// now look for unreferenced files: discount ones that we tried to delete but could not
							ICollection<string> allFiles = new HashSet<string>(Arrays.AsList(ListAll()));
							allFiles.RemoveAll(pendingDeletions);
							string[] startFiles = Sharpen.Collections.ToArray(allFiles, new string[0]);
							IndexWriterConfig iwc = new IndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT
								, null);
							iwc.SetIndexDeletionPolicy(NoDeletionPolicy.INSTANCE);
							new IndexWriter(@in, iwc).Rollback();
							string[] endFiles = @in.ListAll();
							ICollection<string> startSet = new TreeSet<string>(Arrays.AsList(startFiles));
							ICollection<string> endSet = new TreeSet<string>(Arrays.AsList(endFiles));
							if (pendingDeletions.Contains("segments.gen") && endSet.Contains("segments.gen"))
							{
								// this is possible if we hit an exception while writing segments.gen, we try to delete it
								// and it ends out in pendingDeletions (but IFD wont remove this).
								startSet.AddItem("segments.gen");
								if (LuceneTestCase.VERBOSE)
								{
									System.Console.Out.WriteLine("MDW: Unreferenced check: Ignoring segments.gen that we could not delete."
										);
								}
							}
							// its possible we cannot delete the segments_N on windows if someone has it open and
							// maybe other files too, depending on timing. normally someone on windows wouldnt have
							// an issue (IFD would nuke this stuff eventually), but we pass NoDeletionPolicy...
							foreach (string file in pendingDeletions)
							{
								if (file.StartsWith("segments") && !file.Equals("segments.gen") && endSet.Contains
									(file))
								{
									startSet.AddItem(file);
									if (LuceneTestCase.VERBOSE)
									{
										System.Console.Out.WriteLine("MDW: Unreferenced check: Ignoring segments file: " 
											+ file + " that we could not delete.");
									}
									SegmentInfos sis = new SegmentInfos();
									try
									{
										sis.Read(@in, file);
									}
									catch (IOException)
									{
									}
									// OK: likely some of the .si files were deleted
									try
									{
										ICollection<string> ghosts = new HashSet<string>(sis.Files(@in, false));
										foreach (string s in ghosts)
										{
											if (endSet.Contains(s) && !startSet.Contains(s))
											{
												 
												//assert pendingDeletions.contains(s);
												if (LuceneTestCase.VERBOSE)
												{
													System.Console.Out.WriteLine("MDW: Unreferenced check: Ignoring referenced file: "
														 + s + " " + "from " + file + " that we could not delete.");
												}
												startSet.AddItem(s);
											}
										}
									}
									catch (Exception t)
									{
										System.Console.Error.WriteLine("ERROR processing leftover segments file " + file 
											+ ":");
										Sharpen.Runtime.PrintStackTrace(t);
									}
								}
							}
							startFiles = Sharpen.Collections.ToArray(startSet, new string[0]);
							endFiles = Sharpen.Collections.ToArray(endSet, new string[0]);
							if (!Arrays.Equals(startFiles, endFiles))
							{
								IList<string> removed = new AList<string>();
								foreach (string fileName in startFiles)
								{
									if (!endSet.Contains(fileName))
									{
										removed.AddItem(fileName);
									}
								}
								IList<string> added = new AList<string>();
								foreach (string fileName_1 in endFiles)
								{
									if (!startSet.Contains(fileName_1))
									{
										added.AddItem(fileName_1);
									}
								}
								string extras;
								if (removed.Count != 0)
								{
									extras = "\n\nThese files were removed: " + removed;
								}
								else
								{
									extras = string.Empty;
								}
								if (added.Count != 0)
								{
									extras += "\n\nThese files were added (waaaaaaaaaat!): " + added;
								}
								if (pendingDeletions.Count != 0)
								{
									extras += "\n\nThese files we had previously tried to delete, but couldn't: " + pendingDeletions;
								}
							}
							 
							//assert false : "unreferenced files: before delete:\n    " + Arrays.toString(startFiles) + "\n  after delete:\n    " + Arrays.toString(endFiles) + extras;
							DirectoryReader ir1 = DirectoryReader.Open(this);
							int numDocs1 = ir1.NumDocs();
							ir1.Close();
							new IndexWriter(this, new IndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, 
								null)).Close();
							DirectoryReader ir2 = DirectoryReader.Open(this);
							int numDocs2 = ir2.NumDocs();
							ir2.Close();
						}
					}
				}
				 
				//assert numDocs1 == numDocs2 : "numDocs changed after opening/closing IW: before=" + numDocs1 + " after=" + numDocs2;
				@in.Close();
			}
		}

		internal virtual void RemoveOpenFile(IDisposable c, string name)
		{
			lock (this)
			{
				int v = openFiles.Get(name);
				// Could be null when crash() was called
				if (v != null)
				{
					if (v == 1)
					{
						Sharpen.Collections.Remove(openFiles, name);
					}
					else
					{
						v = Sharpen.Extensions.ValueOf(v - 1);
						openFiles.Put(name, v);
					}
				}
				Sharpen.Collections.Remove(openFileHandles, c);
			}
		}

		public virtual void RemoveIndexOutput(IndexOutput @out, string name)
		{
			lock (this)
			{
				openFilesForWrite.Remove(name);
				RemoveOpenFile(@out, name);
			}
		}

		public virtual void RemoveIndexInput(IndexInput @in, string name)
		{
			lock (this)
			{
				RemoveOpenFile(@in, name);
			}
		}

		/// <summary>Objects that represent fail-able conditions.</summary>
		/// <remarks>
		/// Objects that represent fail-able conditions. Objects of a derived
		/// class are created and registered with the mock directory. After
		/// register, each object will be invoked once for each first write
		/// of a file, giving the object a chance to throw an IOException.
		/// </remarks>
		public class Failure
		{
			/// <summary>eval is called on the first write of every new file.</summary>
			/// <remarks>eval is called on the first write of every new file.</remarks>
			/// <exception cref="System.IO.IOException"></exception>
			public virtual void Eval(MockDirectoryWrapper dir)
			{
			}

			/// <summary>
			/// reset should set the state of the failure to its default
			/// (freshly constructed) state.
			/// </summary>
			/// <remarks>
			/// reset should set the state of the failure to its default
			/// (freshly constructed) state. Reset is convenient for tests
			/// that want to create one failure object and then reuse it in
			/// multiple cases. This, combined with the fact that Failure
			/// subclasses are often anonymous classes makes reset difficult to
			/// do otherwise.
			/// A typical example of use is
			/// Failure failure = new Failure() { ... };
			/// ...
			/// mock.failOn(failure.reset())
			/// </remarks>
			public virtual MockDirectoryWrapper.Failure Reset()
			{
				return this;
			}

			protected internal bool doFail;

			public virtual void SetDoFail()
			{
				doFail = true;
			}

			public virtual void ClearDoFail()
			{
				doFail = false;
			}
		}

		internal AList<MockDirectoryWrapper.Failure> failures;

		/// <summary>
		/// add a Failure object to the list of objects to be evaluated
		/// at every potential failure point
		/// </summary>
		public virtual void FailOn(MockDirectoryWrapper.Failure fail)
		{
			lock (this)
			{
				if (failures == null)
				{
					failures = new AList<MockDirectoryWrapper.Failure>();
				}
				failures.AddItem(fail);
			}
		}

		/// <summary>
		/// Iterate through the failures list, giving each object a
		/// chance to throw an IOE
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void MaybeThrowDeterministicException()
		{
			lock (this)
			{
				if (failures != null)
				{
					for (int i = 0; i < failures.Count; i++)
					{
						failures[i].Eval(this);
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override string[] ListAll()
		{
			lock (this)
			{
				MaybeYield();
				return @in.ListAll();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool FileExists(string name)
		{
			lock (this)
			{
				MaybeYield();
				return @in.FileExists(name);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override long FileLength(string name)
		{
			lock (this)
			{
				MaybeYield();
				return @in.FileLength(name);
			}
		}

		public override Lock MakeLock(string name)
		{
			lock (this)
			{
				MaybeYield();
				return GetLockFactory().MakeLock(name);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void ClearLock(string name)
		{
			lock (this)
			{
				MaybeYield();
				GetLockFactory().ClearLock(name);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetLockFactory(LockFactory lockFactory)
		{
			lock (this)
			{
				MaybeYield();
				// sneaky: we must pass the original this way to the dir, because
				// some impls (e.g. FSDir) do instanceof here.
				@in.SetLockFactory(lockFactory);
				// now set our wrapped factory here
				this.lockFactory = new MockLockFactoryWrapper(this, lockFactory);
			}
		}

		public override LockFactory GetLockFactory()
		{
			lock (this)
			{
				MaybeYield();
				if (wrapLockFactory)
				{
					return lockFactory;
				}
				else
				{
					return @in.GetLockFactory();
				}
			}
		}

		public override string GetLockID()
		{
			lock (this)
			{
				MaybeYield();
				return @in.GetLockID();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Copy(Directory to, string src, string dest, IOContext context
			)
		{
			lock (this)
			{
				MaybeYield();
				// randomize the IOContext here?
				@in.Copy(to, src, dest, context);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Directory.IndexInputSlicer CreateSlicer(string name, IOContext context
			)
		{
			MaybeYield();
			if (!LuceneTestCase.SlowFileExists(@in, name))
			{
				throw randomState.NextBoolean() ? new FileNotFoundException(name) : new NoSuchFileException
					(name);
			}
			// cannot open a file for input if it's still open for
			// output, except for segments.gen and segments_N
			if (openFilesForWrite.Contains(name) && !name.StartsWith("segments"))
			{
				throw (IOException)FillOpenTrace(new IOException("MockDirectoryWrapper: file \"" 
					+ name + "\" is still open for writing"), name, false);
			}
			Directory.IndexInputSlicer delegateHandle = @in.CreateSlicer(name, context);
			Directory.IndexInputSlicer handle = new _IndexInputSlicer_973(this, delegateHandle
				, name);
			AddFileHandle(handle, name, MockDirectoryWrapper.Handle.Slice);
			return handle;
		}

		private sealed class _IndexInputSlicer_973 : Directory.IndexInputSlicer
		{
			public _IndexInputSlicer_973(MockDirectoryWrapper _enclosing, Directory.IndexInputSlicer
				 delegateHandle, string name) : base(_enclosing)
			{
				this._enclosing = _enclosing;
				this.delegateHandle = delegateHandle;
				this.name = name;
			}

			private bool isClosed;

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				if (!this.isClosed)
				{
					delegateHandle.Close();
					this._enclosing.RemoveOpenFile(this, name);
					this.isClosed = true;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IndexInput OpenSlice(string sliceDescription, long offset, long length
				)
			{
				this._enclosing.MaybeYield();
				IndexInput ii = new MockIndexInputWrapper(this._enclosing, name, delegateHandle.OpenSlice
					(sliceDescription, offset, length));
				this._enclosing.AddFileHandle(ii, name, MockDirectoryWrapper.Handle.Input);
				return ii;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override IndexInput OpenFullSlice()
			{
				this._enclosing.MaybeYield();
				IndexInput ii = new MockIndexInputWrapper(this._enclosing, name, delegateHandle.OpenFullSlice
					());
				this._enclosing.AddFileHandle(ii, name, MockDirectoryWrapper.Handle.Input);
				return ii;
			}

			private readonly MockDirectoryWrapper _enclosing;

			private readonly Directory.IndexInputSlicer delegateHandle;

			private readonly string name;
		}

		internal sealed class BufferedIndexOutputWrapper : BufferedIndexOutput
		{
			private readonly IndexOutput io;

			public BufferedIndexOutputWrapper(MockDirectoryWrapper _enclosing, int bufferSize
				, IndexOutput io) : base(bufferSize)
			{
				this._enclosing = _enclosing;
				this.io = io;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override long Length()
			{
				return this.io.Length();
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected override void FlushBuffer(byte[] b, int offset, int len)
			{
				this.io.WriteBytes(b, offset, len);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Seek(long pos)
			{
				this.Flush();
				this.io.Seek(pos);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Flush()
			{
				try
				{
					base.Flush();
				}
				finally
				{
					this.io.Flush();
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				try
				{
					base.Close();
				}
				finally
				{
					this.io.Close();
				}
			}

			private readonly MockDirectoryWrapper _enclosing;
		}

		/// <summary>
		/// Use this when throwing fake
		/// <code>IOException</code>
		/// ,
		/// e.g. from
		/// <see cref="Failure">Failure</see>
		/// .
		/// </summary>
		[System.Serializable]
		public class FakeIOException : IOException
		{
		}
    }
}
