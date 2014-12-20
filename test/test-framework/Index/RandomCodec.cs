/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using Lucene.NetCodecs;
using Lucene.NetCodecs.Asserting;
using Lucene.NetCodecs.Bloom;
using Lucene.NetCodecs.Diskdv;
using Lucene.NetCodecs.Lucene41;
using Lucene.NetCodecs.Lucene41ords;
using Lucene.NetCodecs.Lucene45;
using Lucene.NetCodecs.Lucene46;
using Lucene.NetCodecs.Memory;
using Lucene.NetCodecs.Mockintblock;
using Lucene.NetCodecs.Mockrandom;
using Lucene.NetCodecs.Mocksep;
using Lucene.NetCodecs.Nestedpulsing;
using Lucene.NetCodecs.Pulsing;
using Lucene.NetCodecs.Simpletext;
using Lucene.Net.TestFramework.Util;
using Sharpen;

namespace Lucene.Net.TestFramework.Index
{
	/// <summary>Codec that assigns per-field random postings formats.</summary>
	/// <remarks>
	/// Codec that assigns per-field random postings formats.
	/// <p>
	/// The same field/format assignment will happen regardless of order,
	/// a hash is computed up front that determines the mapping.
	/// This means fields can be put into things like HashSets and added to
	/// documents in different orders and the test will still be deterministic
	/// and reproducable.
	/// </remarks>
	public class RandomCodec : Lucene46Codec
	{
		/// <summary>Shuffled list of postings formats to use for new mappings</summary>
		private IList<PostingsFormat> formats = new AList<PostingsFormat>();

		/// <summary>Shuffled list of docvalues formats to use for new mappings</summary>
		private IList<DocValuesFormat> dvFormats = new AList<DocValuesFormat>();

		/// <summary>unique set of format names this codec knows about</summary>
		public ICollection<string> formatNames = new HashSet<string>();

		/// <summary>unique set of docvalues format names this codec knows about</summary>
		public ICollection<string> dvFormatNames = new HashSet<string>();

		/// <summary>memorized field-&gt;postingsformat mappings</summary>
		private IDictionary<string, PostingsFormat> previousMappings = Sharpen.Collections
			.SynchronizedMap(new Dictionary<string, PostingsFormat>());

		private IDictionary<string, DocValuesFormat> previousDVMappings = Sharpen.Collections
			.SynchronizedMap(new Dictionary<string, DocValuesFormat>());

		private readonly int perFieldSeed;

		// note: we have to sync this map even though its just for debugging/toString, 
		// otherwise DWPT's .toString() calls that iterate over the map can 
		// cause concurrentmodificationexception if indexwriter's infostream is on
		public override PostingsFormat GetPostingsFormatForField(string name)
		{
			PostingsFormat codec = previousMappings.Get(name);
			if (codec == null)
			{
				codec = formats[Math.Abs(perFieldSeed ^ name.GetHashCode()) % formats.Count];
				if (codec is SimpleTextPostingsFormat && perFieldSeed % 5 != 0)
				{
					// make simpletext rarer, choose again
					codec = formats[Math.Abs(perFieldSeed ^ name.ToUpper(CultureInfo.ROOT).GetHashCode
						()) % formats.Count];
				}
				previousMappings.Put(name, codec);
			}
			// Safety:
			//HM:revisit 
			//assert previousMappings.size() < 10000: "test went insane";
			return codec;
		}

		public override DocValuesFormat GetDocValuesFormatForField(string name)
		{
			DocValuesFormat codec = previousDVMappings.Get(name);
			if (codec == null)
			{
				codec = dvFormats[Math.Abs(perFieldSeed ^ name.GetHashCode()) % dvFormats.Count];
				if (codec is SimpleTextDocValuesFormat && perFieldSeed % 5 != 0)
				{
					// make simpletext rarer, choose again
					codec = dvFormats[Math.Abs(perFieldSeed ^ name.ToUpper(CultureInfo.ROOT).GetHashCode
						()) % dvFormats.Count];
				}
				previousDVMappings.Put(name, codec);
			}
			// Safety:
			//HM:revisit 
			//assert previousDVMappings.size() < 10000: "test went insane";
			return codec;
		}

