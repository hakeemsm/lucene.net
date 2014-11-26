/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Sharpen;

namespace Lucene.Net.Replicator
{
	/// <summary>
	/// Describes a file in a
	/// <see cref="Revision">Revision</see>
	/// . A file has a source, which allows a
	/// single revision to contain files from multiple sources (e.g. multiple
	/// indexes).
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class RevisionFile
	{
		/// <summary>The name of the file.</summary>
		/// <remarks>The name of the file.</remarks>
		public readonly string fileName;

		/// <summary>
		/// The size of the file denoted by
		/// <see cref="fileName">fileName</see>
		/// .
		/// </summary>
		public long size = -1;

		/// <summary>Constructor with the given file name.</summary>
		/// <remarks>Constructor with the given file name.</remarks>
		public RevisionFile(string fileName)
		{
			if (fileName == null || fileName.IsEmpty())
			{
				throw new ArgumentException("fileName cannot be null or empty");
			}
			this.fileName = fileName;
		}

		public override bool Equals(object obj)
		{
			Lucene.Net.Replicator.RevisionFile other = (Lucene.Net.Replicator.RevisionFile
				)obj;
			return fileName.Equals(other.fileName) && size == other.size;
		}

		public override int GetHashCode()
		{
			return fileName.GetHashCode() ^ (int)(size ^ ((long)(((ulong)size) >> 32)));
		}

		public override string ToString()
		{
			return "fileName=" + fileName + " size=" + size;
		}
	}
}
