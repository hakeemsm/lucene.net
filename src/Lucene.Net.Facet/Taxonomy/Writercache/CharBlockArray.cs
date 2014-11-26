/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lucene.Net.Facet.Taxonomy.Writercache;
using Sharpen;

namespace Lucene.Net.Facet.Taxonomy.Writercache
{
	/// <summary>
	/// Similar to
	/// <see cref="System.Text.StringBuilder">System.Text.StringBuilder</see>
	/// , but with a more efficient growing strategy.
	/// This class uses char array blocks to grow.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	[System.Serializable]
	internal class CharBlockArray : Appendable, CharSequence
	{
		private const long serialVersionUID = 1L;

		private const int DefaultBlockSize = 32 * 1024;

		[System.Serializable]
		internal sealed class Block : ICloneable
		{
			private const long serialVersionUID = 1L;

			internal readonly char[] chars;

			internal int length;

			internal Block(int size)
			{
				// 32 KB default size
				this.chars = new char[size];
				this.length = 0;
			}
		}

		internal IList<CharBlockArray.Block> blocks;

		internal CharBlockArray.Block current;

		internal int blockSize;

		internal int length;

		public CharBlockArray() : this(DefaultBlockSize)
		{
		}

		internal CharBlockArray(int blockSize)
		{
			this.blocks = new AList<CharBlockArray.Block>();
			this.blockSize = blockSize;
			AddBlock();
		}

		private void AddBlock()
		{
			this.current = new CharBlockArray.Block(this.blockSize);
			this.blocks.AddItem(this.current);
		}

		internal virtual int BlockIndex(int index)
		{
			return index / blockSize;
		}

		internal virtual int IndexInBlock(int index)
		{
			return index % blockSize;
		}

		public virtual CharBlockArray Append(CharSequence chars)
		{
			return AppendRange(chars, 0, chars.Length);
		}

		public virtual CharBlockArray Append(char c)
		{
			if (this.current.length == this.blockSize)
			{
				AddBlock();
			}
			this.current.chars[this.current.length++] = c;
			this.length++;
			return this;
		}

		public virtual CharBlockArray AppendRange(CharSequence chars, int start, int length
			)
		{
			int end = start + length;
			for (int i = start; i < end; i++)
			{
				Append(chars[i]);
			}
			return this;
		}

		public virtual CharBlockArray Append(char[] chars, int start, int length)
		{
			int offset = start;
			int remain = length;
			while (remain > 0)
			{
				if (this.current.length == this.blockSize)
				{
					AddBlock();
				}
				int toCopy = remain;
				int remainingInBlock = this.blockSize - this.current.length;
				if (remainingInBlock < toCopy)
				{
					toCopy = remainingInBlock;
				}
				System.Array.Copy(chars, offset, this.current.chars, this.current.length, toCopy);
				offset += toCopy;
				remain -= toCopy;
				this.current.length += toCopy;
			}
			this.length += length;
			return this;
		}

		public virtual CharBlockArray Append(string s)
		{
			int remain = s.Length;
			int offset = 0;
			while (remain > 0)
			{
				if (this.current.length == this.blockSize)
				{
					AddBlock();
				}
				int toCopy = remain;
				int remainingInBlock = this.blockSize - this.current.length;
				if (remainingInBlock < toCopy)
				{
					toCopy = remainingInBlock;
				}
				Sharpen.Runtime.GetCharsForString(s, offset, offset + toCopy, this.current.chars, 
					this.current.length);
				offset += toCopy;
				remain -= toCopy;
				this.current.length += toCopy;
			}
			this.length += s.Length;
			return this;
		}

		public virtual char CharAt(int index)
		{
			CharBlockArray.Block b = blocks[BlockIndex(index)];
			return b.chars[IndexInBlock(index)];
		}

		public virtual int Length
		{
			get
			{
				return this.length;
			}
		}

		public virtual CharSequence SubSequence(int start, int end)
		{
			int remaining = end - start;
			StringBuilder sb = new StringBuilder(remaining);
			int blockIdx = BlockIndex(start);
			int indexInBlock = IndexInBlock(start);
			while (remaining > 0)
			{
				CharBlockArray.Block b = blocks[blockIdx++];
				int numToAppend = Math.Min(remaining, b.length - indexInBlock);
				sb.Append(b.chars, indexInBlock, numToAppend);
				remaining -= numToAppend;
				indexInBlock = 0;
			}
			// 2nd+ iterations read from start of the block 
			return sb.ToString();
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			foreach (CharBlockArray.Block b in blocks)
			{
				sb.Append(b.chars, 0, b.length);
			}
			return sb.ToString();
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void Flush(OutputStream @out)
		{
			ObjectOutputStream oos = null;
			try
			{
				oos = new ObjectOutputStream(@out);
				oos.WriteObject(this);
				oos.Flush();
			}
			finally
			{
				if (oos != null)
				{
					oos.Close();
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="System.TypeLoadException"></exception>
		public static CharBlockArray Open(InputStream @in)
		{
			ObjectInputStream ois = null;
			try
			{
				ois = new ObjectInputStream(@in);
				CharBlockArray a = (CharBlockArray)ois.ReadObject();
				return a;
			}
			finally
			{
				if (ois != null)
				{
					ois.Close();
				}
			}
		}
	}
}