		public RandomCodec(Random random, ICollection<string> avoidCodecs)
		{
			this.perFieldSeed = random.Next();
			// TODO: make it possible to specify min/max iterms per
			// block via CL:
			int minItemsPerBlock = TestUtil.NextInt(random, 2, 100);
			int maxItemsPerBlock = 2 * (Math.Max(2, minItemsPerBlock - 1)) + random.Next(100);
			int lowFreqCutoff = TestUtil.NextInt(random, 2, 100);
			Add(avoidCodecs, new Lucene41PostingsFormat(minItemsPerBlock, maxItemsPerBlock), 
				new FSTPostingsFormat(), new FSTOrdPostingsFormat(), new FSTPulsing41PostingsFormat
				(1 + random.Next(20)), new FSTOrdPulsing41PostingsFormat(1 + random.Next(20)), new 
				DirectPostingsFormat(LuceneTestCase.Rarely(random) ? 1 : (LuceneTestCase.Rarely(
				random) ? int.MaxValue : maxItemsPerBlock), LuceneTestCase.Rarely(random) ? 1 : 
				(LuceneTestCase.Rarely(random) ? int.MaxValue : lowFreqCutoff)), new Pulsing41PostingsFormat
				(1 + random.Next(20), minItemsPerBlock, maxItemsPerBlock), new Pulsing41PostingsFormat
				(1 + random.Next(20), minItemsPerBlock, maxItemsPerBlock), new TestBloomFilteredLucene41Postings
				(), new MockSepPostingsFormat(), new MockFixedIntBlockPostingsFormat(TestUtil.NextInt
				(random, 1, 2000)), new MockVariableIntBlockPostingsFormat(TestUtil.NextInt(random
				, 1, 127)), new MockRandomPostingsFormat(random), new NestedPulsingPostingsFormat
				(), new Lucene41WithOrds(), new SimpleTextPostingsFormat(), new AssertingPostingsFormat
				(), new MemoryPostingsFormat(true, random.NextFloat()), new MemoryPostingsFormat
				(false, random.NextFloat()));
			// add pulsing again with (usually) different parameters
			//TODO as a PostingsFormat which wraps others, we should allow TestBloomFilteredLucene41Postings to be constructed 
			//with a choice of concrete PostingsFormats. Maybe useful to have a generic means of marking and dealing 
			//with such "wrapper" classes?
			AddDocValues(avoidCodecs, new Lucene45DocValuesFormat(), new DiskDocValuesFormat(
				), new MemoryDocValuesFormat(), new SimpleTextDocValuesFormat(), new AssertingDocValuesFormat
				());
			Sharpen.Collections.Shuffle(formats, random);
			Sharpen.Collections.Shuffle(dvFormats, random);
			// Avoid too many open files:
			if (formats.Count > 4)
			{
				formats = formats.SubList(0, 4);
			}
			if (dvFormats.Count > 4)
			{
				dvFormats = dvFormats.SubList(0, 4);
			}
		}

		public RandomCodec(Random random) : this(random, Sharpen.Collections.EmptySet<string
			>())
		{
		}

		private void Add(ICollection<string> avoidCodecs, params PostingsFormat[] postings
			)
		{
			foreach (PostingsFormat p in postings)
			{
				if (!avoidCodecs.Contains(p.GetName()))
				{
					formats.AddItem(p);
					formatNames.AddItem(p.GetName());
				}
			}
		}

		private void AddDocValues(ICollection<string> avoidCodecs, params DocValuesFormat
			[] docvalues)
		{
			foreach (DocValuesFormat d in docvalues)
			{
				if (!avoidCodecs.Contains(d.GetName()))
				{
					dvFormats.AddItem(d);
					dvFormatNames.AddItem(d.GetName());
				}
			}
		}

		public override string ToString()
		{
			return base.ToString() + ": " + previousMappings.ToString() + ", docValues:" + previousDVMappings
				.ToString();
		}
	}
}
