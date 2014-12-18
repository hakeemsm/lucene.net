//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.IO;

//namespace Lucene.Net.Util
//{
//    /// <summary>On-disk sorting of byte arrays.</summary>
//    /// <remarks>
//    /// On-disk sorting of byte arrays. Each byte array (entry) is a composed of the following
//    /// fields:
//    /// <ul>
//    /// <li>(two bytes) length of the following byte array,
//    /// <li>exactly the above count of bytes for the sequence to be sorted.
//    /// </ul>
//    /// </remarks>
//    /// <seealso cref="Sort(Sharpen.FilePath, Sharpen.FilePath)">Sort(Sharpen.FilePath, Sharpen.FilePath)
//    /// 	</seealso>
//    /// <lucene.experimental></lucene.experimental>
//    /// <lucene.internal></lucene.internal>
//    public sealed class OfflineSorter
//    {
//        /// <summary>Convenience constant for megabytes</summary>
//        public const long MB = 1024 * 1024;

//        /// <summary>Convenience constant for gigabytes</summary>
//        public const long GB = MB * 1024;

//        /// <summary>Minimum recommended buffer size for sorting.</summary>
//        /// <remarks>Minimum recommended buffer size for sorting.</remarks>
//        public const long MIN_BUFFER_SIZE_MB = 32;

//        /// <summary>Absolute minimum required buffer size for sorting.</summary>
//        /// <remarks>Absolute minimum required buffer size for sorting.</remarks>
//        public const long ABSOLUTE_MIN_SORT_BUFFER_SIZE = MB / 2;

//        private static readonly string MIN_BUFFER_SIZE_MSG = "At least 0.5MB RAM buffer is needed";

//        /// <summary>Maximum number of temporary files before doing an intermediate merge.</summary>
//        /// <remarks>Maximum number of temporary files before doing an intermediate merge.</remarks>
//        public const int MAX_TEMPFILES = 128;

//        /// <summary>A bit more descriptive unit for constructors.</summary>
//        /// <remarks>A bit more descriptive unit for constructors.</remarks>
//        /// <seealso cref="Automatic()">Automatic()</seealso>
//        /// <seealso cref="Megabytes(long)">Megabytes(long)</seealso>
//        public sealed class BufferSize
//        {
//            internal readonly int bytes;

//            private BufferSize(long bytes)
//            {
//                if (bytes > int.MaxValue)
//                {
//                    throw new ArgumentException("Buffer too large for Java (" + (int.MaxValue / MB) +
//                         "mb max): " + bytes);
//                }
//                if (bytes < ABSOLUTE_MIN_SORT_BUFFER_SIZE)
//                {
//                    throw new ArgumentException(MIN_BUFFER_SIZE_MSG + ": " + bytes);
//                }
//                this.bytes = (int)bytes;
//            }

//            /// <summary>
//            /// Creates a
//            /// <see cref="BufferSize">BufferSize</see>
//            /// in MB. The given
//            /// values must be &gt; 0 and &lt; 2048.
//            /// </summary>
//            public static OfflineSorter.BufferSize Megabytes(long mb)
//            {
//                return new OfflineSorter.BufferSize(mb * MB);
//            }

//            /// <summary>
//            /// Approximately half of the currently available free heap, but no less
//            /// than
//            /// <see cref="OfflineSorter.ABSOLUTE_MIN_SORT_BUFFER_SIZE">OfflineSorter.ABSOLUTE_MIN_SORT_BUFFER_SIZE
//            /// 	</see>
//            /// . However if current heap allocation
//            /// is insufficient or if there is a large portion of unallocated heap-space available
//            /// for sorting consult with max allowed heap size.
//            /// </summary>
//            public static BufferSize Automatic()
//            {
//                long totalMemory = GC.GetTotalMemory(false);
//                Runtime rt = Runtime.GetRuntime();
//                // take sizes in "conservative" order
//                long max = rt.MaxMemory();
//                // max allocated
//                long total = rt.TotalMemory();
//                // currently allocated
//                long free = rt.FreeMemory();
//                // unused portion of currently allocated
//                long totalAvailableBytes = max - total + free;
//                // by free mem (attempting to not grow the heap for this)
//                long sortBufferByteSize = free / 2;
//                long minBufferSizeBytes = MIN_BUFFER_SIZE_MB * MB;
//                if (sortBufferByteSize < minBufferSizeBytes || totalAvailableBytes > 10 * minBufferSizeBytes)
//                {
//                    // lets see if we need/should to grow the heap 
//                    if (totalAvailableBytes / 2 > minBufferSizeBytes)
//                    {
//                        // there is enough mem for a reasonable buffer
//                        sortBufferByteSize = totalAvailableBytes / 2;
//                    }
//                    else
//                    {
//                        // grow the heap
//                        //heap seems smallish lets be conservative fall back to the free/2 
//                        sortBufferByteSize = Math.Max(ABSOLUTE_MIN_SORT_BUFFER_SIZE, sortBufferByteSize);
//                    }
//                }
//                return new BufferSize(Math.Min((long)int.MaxValue, sortBufferByteSize));
//            }
//        }

