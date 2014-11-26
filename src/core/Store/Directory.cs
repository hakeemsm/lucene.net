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

using Lucene.Net.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Store
{

    /// <summary>A Directory is a flat list of files.  Files may be written once, when they
    /// are created.  Once a file is created it may only be opened for read, or
    /// deleted.  Random access is permitted both when reading and writing.
    /// 
    /// <p/> Java's i/o APIs not used directly, but rather all i/o is
    /// through this API.  This permits things such as: <list>
    /// <item> implementation of RAM-based indices;</item>
    /// <item> implementation indices stored in a database, via JDBC;</item>
    /// <item> implementation of an index as a single file;</item>
    /// </list>
    /// 
    /// Directory locking is implemented by an instance of <see cref="LockFactory" />
    ///, and can be changed for each Directory
    /// instance using <see cref="SetLockFactory" />.
    /// 
    /// </summary>
    [Serializable]
    public abstract class Directory : IDisposable
    {
        protected volatile bool isOpen = true;

        /// <summary>Holds the LockFactory instance (implements locking for
        /// this Directory instance). 
        /// </summary>
        [NonSerialized]
        protected LockFactory interalLockFactory;

        /// <summary>Returns an array of strings, one for each file in the directory.</summary>
        /// <exception cref="System.IO.IOException"></exception>
        public abstract String[] ListAll();

        /// <summary>Returns true iff a file with the given name exists. </summary>
        [System.ObsoleteAttribute(@"This method will be removed in 5.0")]
        public abstract bool FileExists(String name);

        /// <summary>Removes an existing file in the directory. </summary>
        public abstract void DeleteFile(String name);

        /// <summary>Returns the length of a file in the directory. </summary>
        public abstract long FileLength(String name);


        /// <summary>Creates a new, empty file in the directory with the given name.
        /// Returns a stream writing this file. 
        /// </summary>
        public abstract IndexOutput CreateOutput(String name, IOContext context);

        /// <summary>Ensure that any writes to this file are moved to
        /// stable storage.  Lucene uses this to properly commit
        /// changes to the index, to prevent a machine/OS crash
        /// from corrupting the index. 
        /// </summary>
        public abstract void Sync(ICollection<String> names);

        /// <summary>Returns a stream reading an existing file. </summary>
        public abstract IndexInput OpenInput(String name, IOContext context);

        /// <summary>Returns a stream reading an existing file, computing checksum as it reads
        ///   </summary>
        /// <exception cref="System.IO.IOException"></exception>
        public virtual ChecksumIndexInput OpenChecksumInput(string name, IOContext context
           )
        {
            return new BufferedChecksumIndexInput(OpenInput(name, context));
        }
        /// <summary>Construct a <see cref="Lock" />.</summary>
        /// <param name="name">the name of the lock file
        /// </param>
        public virtual Lock MakeLock(String name)
        {
            return interalLockFactory.MakeLock(name);
        }
        /// <summary> Attempt to clear (forcefully unlock and remove) the
        /// specified lock.  Only call this at a time when you are
        /// certain this lock is no longer in use.
        /// </summary>
        /// <param name="name">name of the lock to be cleared.
        /// </param>
        public virtual void ClearLock(String name)
        {
            if (interalLockFactory != null)
            {
                interalLockFactory.ClearLock(name);
            }
        }

        /// <summary>Closes the store. </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        protected abstract void Dispose(bool disposing);

        /// <summary> Get the LockFactory that this Directory instance is
        /// using for its locking implementation.  Note that this
        /// may be null for Directory implementations that provide
        /// their own locking implementation.
        /// </summary>
        public virtual LockFactory LockFactory
        {
            get { return this.interalLockFactory; }
            set
            {
                System.Diagnostics.Debug.Assert(value != null);
                this.interalLockFactory = value;
                value.LockPrefix = this.LockId;
            }
        }

        /// <summary> Return a string identifier that uniquely differentiates
        /// this Directory instance from other Directory instances.
        /// This ID should be the same if two Directory instances
        /// (even in different JVMs and/or on different machines)
        /// are considered "the same index".  This is how locking
        /// "scopes" to the right index.
        /// </summary>
        public virtual string LockId
        {
            get { return ToString(); }
        }

        public override string ToString()
        {
            return base.ToString() + " lockFactory=" + LockFactory;
        }

        /// <summary> Copy contents of a directory src to a directory dest.
        /// If a file in src already exists in dest then the
        /// one in dest will be blindly overwritten.
        /// 
        /// <p/><b>NOTE:</b> the source directory cannot change
        /// while this method is running.  Otherwise the results
        /// are undefined and you could easily hit a
        /// FileNotFoundException.
        /// 
        /// <p/><b>NOTE:</b> this method only copies files that look
        /// like index files (ie, have extensions matching the
        /// known extensions of index files).
        /// 
        /// </summary>
        /// <throws>  IOException </throws>
        public virtual void Copy(Directory to, string src, string dest, IOContext context)
        {
            IndexOutput os = null;
            IndexInput iinput = null;
            System.IO.IOException priorException = null;
            try
            {
                os = to.CreateOutput(dest, context);
                iinput = OpenInput(src, context);
                os.CopyBytes(iinput, iinput.Length);
            }
            catch (System.IO.IOException ioe)
            {
                priorException = ioe;
            }
            finally
            {
                bool success = false;
                try
                {
                    IOUtils.CloseWhileHandlingException(priorException, os, iinput);
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        try
                        {
                            to.DeleteFile(dest);
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        private class AnonymousCreateSlicer : IndexInputSlicer
        {
            private readonly IndexInput baseinput;

            public AnonymousCreateSlicer(Directory parent, string name, IOContext context)
            {
                baseinput = parent.OpenInput(name, context);
            }

            public override IndexInput OpenSlice(string sliceDescription, long offset, long length)
            {
                return new SlicedIndexInput("SlicedIndexInput(" + sliceDescription + " in " + baseinput + ")", baseinput, offset, length);
            }

            public override void Dispose(bool disposing)
            {
                baseinput.Dispose();
            }

            public override IndexInput OpenFullSlice()
            {
                return (IndexInput)baseinput.Clone();
            }
        }

        public virtual IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            EnsureOpen();
            return new AnonymousCreateSlicer(this, name, context);
        }

        /// <throws>  AlreadyClosedException if this Directory is closed </throws>
        public void EnsureOpen()
        {
            if (!isOpen)
                throw new AlreadyClosedException("this Directory is closed");
        }

        public abstract class IndexInputSlicer : IDisposable
        {
            public abstract IndexInput OpenSlice(string sliceDescription, long offset, long length);


            [Obsolete(@"Only for reading CFS files from 3.x indexes.")]
            public abstract IndexInput OpenFullSlice();

            public abstract void Dispose(bool disposing);

            public void Dispose()
            {
                Dispose(true);
            }
            
            private readonly Directory _enclosing;
        }

        private sealed class SlicedIndexInput : BufferedIndexInput
        {
            private IndexInput baseinput;
            private long fileOffset;
            private long length;

            public SlicedIndexInput(String sliceDescription, IndexInput baseinput, long fileOffset, long length)
                : this(sliceDescription, baseinput, fileOffset, length, BUFFER_SIZE)
            {
            }

            public SlicedIndexInput(String sliceDescription, IndexInput baseinput, long fileOffset, long length, int readBufferSize)
                : base("SlicedIndexInput(" + sliceDescription + " in " + baseinput + " slice=" + fileOffset + ":" + (fileOffset + length) + ")", readBufferSize)
            {
                this.baseinput = (IndexInput)baseinput.Clone();
                this.fileOffset = fileOffset;
                this.length = length;
            }

            public override object Clone()
            {
                SlicedIndexInput clone = (SlicedIndexInput)base.Clone();
                clone.baseinput = (IndexInput)baseinput.Clone();
                clone.fileOffset = fileOffset;
                clone.length = length;
                return clone;
            }

            public override void ReadInternal(byte[] b, int offset, int len)
            {
                long start = FilePointer;
                if (start + len > length)
                    throw new System.IO.EndOfStreamException("read past EOF: " + this);
                baseinput.Seek(fileOffset + start);
                baseinput.ReadBytes(b, offset, len, false);
            }

            public override void SeekInternal(long pos)
            {
            }

            protected override void Dispose(bool disposing)
            {
                baseinput.Dispose();
            }

            public override long Length
            {
                get { return length; }
            }
        }

        public bool isOpen_ForNUnit
        {
            get { return isOpen; }
        }
    }
}