/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Codecs.Asserting;
using Org.Apache.Lucene.Codecs.Lucene42;
using Org.Apache.Lucene.Index;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Asserting
{
	/// <summary>
	/// Just like
	/// <see cref="Org.Apache.Lucene.Codecs.Lucene42.Lucene42NormsFormat">Org.Apache.Lucene.Codecs.Lucene42.Lucene42NormsFormat
	/// 	</see>
	/// but with additional asserts.
	/// </summary>
	public class AssertingNormsFormat : NormsFormat
	{
		private readonly NormsFormat @in = new Lucene42NormsFormat();

		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesConsumer NormsConsumer(SegmentWriteState state)
		{
			DocValuesConsumer consumer = @in.NormsConsumer(state);
			//HM:revisit 
			//assert consumer != null;
			return new AssertingDocValuesFormat.AssertingNormsConsumer(consumer, state.segmentInfo
				.GetDocCount());
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesProducer NormsProducer(SegmentReadState state)
		{
			//HM:revisit 
			//assert state.fieldInfos.hasNorms();
			DocValuesProducer producer = @in.NormsProducer(state);
			//HM:revisit 
			//assert producer != null;
			return new AssertingDocValuesFormat.AssertingDocValuesProducer(producer, state.segmentInfo
				.GetDocCount());
		}
	}
}
