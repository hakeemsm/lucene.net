/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Codecs;
using Org.Apache.Lucene.Codecs.Lucene3x;
using Org.Apache.Lucene.Index;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Lucene3x
{
	/// <lucene.internal></lucene.internal>
	/// <lucene.experimental></lucene.experimental>
	internal class PreFlexRWNormsFormat : Lucene3xNormsFormat
	{
		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesConsumer NormsConsumer(SegmentWriteState state)
		{
			return new PreFlexRWNormsConsumer(state.directory, state.segmentInfo.name, state.
				context);
		}
	}
}
