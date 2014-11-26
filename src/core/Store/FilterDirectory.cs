using System;
using System.Collections.Generic;
using Lucene.Net.Store;

namespace Lucene.Net.Store
{
    /// <summary>Directory implementation that delegates calls to another directory.</summary>
    /// <remarks>
    /// Directory implementation that delegates calls to another directory.
    /// This class can be used to add limitations on top of an existing
    /// <see cref="Directory">Directory</see>
    /// implementation such as
    /// <see cref="RateLimitedDirectoryWrapper">rate limiting</see>
    /// or to add additional
    /// sanity checks for tests. However, if you plan to write your own
    /// <see cref="Directory">Directory</see>
    /// implementation, you should consider extending directly
    /// <see cref="Directory">Directory</see>
    /// or
    /// <see cref="BaseDirectory">BaseDirectory</see>
    /// rather than try to reuse
    /// functionality of existing
    /// <see cref="Directory">Directory</see>
    /// s by extending this class.
    /// </remarks>
    /// <lucene.internal></lucene.internal>
    public class FilterDirectory : Directory
    {
        protected Directory dir;
        private bool isDisposed;

        /// <summary>Sole constructor, typically called from sub-classes.</summary>
        /// <remarks>Sole constructor, typically called from sub-classes.</remarks>
        protected internal FilterDirectory(Directory dir)
        {
            this.dir = dir;
        }

        /// <summary>
        /// Return the wrapped
        /// <see cref="Directory">Directory</see>
        /// .
        /// </summary>
        public Directory GetDelegate()
        {
            return dir;
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override string[] ListAll()
        {
            return dir.ListAll();
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override bool FileExists(string name)
        {
            return dir.FileExists(name);
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override void DeleteFile(string name)
        {
            dir.DeleteFile(name);
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override long FileLength(string name)
        {
            return dir.FileLength(name);
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            return dir.CreateOutput(name, context);
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override void Sync(ICollection<string> names)
        {
            dir.Sync(names);
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override IndexInput OpenInput(string name, IOContext context)
        {
            return dir.OpenInput(name, context);
        }

        public override Lock MakeLock(string name)
        {
            return dir.MakeLock(name);
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override void ClearLock(string name)
        {
            dir.ClearLock(name);
        }

       
        protected override void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }
            if (dir!=null)
            {
                dir.Dispose();
                dir = null;
                isDisposed = true;
            }

        }

        /// <exception cref="System.IO.IOException"></exception>
        public override LockFactory LockFactory
        {
            get
            {
                return dir.LockFactory;
            }
            set
            {
                dir.LockFactory = value;
            }
        }


        public override string LockId
        {
            get { return dir.LockId; }
        }

       

        public override string ToString()
        {
            return GetType().Name + "(" + dir.ToString() + ")";
        }
    }
}