//        /// <summary>Sort info (debugging mostly).</summary>
//        /// <remarks>Sort info (debugging mostly).</remarks>
//        public class SortInfo
//        {
//            /// <summary>number of temporary files created when merging partitions</summary>
//            public int tempMergeFiles;

//            /// <summary>number of partition merges</summary>
//            public int mergeRounds;

//            /// <summary>number of lines of data read</summary>
//            public int lines;

//            /// <summary>time spent merging sorted partitions (in milliseconds)</summary>
//            public long mergeTime;

//            /// <summary>time spent sorting data (in milliseconds)</summary>
//            public long sortTime;

//            /// <summary>total time spent (in milliseconds)</summary>
//            public long totalTime;

//            /// <summary>time spent in i/o read (in milliseconds)</summary>
//            public long readTime;

//            /// <summary>read buffer size (in bytes)</summary>
//            public readonly long bufferSize = this._enclosing.ramBufferSize.bytes;

//            /// <summary>create a new SortInfo (with empty statistics) for debugging</summary>
//            public SortInfo(OfflineSorter _enclosing)
//            {
//                this._enclosing = _enclosing;
//            }

//            public override string ToString()
//            {
//                return string.Format(CultureInfo.ROOT, "time=%.2f sec. total (%.2f reading, %.2f sorting, %.2f merging), lines=%d, temp files=%d, merges=%d, soft ram limit=%.2f MB"
//                    , this.totalTime / 1000.0d, this.readTime / 1000.0d, this.sortTime / 1000.0d, this
//                    .mergeTime / 1000.0d, this.lines, this.tempMergeFiles, this.mergeRounds, (double
//                    )this.bufferSize / OfflineSorter.MB);
//            }

//            private readonly OfflineSorter _enclosing;
//        }

//        private readonly OfflineSorter.BufferSize ramBufferSize;

//        private readonly FilePath tempDirectory;

//        private readonly Counter bufferBytesUsed = Counter.NewCounter();

//        private readonly BytesRefArray buffer;

//        private OfflineSorter.SortInfo sortInfo;

//        private int maxTempFiles;

//        private readonly IComparer<BytesRef> comparator;

//        /// <summary>Default comparator: sorts in binary (codepoint) order</summary>
//        public static readonly IComparer<BytesRef> DEFAULT_COMPARATOR = BytesRef.GetUTF8SortedAsUnicodeComparator
//            ();

//        /// <summary>Defaults constructor.</summary>
//        /// <remarks>Defaults constructor.</remarks>
//        /// <seealso cref="DefaultTempDir()">DefaultTempDir()</seealso>
//        /// <seealso cref="BufferSize.Automatic()">BufferSize.Automatic()</seealso>
//        /// <exception cref="System.IO.IOException"></exception>
//        public OfflineSorter() : this(DEFAULT_COMPARATOR, OfflineSorter.BufferSize.Automatic
//            (), DefaultTempDir(), MAX_TEMPFILES)
//        {
//            buffer = new BytesRefArray(bufferBytesUsed);
//        }

//        /// <summary>Defaults constructor with a custom comparator.</summary>
//        /// <remarks>Defaults constructor with a custom comparator.</remarks>
//        /// <seealso cref="DefaultTempDir()">DefaultTempDir()</seealso>
//        /// <seealso cref="BufferSize.Automatic()">BufferSize.Automatic()</seealso>
//        /// <exception cref="System.IO.IOException"></exception>
//        public OfflineSorter(IComparer<BytesRef> comparator) : this(comparator, OfflineSorter.BufferSize
//            .Automatic(), DefaultTempDir(), MAX_TEMPFILES)
//        {
//            buffer = new BytesRefArray(bufferBytesUsed);
//        }

