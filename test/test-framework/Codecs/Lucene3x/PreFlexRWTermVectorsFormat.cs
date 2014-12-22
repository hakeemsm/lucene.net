/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Org.Apache.Lucene.Codecs;
using Lucene.Net.Codecs.Lucene3x;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Lucene.Net.Codecs.Lucene3x
{
	internal class PreFlexRWTermVectorsFormat : Lucene3xTermVectorsFormat
	{
		/// <exception cref="System.IO.IOException"></exception>
		public override TermVectorsWriter VectorsWriter(Directory directory, SegmentInfo 
			segmentInfo, IOContext context)
		{
			return new PreFlexRWTermVectorsWriter(directory, segmentInfo.name, context);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override TermVectorsReader VectorsReader(Directory directory, SegmentInfo 
			segmentInfo, FieldInfos fieldInfos, IOContext context)
		{
			return new _Lucene3xTermVectorsReader_39(directory, segmentInfo, fieldInfos, context
				);
		}

		private sealed class _Lucene3xTermVectorsReader_39 : Lucene3xTermVectorsReader
		{
			public _Lucene3xTermVectorsReader_39(Directory baseArg1, SegmentInfo baseArg2, FieldInfos
				 baseArg3, IOContext baseArg4) : base(baseArg1, baseArg2, baseArg3, baseArg4)
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
							System.Console.Out.WriteLine("NOTE: PreFlexRW codec: forcing legacy UTF16 vector term sort order"
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
