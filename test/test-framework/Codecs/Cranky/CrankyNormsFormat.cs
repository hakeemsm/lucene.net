/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.IO;
using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Codecs.Cranky;
using Org.Apache.Lucene.Index;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Cranky
{
	internal class CrankyNormsFormat : NormsFormat
	{
		internal readonly NormsFormat delegate_;

		internal readonly Random random;

		internal CrankyNormsFormat(NormsFormat delegate_, Random random)
		{
			this.delegate_ = delegate_;
			this.random = random;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesConsumer NormsConsumer(SegmentWriteState state)
		{
			if (random.Next(100) == 0)
			{
				throw new IOException("Fake IOException from NormsFormat.fieldsConsumer()");
			}
			return new CrankyDocValuesFormat.CrankyDocValuesConsumer(delegate_.NormsConsumer(
				state), random);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesProducer NormsProducer(SegmentReadState state)
		{
			return delegate_.NormsProducer(state);
		}
	}
}
