using Lucene.Net.Index;
using Lucene.Net.Codecs.Lucene42;

namespace Lucene.Net.Codecs.Asserting.TestFramework
{
	/// <summary>
	/// Just like
	/// <see cref="Lucene.Net.Codecs.Lucene42.Lucene42NormsFormat">Lucene.Net.Codecs.Lucene42.Lucene42NormsFormat
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
			 
			//assert consumer != null;
			return new AssertingDocValuesFormat.AssertingNormsConsumer(consumer, state.segmentInfo.DocCount);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesProducer NormsProducer(SegmentReadState state)
		{
			 
			//assert state.fieldInfos.hasNorms();
			DocValuesProducer producer = @in.NormsProducer(state);
			 
			//assert producer != null;
			return new AssertingDocValuesFormat.AssertingDocValuesProducer(producer, state.segmentInfo.DocCount);
		}
	}
}
