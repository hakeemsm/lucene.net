using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Store;
using Lucene.Net.Support;

namespace Lucene.Net.Index
{
	/// <summary>
	/// Embeds a [read-only] SegmentInfo and adds per-commit
	/// fields.
	/// </summary>
	/// <remarks>
	/// Embeds a [read-only] SegmentInfo and adds per-commit
	/// fields.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class SegmentCommitInfo
	{
		/// <summary>
		/// The
		/// <see cref="SegmentInfo">SegmentInfo</see>
		/// that we wrap.
		/// </summary>
		public readonly SegmentInfo info;

		private int delCount;

		private long delGen;

		private long nextWriteDelGen;

		private long fieldInfosGen;

		private long nextWriteFieldInfosGen;

		private readonly IDictionary<long, ICollection<string>> genUpdatesFiles = new Dictionary
			<long, ICollection<string>>();

		private long sizeInBytes = -1;

		/// <summary>Sole constructor.</summary>
		/// <remarks>Sole constructor.</remarks>
		/// <param name="info">
		/// <see cref="SegmentInfo">SegmentInfo</see>
		/// that we wrap
		/// </param>
		/// <param name="delCount">number of deleted documents in this segment</param>
		/// <param name="delGen">deletion generation number (used to name deletion files)</param>
		/// <param name="fieldInfosGen">FieldInfos generation number (used to name field-infos files)
		/// 	</param>
		public SegmentCommitInfo(SegmentInfo info, int delCount, long delGen, long fieldInfosGen
			)
		{
			// How many deleted docs in the segment:
			// Generation number of the live docs file (-1 if there
			// are no deletes yet):
			// Normally 1+delGen, unless an exception was hit on last
			// attempt to write:
			// Generation number of the FieldInfos (-1 if there are no updates)
			// Normally 1 + fieldInfosGen, unless an exception was hit on last attempt to
			// write
			// Track the per-generation updates files
			this.info = info;
			this.delCount = delCount;
			this.delGen = delGen;
			if (delGen == -1)
			{
				nextWriteDelGen = 1;
			}
			else
			{
				nextWriteDelGen = delGen + 1;
			}
			this.fieldInfosGen = fieldInfosGen;
			if (fieldInfosGen == -1)
			{
				nextWriteFieldInfosGen = 1;
			}
			else
			{
				nextWriteFieldInfosGen = fieldInfosGen + 1;
			}
		}

		/// <summary>Returns the per generation updates files.</summary>
		/// <remarks>Returns the per generation updates files.</remarks>
		public virtual IDictionary<long, ICollection<string>> GetUpdatesFiles()
		{
			return new HashMap<long, ICollection<string>>(genUpdatesFiles);
		}

		/// <summary>Sets the updates file names per generation.</summary>
		/// <remarks>Sets the updates file names per generation. Does not deep clone the map.
		/// 	</remarks>
		public virtual void SetGenUpdatesFiles(IDictionary<long, ICollection<string>> genUpdatesFiles)
		{
			this.genUpdatesFiles.Clear();
			this.genUpdatesFiles.PutAll(genUpdatesFiles);
		}

		/// <summary>Called when we succeed in writing deletes</summary>
		internal virtual void AdvanceDelGen()
		{
			delGen = nextWriteDelGen;
			nextWriteDelGen = delGen + 1;
			sizeInBytes = -1;
		}

		/// <summary>
		/// Called if there was an exception while writing
		/// deletes, so that we don't try to write to the same
		/// file more than once.
		/// </summary>
		/// <remarks>
		/// Called if there was an exception while writing
		/// deletes, so that we don't try to write to the same
		/// file more than once.
		/// </remarks>
		internal virtual void AdvanceNextWriteDelGen()
		{
			nextWriteDelGen++;
		}

		/// <summary>Called when we succeed in writing a new FieldInfos generation.</summary>
		/// <remarks>Called when we succeed in writing a new FieldInfos generation.</remarks>
		internal virtual void AdvanceFieldInfosGen()
		{
			fieldInfosGen = nextWriteFieldInfosGen;
			nextWriteFieldInfosGen = fieldInfosGen + 1;
			sizeInBytes = -1;
		}

		/// <summary>
		/// Called if there was an exception while writing a new generation of
		/// FieldInfos, so that we don't try to write to the same file more than once.
		/// </summary>
		/// <remarks>
		/// Called if there was an exception while writing a new generation of
		/// FieldInfos, so that we don't try to write to the same file more than once.
		/// </remarks>
		internal virtual void AdvanceNextWriteFieldInfosGen()
		{
			nextWriteFieldInfosGen++;
		}

		/// <summary>
		/// Returns total size in bytes of all files for this
		/// segment.
		/// </summary>
		/// <remarks>
		/// Returns total size in bytes of all files for this
		/// segment.
		/// <p><b>NOTE:</b> This value is not correct for 3.0 segments
		/// that have shared docstores. To get the correct value, upgrade!
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual long SizeInBytes()
		{
			if (sizeInBytes == -1)
			{
				long sum = Files.Sum(fileName => info.dir.FileLength(fileName));
			    sizeInBytes = sum;
			}
			return sizeInBytes;
		}

		public virtual ICollection<string> Files
		{
		    get
		    {
		        // Start from the wrapped info's files:
		        ICollection<string> files = new HashSet<string>(info.Files);
		        // TODO we could rely on TrackingDir.getCreatedFiles() (like we do for
		        // updates) and then maybe even be able to remove LiveDocsFormat.files().
		        // Must separately add any live docs files:
		        info.Codec.LiveDocsFormat.Files(this, files);
		        // Must separately add any field updates files
		        foreach (ICollection<string> updateFiles in genUpdatesFiles.Values)
		        {
		            files.Concat(updateFiles);
		        }
		        return files;
		    }
		}

		private long bufferedDeletesGen;

		// NOTE: only used in-RAM by IW to track buffered deletes;
		// this is never written to/read from the Directory
		internal virtual long BufferedDeletesGen
		{
		    get { return bufferedDeletesGen; }
		    set
		    {
                bufferedDeletesGen = value;
                sizeInBytes = -1;
		    }
		}

		/// <summary>
		/// Returns true if there are any deletions for the
		/// segment at this commit.
		/// </summary>
		/// <remarks>
		/// Returns true if there are any deletions for the
		/// segment at this commit.
		/// </remarks>
		public virtual bool HasDeletions
		{
		    get { return delGen != -1; }
		}

		/// <summary>Returns true if there are any field updates for the segment in this commit.
		/// 	</summary>
		/// <remarks>Returns true if there are any field updates for the segment in this commit.
		/// 	</remarks>
		public virtual bool HasFieldUpdates
		{
		    get { return fieldInfosGen != -1; }
		}

		/// <summary>Returns the next available generation number of the FieldInfos files.</summary>
		/// <remarks>Returns the next available generation number of the FieldInfos files.</remarks>
		public virtual long NextFieldInfosGen
		{
		    get { return nextWriteFieldInfosGen; }
		}

		/// <summary>
		/// Returns the generation number of the field infos file or -1 if there are no
		/// field updates yet.
		/// </summary>
		/// <remarks>
		/// Returns the generation number of the field infos file or -1 if there are no
		/// field updates yet.
		/// </remarks>
		public virtual long FieldInfosGen
		{
		    get { return fieldInfosGen; }
		}

		/// <summary>
		/// Returns the next available generation number
		/// of the live docs file.
		/// </summary>
		/// <remarks>
		/// Returns the next available generation number
		/// of the live docs file.
		/// </remarks>
		public virtual long NextDelGen
		{
		    get { return nextWriteDelGen; }
		}

		/// <summary>
		/// Returns generation number of the live docs file
		/// or -1 if there are no deletes yet.
		/// </summary>
		/// <remarks>
		/// Returns generation number of the live docs file
		/// or -1 if there are no deletes yet.
		/// </remarks>
		public virtual long DelGen
		{
		    get { return delGen; }
		}

		/// <summary>Returns the number of deleted docs in the segment.</summary>
		/// <remarks>Returns the number of deleted docs in the segment.</remarks>
		public virtual int DelCount
		{
		    get { return delCount; }
		    set
		    {
                if (value < 0 || value > info.DocCount)
                {
                    throw new ArgumentException("invalid delCount=" + delCount + " (docCount=" + info.DocCount + ")");
                }
                this.delCount = value;
		    }
		}

		/// <summary>Returns a description of this segment.</summary>
		/// <remarks>Returns a description of this segment.</remarks>
		public virtual string ToString(Directory dir, int pendingDelCount)
		{
			string s = info.ToString(dir, delCount + pendingDelCount);
			if (delGen != -1)
			{
				s += ":delGen=" + delGen;
			}
			if (fieldInfosGen != -1)
			{
				s += ":fieldInfosGen=" + fieldInfosGen;
			}
			return s;
		}

		public override string ToString()
		{
			return ToString(info.dir, 0);
		}

		public virtual SegmentCommitInfo Clone()
		{
			var other = new SegmentCommitInfo
				(info, delCount, delGen, fieldInfosGen)
			{
			    nextWriteDelGen = nextWriteDelGen,
			    nextWriteFieldInfosGen = nextWriteFieldInfosGen
			};
			// Not clear that we need to carry over nextWriteDelGen
			// (i.e. do we ever clone after a failed write and
			// before the next successful write?), but just do it to
			// be safe:
		    // deep clone
			foreach (KeyValuePair<long, ICollection<string>> e in genUpdatesFiles)
			{
				other.genUpdatesFiles[e.Key] = new HashSet<string>(e.Value);
			}
			return other;
		}
	}
}
