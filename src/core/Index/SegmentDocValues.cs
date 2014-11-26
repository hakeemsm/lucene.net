/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Codecs;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Index
{
	/// <summary>
	/// Manages the
	/// <see cref="Lucene.Net.Codecs.DocValuesProducer">Lucene.Net.Codecs.DocValuesProducer
	/// 	</see>
	/// held by
	/// <see cref="SegmentReader">SegmentReader</see>
	/// and
	/// keeps track of their reference counting.
	/// </summary>
	internal sealed class SegmentDocValues
	{
		private readonly IDictionary<long, RefCount<DocValuesProducer>> genDVProducers = 
			new Dictionary<long, RefCount<DocValuesProducer>>();

		/// <exception cref="System.IO.IOException"></exception>
		private RefCount<DocValuesProducer> NewDocValuesProducer(SegmentCommitInfo si, IOContext
			 context, Directory dir, DocValuesFormat dvFormat, long gen, IList<FieldInfo> infos
			, int termsIndexDivisor)
		{
			Directory dvDir = dir;
			string segmentSuffix = string.Empty;
			if (gen != -1)
			{
				dvDir = si.info.dir;
				// gen'd files are written outside CFS, so use SegInfo directory
				segmentSuffix = System.Convert.ToString(gen, char.MAX_RADIX);
			}
			// set SegmentReadState to list only the fields that are relevant to that gen
			SegmentReadState srs = new SegmentReadState(dvDir, si.info, new FieldInfos(Sharpen.Collections.ToArray
				(infos, new FieldInfo[infos.Count])), context, termsIndexDivisor, segmentSuffix);
			return new _RefCount_51(this, gen, dvFormat.FieldsProducer(srs));
		}

		private sealed class _RefCount_51 : RefCount<DocValuesProducer>
		{
			public _RefCount_51(SegmentDocValues _enclosing, long gen, DocValuesProducer baseArg1
				) : base(baseArg1)
			{
				this._enclosing = _enclosing;
				this.gen = gen;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override void Release()
			{
				this.genericObject.Close();
				lock (this._enclosing)
				{
					Sharpen.Collections.Remove(this._enclosing.genDVProducers, gen);
				}
			}

			private readonly SegmentDocValues _enclosing;

			private readonly long gen;
		}

		/// <summary>
		/// Returns the
		/// <see cref="Lucene.Net.Codecs.DocValuesProducer">Lucene.Net.Codecs.DocValuesProducer
		/// 	</see>
		/// for the given generation.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		internal DocValuesProducer GetDocValuesProducer(long gen, SegmentCommitInfo si, IOContext
			 context, Directory dir, DocValuesFormat dvFormat, IList<FieldInfo> infos, int termsIndexDivisor
			)
		{
			lock (this)
			{
				RefCount<DocValuesProducer> dvp = genDVProducers.Get(gen);
				if (dvp == null)
				{
					dvp = NewDocValuesProducer(si, context, dir, dvFormat, gen, infos, termsIndexDivisor
						);
					//HM:revisit 
					//assert dvp != null;
					genDVProducers.Put(gen, dvp);
				}
				else
				{
					dvp.IncRef();
				}
				return dvp.Get();
			}
		}

		/// <summary>
		/// Decrement the reference count of the given
		/// <see cref="Lucene.Net.Codecs.DocValuesProducer">Lucene.Net.Codecs.DocValuesProducer
		/// 	</see>
		/// generations.
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		internal void DecRef(IList<long> dvProducersGens)
		{
			lock (this)
			{
				Exception t = null;
				foreach (long gen in dvProducersGens)
				{
					RefCount<DocValuesProducer> dvp = genDVProducers.Get(gen);
					//HM:revisit 
					//assert dvp != null : "gen=" + gen;
					try
					{
						dvp.DecRef();
					}
					catch (Exception th)
					{
						if (t != null)
						{
							t = th;
						}
					}
				}
				if (t != null)
				{
					IOUtils.ReThrow(t);
				}
			}
		}
	}
}