//        /// <summary>All-details constructor.</summary>
//        /// <remarks>All-details constructor.</remarks>
//        public OfflineSorter(IComparer<BytesRef> comparator, OfflineSorter.BufferSize ramBufferSize
//            , FilePath tempDirectory, int maxTempfiles)
//        {
//            buffer = new BytesRefArray(bufferBytesUsed);
//            if (ramBufferSize.bytes < ABSOLUTE_MIN_SORT_BUFFER_SIZE)
//            {
//                throw new ArgumentException(MIN_BUFFER_SIZE_MSG + ": " + ramBufferSize.bytes);
//            }
//            if (maxTempfiles < 2)
//            {
//                throw new ArgumentException("maxTempFiles must be >= 2");
//            }
//            this.ramBufferSize = ramBufferSize;
//            this.tempDirectory = tempDirectory;
//            this.maxTempFiles = maxTempfiles;
//            this.comparator = comparator;
//        }

//        /// <summary>Sort input to output, explicit hint for the buffer size.</summary>
//        /// <remarks>
//        /// Sort input to output, explicit hint for the buffer size. The amount of allocated
//        /// memory may deviate from the hint (may be smaller or larger).
//        /// </remarks>
//        /// <exception cref="System.IO.IOException"></exception>
//        public OfflineSorter.SortInfo Sort(FilePath input, FilePath output)
//        {
//            sortInfo = new OfflineSorter.SortInfo(this);
//            sortInfo.totalTime = DateTime.Now.CurrentTimeMillis();
//            output.Delete();
//            AList<FilePath> merges = new AList<FilePath>();
//            bool success2 = false;
//            try
//            {
//                OfflineSorter.ByteSequencesReader @is = new OfflineSorter.ByteSequencesReader(input
//                    );
//                bool success = false;
//                try
//                {
//                    int lines = 0;
//                    while ((lines = ReadPartition(@is)) > 0)
//                    {
//                        merges.AddItem(SortPartition(lines));
//                        sortInfo.tempMergeFiles++;
//                        sortInfo.lines += lines;
//                        // Handle intermediate merges.
//                        if (merges.Count == maxTempFiles)
//                        {
//                            FilePath intermediate = FilePath.CreateTempFile("sort", "intermediate", tempDirectory
//                                );
//                            try
//                            {
//                                MergePartitions(merges, intermediate);
//                            }
//                            finally
//                            {
//                                foreach (FilePath file in merges)
//                                {
//                                    file.Delete();
//                                }
//                                merges.Clear();
//                                merges.AddItem(intermediate);
//                            }
//                            sortInfo.tempMergeFiles++;
//                        }
//                    }
//                    success = true;
//                }
//                finally
//                {
//                    if (success)
//                    {
//                        IOUtils.Close(@is);
//                    }
//                    else
//                    {
//                        IOUtils.CloseWhileHandlingException(@is);
//                    }
//                }
//                // One partition, try to rename or copy if unsuccessful.
//                if (merges.Count == 1)
//                {
//                    FilePath single = merges[0];
//                    // If simple rename doesn't work this means the output is
//                    // on a different volume or something. Copy the input then.
//                    if (!single.RenameTo(output))
//                    {
//                        Copy(single, output);
//                    }
//                }
//                else
//                {
//                    // otherwise merge the partitions with a priority queue.
//                    MergePartitions(merges, output);
//                }
//                success2 = true;
//            }
//            finally
//            {
//                foreach (FilePath file in merges)
//                {
//                    file.Delete();
//                }
//                if (!success2)
//                {
//                    output.Delete();
//                }
//            }
//            sortInfo.totalTime = (DateTime.Now.CurrentTimeMillis() - sortInfo.totalTime);
//            return sortInfo;
//        }

