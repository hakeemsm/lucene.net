/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Codecs;
using Lucene.Net.Codecs.Simpletext;
using Lucene.Net.Index;
using Sharpen;

namespace Lucene.Net.Codecs.Simpletext
{
	/// <summary>plain-text norms format.</summary>
	/// <remarks>
	/// plain-text norms format.
	/// <p>
	/// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class SimpleTextNormsFormat : NormsFormat
	{
		private static readonly string NORMS_SEG_EXTENSION = "len";

		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesConsumer NormsConsumer(SegmentWriteState state)
		{
			return new SimpleTextNormsFormat.SimpleTextNormsConsumer(state);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override DocValuesProducer NormsProducer(SegmentReadState state)
		{
			return new SimpleTextNormsFormat.SimpleTextNormsProducer(state);
		}

		/// <summary>Reads plain-text norms.</summary>
		/// <remarks>
		/// Reads plain-text norms.
		/// <p>
		/// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
		/// </remarks>
		/// <lucene.experimental></lucene.experimental>
		public class SimpleTextNormsProducer : SimpleTextDocValuesReader
		{
			/// <exception cref="System.IO.IOException"></exception>
			public SimpleTextNormsProducer(SegmentReadState state) : base(state, NORMS_SEG_EXTENSION
				)
			{
			}
			// All we do is change the extension from .dat -> .len;
			// otherwise this is a normal simple doc values file:
		}

		/// <summary>Writes plain-text norms.</summary>
		/// <remarks>
		/// Writes plain-text norms.
		/// <p>
		/// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
		/// </remarks>
		/// <lucene.experimental></lucene.experimental>
		public class SimpleTextNormsConsumer : SimpleTextDocValuesWriter
		{
			/// <exception cref="System.IO.IOException"></exception>
			public SimpleTextNormsConsumer(SegmentWriteState state) : base(state, NORMS_SEG_EXTENSION
				)
			{
			}
			// All we do is change the extension from .dat -> .len;
			// otherwise this is a normal simple doc values file:
		}
	}
}
