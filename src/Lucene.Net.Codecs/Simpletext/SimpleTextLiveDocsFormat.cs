/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Simpletext;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Simpletext
{
	/// <summary>
	/// reads/writes plaintext live docs
	/// <p>
	/// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class SimpleTextLiveDocsFormat : LiveDocsFormat
	{
		internal static readonly string LIVEDOCS_EXTENSION = "liv";

		internal static readonly BytesRef SIZE = new BytesRef("size ");

		internal static readonly BytesRef DOC = new BytesRef("  doc ");

		internal static readonly BytesRef END = new BytesRef("END");

		/// <exception cref="System.IO.IOException"></exception>
		public override MutableBits NewLiveDocs(int size)
		{
			return new SimpleTextLiveDocsFormat.SimpleTextMutableBits(size);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override MutableBits NewLiveDocs(Bits existing)
		{
			SimpleTextLiveDocsFormat.SimpleTextBits bits = (SimpleTextLiveDocsFormat.SimpleTextBits
				)existing;
			return new SimpleTextLiveDocsFormat.SimpleTextMutableBits((BitSet)bits.bits.Clone
				(), bits.size);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Bits ReadLiveDocs(Directory dir, SegmentCommitInfo info, IOContext
			 context)
		{
			//HM:revisit 
			//assert info.hasDeletions();
			BytesRef scratch = new BytesRef();
			CharsRef scratchUTF16 = new CharsRef();
			string fileName = IndexFileNames.FileNameFromGeneration(info.info.name, LIVEDOCS_EXTENSION
				, info.GetDelGen());
			ChecksumIndexInput @in = null;
			bool success = false;
			try
			{
				@in = dir.OpenChecksumInput(fileName, context);
				SimpleTextUtil.ReadLine(@in, scratch);
				//HM:revisit 
				//assert StringHelper.startsWith(scratch, SIZE);
				int size = ParseIntAt(scratch, SIZE.length, scratchUTF16);
				BitSet bits = new BitSet(size);
				SimpleTextUtil.ReadLine(@in, scratch);
				while (!scratch.Equals(END))
				{
					//HM:revisit 
					//assert StringHelper.startsWith(scratch, DOC);
					int docid = ParseIntAt(scratch, DOC.length, scratchUTF16);
					bits.Set(docid);
					SimpleTextUtil.ReadLine(@in, scratch);
				}
				SimpleTextUtil.CheckFooter(@in);
				success = true;
				return new SimpleTextLiveDocsFormat.SimpleTextBits(bits, size);
			}
			finally
			{
				if (success)
				{
					IOUtils.Close(@in);
				}
				else
				{
					IOUtils.CloseWhileHandlingException(@in);
				}
			}
		}

		private int ParseIntAt(BytesRef bytes, int offset, CharsRef scratch)
		{
			UnicodeUtil.UTF8toUTF16(bytes.bytes, bytes.offset + offset, bytes.length - offset
				, scratch);
			return ArrayUtil.ParseInt(scratch.chars, 0, scratch.length);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void WriteLiveDocs(MutableBits bits, Directory dir, SegmentCommitInfo
			 info, int newDelCount, IOContext context)
		{
			BitSet set = ((SimpleTextLiveDocsFormat.SimpleTextBits)bits).bits;
			int size = bits.Length();
			BytesRef scratch = new BytesRef();
			string fileName = IndexFileNames.FileNameFromGeneration(info.info.name, LIVEDOCS_EXTENSION
				, info.GetNextDelGen());
			IndexOutput @out = null;
			bool success = false;
			try
			{
				@out = dir.CreateOutput(fileName, context);
				SimpleTextUtil.Write(@out, SIZE);
				SimpleTextUtil.Write(@out, Sharpen.Extensions.ToString(size), scratch);
				SimpleTextUtil.WriteNewline(@out);
				for (int i = set.NextSetBit(0); i >= 0; i = set.NextSetBit(i + 1))
				{
					SimpleTextUtil.Write(@out, DOC);
					SimpleTextUtil.Write(@out, Sharpen.Extensions.ToString(i), scratch);
					SimpleTextUtil.WriteNewline(@out);
				}
				SimpleTextUtil.Write(@out, END);
				SimpleTextUtil.WriteNewline(@out);
				SimpleTextUtil.WriteChecksum(@out, scratch);
				success = true;
			}
			finally
			{
				if (success)
				{
					IOUtils.Close(@out);
				}
				else
				{
					IOUtils.CloseWhileHandlingException(@out);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Files(SegmentCommitInfo info, ICollection<string> files)
		{
			if (info.HasDeletions())
			{
				files.AddItem(IndexFileNames.FileNameFromGeneration(info.info.name, LIVEDOCS_EXTENSION
					, info.GetDelGen()));
			}
		}

		internal class SimpleTextBits : Bits
		{
			internal readonly BitSet bits;

			internal readonly int size;

			internal SimpleTextBits(BitSet bits, int size)
			{
				// read-only
				this.bits = bits;
				this.size = size;
			}

			public override bool Get(int index)
			{
				return bits.Get(index);
			}

			public override int Length()
			{
				return size;
			}
		}

		internal class SimpleTextMutableBits : SimpleTextLiveDocsFormat.SimpleTextBits, MutableBits
		{
			internal SimpleTextMutableBits(int size) : this(new BitSet(size), size)
			{
				// read-write
				bits.Set(0, size);
			}

			internal SimpleTextMutableBits(BitSet bits, int size) : base(bits, size)
			{
			}

			public virtual void Clear(int bit)
			{
				bits.Clear(bit);
			}
		}
	}
}