//        /// <summary>Returns the default temporary directory.</summary>
//        /// <remarks>
//        /// Returns the default temporary directory. By default, java.io.tmpdir. If not accessible
//        /// or not available, an IOException is thrown
//        /// </remarks>
//        /// <exception cref="System.IO.IOException"></exception>
//        public static FilePath DefaultTempDir()
//        {
//            string tempDirPath = Runtime.GetProperty("java.io.tmpdir");
//            if (tempDirPath == null)
//            {
//                throw new IOException("Java has no temporary folder property (java.io.tmpdir)?");
//            }
//            FilePath tempDirectory = new FilePath(tempDirPath);
//            if (!tempDirectory.Exists() || !tempDirectory.CanWrite())
//            {
//                throw new IOException("Java's temporary folder not present or writeable?: " + tempDirectory
//                    .GetAbsolutePath());
//            }
//            return tempDirectory;
//        }

//        /// <summary>Copies one file to another.</summary>
//        /// <remarks>Copies one file to another.</remarks>
//        /// <exception cref="System.IO.IOException"></exception>
//        private static void Copy(FilePath file, FilePath output)
//        {
//            // 64kb copy buffer (empirical pick).
//            byte[] buffer = new byte[16 * 1024];
//            InputStream @is = null;
//            OutputStream os = null;
//            try
//            {
//                @is = new FileInputStream(file);
//                os = new FileOutputStream(output);
//                int length;
//                while ((length = @is.Read(buffer)) > 0)
//                {
//                    os.Write(buffer, 0, length);
//                }
//            }
//            finally
//            {
//                IOUtils.Close(@is, os);
//            }
//        }

//        /// <summary>Sort a single partition in-memory.</summary>
//        /// <remarks>Sort a single partition in-memory.</remarks>
//        /// <exception cref="System.IO.IOException"></exception>
//        protected internal FilePath SortPartition(int len)
//        {
//            BytesRefArray data = this.buffer;
//            FilePath tempFile = FilePath.CreateTempFile("sort", "partition", tempDirectory);
//            long start = DateTime.Now.CurrentTimeMillis();
//            sortInfo.sortTime += (DateTime.Now.CurrentTimeMillis() - start);
//            OfflineSorter.ByteSequencesWriter @out = new OfflineSorter.ByteSequencesWriter(tempFile
//                );
//            BytesRef spare;
//            try
//            {
//                BytesRefIterator iter = buffer.Iterator(comparator);
//                while ((spare = iter.Next()) != null)
//                {
//                    //HM:revisit 
//                    //assert spare.length <= Short.MAX_VALUE;
//                    @out.Write(spare);
//                }
//                @out.Close();
//                // Clean up the buffer for the next partition.
//                data.Clear();
//                return tempFile;
//            }
//            finally
//            {
//                IOUtils.Close(@out);
//            }
//        }

//        /// <summary>Merge a list of sorted temporary files (partitions) into an output file</summary>
//        /// <exception cref="System.IO.IOException"></exception>
//        internal void MergePartitions(IList<FilePath> merges, FilePath outputFile)
//        {
//            long start = DateTime.Now.CurrentTimeMillis();
//            OfflineSorter.ByteSequencesWriter @out = new OfflineSorter.ByteSequencesWriter(outputFile
//                );
//            PriorityQueue<OfflineSorter.FileAndTop> queue = new _PriorityQueue_361(this, merges
//                .Count);
//            OfflineSorter.ByteSequencesReader[] streams = new OfflineSorter.ByteSequencesReader
//                [merges.Count];
//            try
//            {
//                // Open streams and read the top for each file
//                for (int i = 0; i < merges.Count; i++)
//                {
//                    streams[i] = new OfflineSorter.ByteSequencesReader(merges[i]);
//                    byte[] line = streams[i].Read();
//                    if (line != null)
//                    {
//                        queue.InsertWithOverflow(new OfflineSorter.FileAndTop(i, line));
//                    }
//                }
//                // Unix utility sort() uses ordered array of files to pick the next line from, updating
//                // it as it reads new lines. The PQ used here is a more elegant solution and has 
//                // a nicer theoretical complexity bound :) The entire sorting process is I/O bound anyway
//                // so it shouldn't make much of a difference (didn't check).
//                OfflineSorter.FileAndTop top;
//                while ((top = queue.Top()) != null)
//                {
//                    @out.Write(top.current);
//                    if (!streams[top.fd].Read(top.current))
//                    {
//                        queue.Pop();
//                    }
//                    else
//                    {
//                        queue.UpdateTop();
//                    }
//                }
//                sortInfo.mergeTime += DateTime.Now.CurrentTimeMillis() - start;
//                sortInfo.mergeRounds++;
//            }
//            finally
//            {
//                // The logic below is: if an exception occurs in closing out, it has a priority over exceptions
//                // happening in closing streams.
//                try
//                {
//                    IOUtils.Close(streams);
//                }
//                finally
//                {
//                    IOUtils.Close(@out);
//                }
//            }
//        }

