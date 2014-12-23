using System;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Codecs.Lucene3x;

namespace Lucene.Net.Codecs.Lucene3x.TestFramework
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
			return new Anon3xTermVectorsReader(directory, segmentInfo, fieldInfos, context);
		}

		private sealed class Anon3xTermVectorsReader : Lucene3xTermVectorsReader
		{
			public Anon3xTermVectorsReader(Directory baseArg1, SegmentInfo baseArg2, FieldInfos
				 baseArg3, IOContext baseArg4) : base(baseArg1, baseArg2, baseArg3, baseArg4)
			{
			}

			protected override bool SortTermsByUnicode
			{
			    get
			    {
			        // We carefully peek into stack track above us: if
			        // we are part of a "merge", we must sort by UTF16:
			        bool unicodeSortOrder = true;
			        var trace = new Exception().StackTrace;
			        //.NET Port Stack trace is a string
			            //System.out.println(trace[i].getClassName());
			            if (trace.Contains("merge"))
			            {
			                unicodeSortOrder = false;
			                if (LuceneTestCase.VERBOSE)
			                {
			                    System.Console.Out.WriteLine("NOTE: PreFlexRW codec: forcing legacy UTF16 vector term sort order"
			                        );
			                }
			                
			            }
			        
			        return unicodeSortOrder;
			    }
			}
		}
	}
}
