/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Writercache;
using Sharpen;

namespace Lucene.Net.Facet.Taxonomy.Writercache
{
	/// <summary>
	/// This is a very efficient LabelToOrdinal implementation that uses a
	/// CharBlockArray to store all labels and a configurable number of HashArrays to
	/// reference the labels.
	/// </summary>
	/// <remarks>
	/// This is a very efficient LabelToOrdinal implementation that uses a
	/// CharBlockArray to store all labels and a configurable number of HashArrays to
	/// reference the labels.
	/// <p>
	/// Since the HashArrays don't handle collisions, a
	/// <see cref="CollisionMap">CollisionMap</see>
	/// is used
	/// to store the colliding labels.
	/// <p>
	/// This data structure grows by adding a new HashArray whenever the number of
	/// collisions in the
	/// <see cref="CollisionMap">CollisionMap</see>
	/// exceeds
	/// <code>loadFactor</code>
	/// *
	/// <see cref="LabelToOrdinal.GetMaxOrdinal()">LabelToOrdinal.GetMaxOrdinal()</see>
	/// . Growing also includes reinserting all colliding
	/// labels into the HashArrays to possibly reduce the number of collisions.
	/// For setting the
	/// <code>loadFactor</code>
	/// see
	/// <see cref="CompactLabelToOrdinal(int, float, int)">CompactLabelToOrdinal(int, float, int)
	/// 	</see>
	/// .
	/// <p>
	/// This data structure has a much lower memory footprint (~30%) compared to a
	/// Java HashMap&lt;String, Integer&gt;. It also only uses a small fraction of objects
	/// a HashMap would use, thus limiting the GC overhead. Ingestion speed was also
	/// ~50% faster compared to a HashMap for 3M unique labels.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class CompactLabelToOrdinal : LabelToOrdinal
	{
		/// <summary>Default maximum load factor.</summary>
		/// <remarks>Default maximum load factor.</remarks>
		public const float DefaultLoadFactor = 0.15f;

		internal const char TERMINATOR_CHAR = unchecked((int)(0xffff));

		private const int COLLISION = -5;

		private CompactLabelToOrdinal.HashArray[] hashArrays;

		private CollisionMap collisionMap;

		private CharBlockArray labelRepository;

		private int capacity;

		private int threshold;

		private float loadFactor;

		/// <summary>How many labels.</summary>
		/// <remarks>How many labels.</remarks>
		public virtual int SizeOfMap()
		{
			return this.collisionMap.Size();
		}

		public CompactLabelToOrdinal()
		{
		}

		/// <summary>Sole constructor.</summary>
		/// <remarks>Sole constructor.</remarks>
		public CompactLabelToOrdinal(int initialCapacity, float loadFactor, int numHashArrays
			)
		{
			this.hashArrays = new CompactLabelToOrdinal.HashArray[numHashArrays];
			this.capacity = DetermineCapacity((int)Math.Pow(2, numHashArrays), initialCapacity
				);
			Init();
			this.collisionMap = new CollisionMap(this.labelRepository);
			this.counter = 0;
			this.loadFactor = loadFactor;
			this.threshold = (int)(this.loadFactor * this.capacity);
		}

		internal static int DetermineCapacity(int minCapacity, int initialCapacity)
		{
			int capacity = minCapacity;
			while (capacity < initialCapacity)
			{
				capacity <<= 1;
			}
			return capacity;
		}

		private void Init()
		{
			labelRepository = new CharBlockArray();
			CategoryPathUtils.Serialize(new FacetLabel(), labelRepository);
			int c = this.capacity;
			for (int i = 0; i < this.hashArrays.Length; i++)
			{
				this.hashArrays[i] = new CompactLabelToOrdinal.HashArray(c);
				c /= 2;
			}
		}

		public override void AddLabel(FacetLabel label, int ordinal)
		{
			if (collisionMap.Size() > threshold)
			{
				Grow();
			}
			int hash = Lucene.Net.Facet.Taxonomy.Writercache.CompactLabelToOrdinal.StringHashCode
				(label);
			for (int i = 0; i < this.hashArrays.Length; i++)
			{
				if (AddLabel(this.hashArrays[i], label, hash, ordinal))
				{
					return;
				}
			}
			int prevVal = collisionMap.AddLabel(label, hash, ordinal);
			if (prevVal != ordinal)
			{
				throw new ArgumentException("Label already exists: " + label + " prev ordinal " +
					 prevVal);
			}
		}

		public override int GetOrdinal(FacetLabel label)
		{
			if (label == null)
			{
				return LabelToOrdinal.INVALID_ORDINAL;
			}
			int hash = Lucene.Net.Facet.Taxonomy.Writercache.CompactLabelToOrdinal.StringHashCode
				(label);
			for (int i = 0; i < this.hashArrays.Length; i++)
			{
				int ord = GetOrdinal(this.hashArrays[i], label, hash);
				if (ord != COLLISION)
				{
					return ord;
				}
			}
			return this.collisionMap.Get(label, hash);
		}

		private void Grow()
		{
			CompactLabelToOrdinal.HashArray temp = this.hashArrays[this.hashArrays.Length - 1
				];
			for (int i = this.hashArrays.Length - 1; i > 0; i--)
			{
				this.hashArrays[i] = this.hashArrays[i - 1];
			}
			this.capacity *= 2;
			this.hashArrays[0] = new CompactLabelToOrdinal.HashArray(this.capacity);
			for (int i_1 = 1; i_1 < this.hashArrays.Length; i_1++)
			{
				int[] sourceOffsetArray = this.hashArrays[i_1].offsets;
				int[] sourceCidsArray = this.hashArrays[i_1].cids;
				for (int k = 0; k < sourceOffsetArray.Length; k++)
				{
					for (int j = 0; j < i_1 && sourceOffsetArray[k] != 0; j++)
					{
						int[] targetOffsetArray = this.hashArrays[j].offsets;
						int[] targetCidsArray = this.hashArrays[j].cids;
						int newIndex = IndexFor(StringHashCode(this.labelRepository, sourceOffsetArray[k]
							), targetOffsetArray.Length);
						if (targetOffsetArray[newIndex] == 0)
						{
							targetOffsetArray[newIndex] = sourceOffsetArray[k];
							targetCidsArray[newIndex] = sourceCidsArray[k];
							sourceOffsetArray[k] = 0;
						}
					}
				}
			}
			for (int i_2 = 0; i_2 < temp.offsets.Length; i_2++)
			{
				int offset = temp.offsets[i_2];
				if (offset > 0)
				{
					int hash = StringHashCode(this.labelRepository, offset);
					AddLabelOffset(hash, temp.cids[i_2], offset);
				}
			}
			CollisionMap oldCollisionMap = this.collisionMap;
			this.collisionMap = new CollisionMap(oldCollisionMap.Capacity(), this.labelRepository
				);
			this.threshold = (int)(this.capacity * this.loadFactor);
			Iterator<CollisionMap.Entry> it = oldCollisionMap.EntryIterator();
			while (it.HasNext())
			{
				CollisionMap.Entry e = it.Next();
				AddLabelOffset(StringHashCode(this.labelRepository, e.offset), e.cid, e.offset);
			}
		}

		private bool AddLabel(CompactLabelToOrdinal.HashArray a, FacetLabel label, int hash
			, int ordinal)
		{
			int index = Lucene.Net.Facet.Taxonomy.Writercache.CompactLabelToOrdinal.IndexFor
				(hash, a.offsets.Length);
			int offset = a.offsets[index];
			if (offset == 0)
			{
				a.offsets[index] = this.labelRepository.Length;
				CategoryPathUtils.Serialize(label, labelRepository);
				a.cids[index] = ordinal;
				return true;
			}
			return false;
		}

		private void AddLabelOffset(int hash, int cid, int knownOffset)
		{
			for (int i = 0; i < this.hashArrays.Length; i++)
			{
				if (AddLabelOffsetToHashArray(this.hashArrays[i], hash, cid, knownOffset))
				{
					return;
				}
			}
			this.collisionMap.AddLabelOffset(hash, knownOffset, cid);
			if (this.collisionMap.Size() > this.threshold)
			{
				Grow();
			}
		}

		private bool AddLabelOffsetToHashArray(CompactLabelToOrdinal.HashArray a, int hash
			, int ordinal, int knownOffset)
		{
			int index = Lucene.Net.Facet.Taxonomy.Writercache.CompactLabelToOrdinal.IndexFor
				(hash, a.offsets.Length);
			int offset = a.offsets[index];
			if (offset == 0)
			{
				a.offsets[index] = knownOffset;
				a.cids[index] = ordinal;
				return true;
			}
			return false;
		}

		private int GetOrdinal(CompactLabelToOrdinal.HashArray a, FacetLabel label, int hash
			)
		{
			if (label == null)
			{
				return LabelToOrdinal.INVALID_ORDINAL;
			}
			int index = IndexFor(hash, a.offsets.Length);
			int offset = a.offsets[index];
			if (offset == 0)
			{
				return LabelToOrdinal.INVALID_ORDINAL;
			}
			if (CategoryPathUtils.EqualsToSerialized(label, labelRepository, offset))
			{
				return a.cids[index];
			}
			return COLLISION;
		}

		/// <summary>Returns index for hash code h.</summary>
		/// <remarks>Returns index for hash code h.</remarks>
		internal static int IndexFor(int h, int length)
		{
			return h & (length - 1);
		}

		// static int stringHashCode(String label) {
		// int len = label.length();
		// int hash = 0;
		// int i;
		// for (i = 0; i < len; ++i)
		// hash = 33 * hash + label.charAt(i);
		//
		// hash = hash ^ ((hash >>> 20) ^ (hash >>> 12));
		// hash = hash ^ (hash >>> 7) ^ (hash >>> 4);
		//
		// return hash;
		//
		// }
		internal static int StringHashCode(FacetLabel label)
		{
			int hash = label.GetHashCode();
			hash = hash ^ (((int)(((uint)hash) >> 20)) ^ ((int)(((uint)hash) >> 12)));
			hash = hash ^ ((int)(((uint)hash) >> 7)) ^ ((int)(((uint)hash) >> 4));
			return hash;
		}

		internal static int StringHashCode(CharBlockArray labelRepository, int offset)
		{
			int hash = CategoryPathUtils.HashCodeOfSerialized(labelRepository, offset);
			hash = hash ^ (((int)(((uint)hash) >> 20)) ^ ((int)(((uint)hash) >> 12)));
			hash = hash ^ ((int)(((uint)hash) >> 7)) ^ ((int)(((uint)hash) >> 4));
			return hash;
		}

		// public static boolean equals(CharSequence label, CharBlockArray array,
		// int offset) {
		// // CONTINUE HERE
		// int len = label.length();
		// int bi = array.blockIndex(offset);
		// CharBlockArray.Block b = array.blocks.get(bi);
		// int index = array.indexInBlock(offset);
		//
		// for (int i = 0; i < len; i++) {
		// if (label.charAt(i) != b.chars[index]) {
		// return false;
		// }
		// index++;
		// if (index == b.length) {
		// b = array.blocks.get(++bi);
		// index = 0;
		// }
		// }
		//
		// return b.chars[index] == TerminatorChar;
		// }
		/// <summary>Returns an estimate of the amount of memory used by this table.</summary>
		/// <remarks>
		/// Returns an estimate of the amount of memory used by this table. Called only in
		/// this package. Memory is consumed mainly by three structures: the hash arrays,
		/// label repository and collision map.
		/// </remarks>
		internal virtual int GetMemoryUsage()
		{
			int memoryUsage = 0;
			if (this.hashArrays != null)
			{
				// HashArray capacity is instance-specific.
				foreach (CompactLabelToOrdinal.HashArray ha in this.hashArrays)
				{
					// Each has 2 capacity-length arrays of ints.
					memoryUsage += (ha.capacity * 2 * 4) + 4;
				}
			}
			if (this.labelRepository != null)
			{
				// All blocks are the same size.
				int blockSize = this.labelRepository.blockSize;
				// Each block has room for blockSize UTF-16 chars.
				int actualBlockSize = (blockSize * 2) + 4;
				memoryUsage += this.labelRepository.blocks.Count * actualBlockSize;
				memoryUsage += 8;
			}
			// Two int values for array as a whole.
			if (this.collisionMap != null)
			{
				memoryUsage += this.collisionMap.GetMemoryUsage();
			}
			return memoryUsage;
		}

		/// <summary>Opens the file and reloads the CompactLabelToOrdinal.</summary>
		/// <remarks>
		/// Opens the file and reloads the CompactLabelToOrdinal. The file it expects
		/// is generated from the
		/// <see cref="Flush(Sharpen.FilePath)">Flush(Sharpen.FilePath)</see>
		/// command.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		internal static Lucene.Net.Facet.Taxonomy.Writercache.CompactLabelToOrdinal
			 Open(FilePath file, float loadFactor, int numHashArrays)
		{
			Lucene.Net.Facet.Taxonomy.Writercache.CompactLabelToOrdinal l2o = new Lucene.Net.Facet.Taxonomy.Writercache.CompactLabelToOrdinal
				();
			l2o.loadFactor = loadFactor;
			l2o.hashArrays = new CompactLabelToOrdinal.HashArray[numHashArrays];
			DataInputStream dis = null;
			try
			{
				dis = new DataInputStream(new BufferedInputStream(new FileInputStream(file)));
				// TaxiReader needs to load the "counter" or occupancy (L2O) to know
				// the next unique facet. we used to load the delimiter too, but
				// never used it.
				l2o.counter = dis.ReadInt();
				l2o.capacity = DetermineCapacity((int)Math.Pow(2, l2o.hashArrays.Length), l2o.counter
					);
				l2o.Init();
				// now read the chars
				l2o.labelRepository = CharBlockArray.Open(dis);
				l2o.collisionMap = new CollisionMap(l2o.labelRepository);
				// Calculate hash on the fly based on how CategoryPath hashes
				// itself. Maybe in the future we can call some static based methods
				// in CategoryPath so that this doesn't break again? I don't like
				// having code in two different places...
				int cid = 0;
				// Skip the initial offset, it's the CategoryPath(0,0), which isn't
				// a hashed value.
				int offset = 1;
				int lastStartOffset = offset;
				// This loop really relies on a well-formed input (assumes pretty blindly
				// that array offsets will work).  Since the initial file is machine 
				// generated, I think this should be OK.
				while (offset < l2o.labelRepository.Length)
				{
					// identical code to CategoryPath.hashFromSerialized. since we need to
					// advance offset, we cannot call the method directly. perhaps if we
					// could pass a mutable Integer or something...
					int length = (short)l2o.labelRepository[offset++];
					int hash = length;
					if (length != 0)
					{
						for (int i = 0; i < length; i++)
						{
							int len = (short)l2o.labelRepository[offset++];
							hash = hash * 31 + l2o.labelRepository.SubSequence(offset, offset + len).GetHashCode
								();
							offset += len;
						}
					}
					// Now that we've hashed the components of the label, do the
					// final part of the hash algorithm.
					hash = hash ^ (((int)(((uint)hash) >> 20)) ^ ((int)(((uint)hash) >> 12)));
					hash = hash ^ ((int)(((uint)hash) >> 7)) ^ ((int)(((uint)hash) >> 4));
					// Add the label, and let's keep going
					l2o.AddLabelOffset(hash, cid, lastStartOffset);
					cid++;
					lastStartOffset = offset;
				}
			}
			catch (TypeLoadException)
			{
				throw new IOException("Invalid file format. Cannot deserialize.");
			}
			finally
			{
				if (dis != null)
				{
					dis.Close();
				}
			}
			l2o.threshold = (int)(l2o.loadFactor * l2o.capacity);
			return l2o;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void Flush(FilePath file)
		{
			FileOutputStream fos = new FileOutputStream(file);
			try
			{
				BufferedOutputStream os = new BufferedOutputStream(fos);
				DataOutputStream dos = new DataOutputStream(os);
				dos.WriteInt(this.counter);
				// write the labelRepository
				this.labelRepository.Flush(dos);
				// Closes the data output stream
				dos.Close();
			}
			finally
			{
				fos.Close();
			}
		}

		private sealed class HashArray
		{
			internal int[] offsets;

			internal int[] cids;

			internal int capacity;

			internal HashArray(int c)
			{
				this.capacity = c;
				this.offsets = new int[this.capacity];
				this.cids = new int[this.capacity];
			}
		}
	}
}