//        private sealed class _PriorityQueue_361 : PriorityQueue<OfflineSorter.FileAndTop>
//        {
//            public _PriorityQueue_361(OfflineSorter _enclosing, int baseArg1) : base(baseArg1
//                )
//            {
//                this._enclosing = _enclosing;
//            }

//            protected internal override bool LessThan(OfflineSorter.FileAndTop a, OfflineSorter.FileAndTop
//                 b)
//            {
//                return this._enclosing.comparator.Compare(a.current, b.current) < 0;
//            }

//            private readonly OfflineSorter _enclosing;
//        }

//        /// <summary>Read in a single partition of data</summary>
//        /// <exception cref="System.IO.IOException"></exception>
//        internal int ReadPartition(OfflineSorter.ByteSequencesReader reader)
//        {
//            long start = DateTime.Now.CurrentTimeMillis();
//            BytesRef scratch = new BytesRef();
//            while ((scratch.bytes = reader.Read()) != null)
//            {
//                scratch.length = scratch.bytes.Length;
//                buffer.Append(scratch);
//                // Account for the created objects.
//                // (buffer slots do not account to buffer size.) 
//                if (ramBufferSize.bytes < bufferBytesUsed.Get())
//                {
//                    break;
//                }
//            }
//            sortInfo.readTime += (DateTime.Now.CurrentTimeMillis() - start);
//            return buffer.Size();
//        }

//        internal class FileAndTop
//        {
//            internal readonly int fd;

//            internal readonly BytesRef current;

//            internal FileAndTop(int fd, byte[] firstLine)
//            {
//                this.fd = fd;
//                this.current = new BytesRef(firstLine);
//            }
//        }

//        /// <summary>Utility class to emit length-prefixed byte[] entries to an output stream for sorting.
//        /// 	</summary>
//        /// <remarks>
//        /// Utility class to emit length-prefixed byte[] entries to an output stream for sorting.
//        /// Complementary to
//        /// <see cref="ByteSequencesReader">ByteSequencesReader</see>
//        /// .
//        /// </remarks>
//        public class ByteSequencesWriter : IDisposable
//        {
//            private readonly DataOutput os;

//            /// <summary>Constructs a ByteSequencesWriter to the provided File</summary>
//            /// <exception cref="System.IO.IOException"></exception>
//            public ByteSequencesWriter(FilePath file) : this(new DataOutputStream(new BufferedOutputStream
//                (new FileOutputStream(file))))
//            {
//            }

//            /// <summary>Constructs a ByteSequencesWriter to the provided DataOutput</summary>
//            public ByteSequencesWriter(DataOutput os)
//            {
//                this.os = os;
//            }

//            /// <summary>Writes a BytesRef.</summary>
//            /// <remarks>Writes a BytesRef.</remarks>
//            /// <seealso cref="Write(byte[], int, int)">Write(byte[], int, int)</seealso>
//            /// <exception cref="System.IO.IOException"></exception>
//            public virtual void Write(BytesRef @ref)
//            {
//                //HM:revisit 
//                //assert ref != null;
//                Write(@ref.bytes, @ref.offset, @ref.length);
//            }

//            /// <summary>Writes a byte array.</summary>
//            /// <remarks>Writes a byte array.</remarks>
//            /// <seealso cref="Write(byte[], int, int)">Write(byte[], int, int)</seealso>
//            /// <exception cref="System.IO.IOException"></exception>
//            public virtual void Write(byte[] bytes)
//            {
//                Write(bytes, 0, bytes.Length);
//            }

//            /// <summary>Writes a byte array.</summary>
//            /// <remarks>
//            /// Writes a byte array.
//            /// <p>
//            /// The length is written as a <code>short</code>, followed
//            /// by the bytes.
//            /// </remarks>
//            /// <exception cref="System.IO.IOException"></exception>
//            public virtual void Write(byte[] bytes, int off, int len)
//            {
//                //HM:revisit 
//                //assert bytes != null;
//                //HM:revisit 
//                //assert off >= 0 && off + len <= bytes.length;
//                //HM:revisit 
//                //assert len >= 0;
//                if (len > short.MaxValue)
//                {
//                    throw new ArgumentException("len must be <= " + short.MaxValue + "; got " + len);
//                }
//                os.WriteShort(len);
//                os.Write(bytes, off, len);
//            }

