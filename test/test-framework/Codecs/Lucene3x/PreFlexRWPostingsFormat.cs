/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Org.Apache.Lucene.Codecs.Lucene3x;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Codecs.Lucene3x
{
	/// <summary>
	/// Codec, only for testing, that can write and read the
	/// pre-flex index format.
	/// </summary>
	/// <remarks>
	/// Codec, only for testing, that can write and read the
	/// pre-flex index format.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	internal class PreFlexRWPostingsFormat : Lucene3xPostingsFormat
	{
		public PreFlexRWPostingsFormat()
		{
		}

		// NOTE: we impersonate the PreFlex codec so that it can
		// read the segments we write!
		/// <exception cref="System.IO.IOException"></exception>
		public override Org.Apache.Lucene.Codecs.FieldsConsumer FieldsConsumer(SegmentWriteState
			 state)
		{
			return new PreFlexRWFieldsWriter(state);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Org.Apache.Lucene.Codecs.FieldsProducer FieldsProducer(SegmentReadState
			 state)
		{
			// Whenever IW opens readers, eg for merging, we have to
			// keep terms order in UTF16:
			return new _Lucene3xFields_51(state.directory, state.fieldInfos, state.segmentInfo
				, state.context, state.termsIndexDivisor);
		}

		private sealed class _Lucene3xFields_51 : Lucene3xFields
		{
			public _Lucene3xFields_51(Directory baseArg1, FieldInfos baseArg2, SegmentInfo baseArg3
				, IOContext baseArg4, int baseArg5) : base(baseArg1, baseArg2, baseArg3, baseArg4
				, baseArg5)
			{
			}

			protected override bool SortTermsByUnicode()
			{
				// We carefully peek into stack track above us: if
				// we are part of a "merge", we must sort by UTF16:
				bool unicodeSortOrder = true;
				StackTraceElement[] trace = new Exception().GetStackTrace();
				for (int i = 0; i < trace.Length; i++)
				{
					//System.out.println(trace[i].getClassName());
					if ("merge".Equals(trace[i].GetMethodName()))
					{
						unicodeSortOrder = false;
						if (LuceneTestCase.VERBOSE)
						{
							System.Console.Out.WriteLine("NOTE: PreFlexRW codec: forcing legacy UTF16 term sort order"
								);
						}
						break;
					}
				}
				return unicodeSortOrder;
			}
		}
	}
}
