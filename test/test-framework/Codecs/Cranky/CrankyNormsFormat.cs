using System;
using System.IO;
using Lucene.Net.Index;

namespace Lucene.Net.Codecs.Cranky.TestFramework
{
	internal class CrankyNormsFormat : NormsFormat
	{
		internal readonly NormsFormat normsFormat;

		internal readonly Random random;

		internal CrankyNormsFormat(NormsFormat del, Random random)
		{
			this.normsFormat = del;
			this.random = random;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesConsumer NormsConsumer(SegmentWriteState state)
		{
			if (random.Next(100) == 0)
			{
				throw new IOException("Fake IOException from NormsFormat.fieldsConsumer()");
			}
			return new CrankyDocValuesFormat.CrankyDocValuesConsumer(normsFormat.NormsConsumer(state), random);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesProducer NormsProducer(SegmentReadState state)
		{
			return normsFormat.NormsProducer(state);
		}
	}
}
