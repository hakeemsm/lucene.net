/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.IO;
using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Cranky
{
	internal class CrankyLiveDocsFormat : LiveDocsFormat
	{
		internal readonly LiveDocsFormat delegate_;

		internal readonly Random random;

		internal CrankyLiveDocsFormat(LiveDocsFormat delegate_, Random random)
		{
			this.delegate_ = delegate_;
			this.random = random;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override MutableBits NewLiveDocs(int size)
		{
			return delegate_.NewLiveDocs(size);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override MutableBits NewLiveDocs(Bits existing)
		{
			return delegate_.NewLiveDocs(existing);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Bits ReadLiveDocs(Directory dir, SegmentCommitInfo info, IOContext
			 context)
		{
			return delegate_.ReadLiveDocs(dir, info, context);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void WriteLiveDocs(MutableBits bits, Directory dir, SegmentCommitInfo
			 info, int newDelCount, IOContext context)
		{
			if (random.Next(100) == 0)
			{
				throw new IOException("Fake IOException from LiveDocsFormat.writeLiveDocs()");
			}
			delegate_.WriteLiveDocs(bits, dir, info, newDelCount, context);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Files(SegmentCommitInfo info, ICollection<string> files)
		{
			// TODO: is this called only from write? if so we should throw exception!
			delegate_.Files(info, files);
		}
	}
}