//            /// <summary>
//            /// Closes the provided
//            /// <see cref="System.IO.DataOutput">System.IO.DataOutput</see>
//            /// if it is
//            /// <see cref="System.IDisposable">System.IDisposable</see>
//            /// .
//            /// </summary>
//            /// <exception cref="System.IO.IOException"></exception>
//            public virtual void Close()
//            {
//                if (os is IDisposable)
//                {
//                    ((IDisposable)os).Close();
//                }
//            }
//        }

//        /// <summary>Utility class to read length-prefixed byte[] entries from an input.</summary>
//        /// <remarks>
//        /// Utility class to read length-prefixed byte[] entries from an input.
//        /// Complementary to
//        /// <see cref="ByteSequencesWriter">ByteSequencesWriter</see>
//        /// .
//        /// </remarks>
//        public class ByteSequencesReader : IDisposable
//        {
//            private readonly DataInput @is;

//            /// <summary>Constructs a ByteSequencesReader from the provided File</summary>
//            /// <exception cref="System.IO.IOException"></exception>
//            public ByteSequencesReader(FilePath file) : this(new DataInputStream(new BufferedInputStream
//                (new FileInputStream(file))))
//            {
//            }

//            /// <summary>Constructs a ByteSequencesReader from the provided DataInput</summary>
//            public ByteSequencesReader(DataInput @is)
//            {
//                this.@is = @is;
//            }

//            /// <summary>
//            /// Reads the next entry into the provided
//            /// <see cref="BytesRef">BytesRef</see>
//            /// . The internal
//            /// storage is resized if needed.
//            /// </summary>
//            /// <returns>
//            /// Returns <code>false</code> if EOF occurred when trying to read
//            /// the header of the next sequence. Returns <code>true</code> otherwise.
//            /// </returns>
//            /// <exception cref="System.IO.EOFException">if the file ends before the full sequence is read.
//            /// 	</exception>
//            /// <exception cref="System.IO.IOException"></exception>
//            public virtual bool Read(BytesRef @ref)
//            {
//                short length;
//                try
//                {
//                    length = @is.ReadShort();
//                }
//                catch (EOFException)
//                {
//                    return false;
//                }
//                @ref.Grow(length);
//                @ref.offset = 0;
//                @ref.length = length;
//                @is.ReadFully(@ref.bytes, 0, length);
//                return true;
//            }

//            /// <summary>Reads the next entry and returns it if successful.</summary>
//            /// <remarks>Reads the next entry and returns it if successful.</remarks>
//            /// <seealso cref="Read(BytesRef)">Read(BytesRef)</seealso>
//            /// <returns>
//            /// Returns <code>null</code> if EOF occurred before the next entry
//            /// could be read.
//            /// </returns>
//            /// <exception cref="System.IO.EOFException">if the file ends before the full sequence is read.
//            /// 	</exception>
//            /// <exception cref="System.IO.IOException"></exception>
//            public virtual byte[] Read()
//            {
//                short length;
//                try
//                {
//                    length = @is.ReadShort();
//                }
//                catch (EOFException)
//                {
//                    return null;
//                }
//                //HM:revisit 
//                //assert length >= 0 : "Sanity: sequence length < 0: " + length;
//                byte[] result = new byte[length];
//                @is.ReadFully(result);
//                return result;
//            }

//            /// <summary>
//            /// Closes the provided
//            /// <see cref="System.IO.DataInput">System.IO.DataInput</see>
//            /// if it is
//            /// <see cref="System.IDisposable">System.IDisposable</see>
//            /// .
//            /// </summary>
//            /// <exception cref="System.IO.IOException"></exception>
//            public virtual void Close()
//            {
//                if (@is is IDisposable)
//                {
//                    ((IDisposable)@is).Close();
//                }
//            }
//        }

//        /// <summary>Returns the comparator in use to sort entries</summary>
//        public IComparer<BytesRef> GetComparator()
//        {
//            return comparator;
//        }
//    }
//}
